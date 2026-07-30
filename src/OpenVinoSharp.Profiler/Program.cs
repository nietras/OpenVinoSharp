using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using OpenVinoSharp;

const string SearchPattern = "*.onnx";
const string DeviceName = "CPU";
const string EnableProfilingProperty = "PERF_COUNT";
const string EnableProfilingValue = "YES";
const string InferenceThreadCountProperty = "INFERENCE_NUM_THREADS";
const string NumberOfStreamsProperty = "NUM_STREAMS";
const int BatchSize = 1;
const int WarmupCount = 3;
const int MinimumIterations = 10;
const int ProfilingSamples = 10;
const double TargetRunDurationMilliseconds = 1000;
TimeSpan concurrentTestDuration = TimeSpan.FromSeconds(1);
int[] concurrentThreadCountsToTest = [1, 2, 4, 8, 16];
ProfilingConfiguration[] configurations =
[
    new("CPU", null, null, false),
    new("CPU 1*Thread 1*Stream", 1, 1, true),
];

Action<string> log = message =>
{
    Console.WriteLine(message);
    Trace.WriteLine(message);
};

var workingDirectory = Environment.CurrentDirectory;
var modelPaths = Directory.GetFiles(workingDirectory, SearchPattern, SearchOption.AllDirectories);
Array.Sort(modelPaths, StringComparer.Ordinal);

log($"Current directory: '{workingDirectory}'");
log($"Found {modelPaths.Length} files for '{SearchPattern}': {string.Join(", ", modelPaths.Select(path => $"'{path}'"))}");

foreach (var modelPath in modelPaths)
{
    var reportPath = Path.Combine(
        Path.GetDirectoryName(modelPath)!,
        $"{Path.GetFileNameWithoutExtension(modelPath)}.openvino-profiler.md");

    using var writer = new StreamWriter(reportPath);
    Action<string> report = message =>
    {
        log(message);
        writer.WriteLine(message);
    };

    report($"# `{Path.GetRelativePath(workingDirectory, modelPath)}` ({new FileInfo(modelPath).Length} bytes)");
    report(string.Empty);
    report("## Single-request performance");
    report("```");
    report($"{"Execution Provider",-32};BatchSize;Compile [ms];First [ms];Iterations;Mean/b [ms];Mean/s [ms]");
    var configurationToProfilingInfo = new List<(ProfilingConfiguration Configuration, IReadOnlyList<NodeProfile> ProfilingInfo)>();
    foreach (var configuration in configurations)
    {
        configurationToProfilingInfo.Add((configuration, RunModel(modelPath, configuration, report)));
    }
    report("```");

    report(string.Empty);
    report("## Concurrent app-thread scaling (single shared compiled model)");
    report("```");
    report($"{"Execution Provider",-32};Threads;Iterations;Throughput [calls/s];Min Mean/call [ms];Avg Mean/call [ms];Max Mean/call [ms]");
    foreach (var configuration in configurations)
    {
        RunModelConcurrent(modelPath, configuration, concurrentThreadCountsToTest, concurrentTestDuration, report);
    }
    report("```");

    foreach (var (configuration, profilingInfo) in configurationToProfilingInfo)
    {
        if (profilingInfo.Count > 0)
        {
            WriteNodeProfileSummary(configuration.Name, profilingInfo, report);
        }
    }
    log($"Wrote report: '{reportPath}'.");
}

if (modelPaths.Length == 0)
{
    log($"No models found. Copy one or more '{SearchPattern}' files below '{workingDirectory}'.");
}

static IReadOnlyList<NodeProfile> RunModel(
    string modelPath,
    ProfilingConfiguration configuration,
    Action<string> log)
{
    using var core = CreateProfilingCore(configuration);
    using var model = core.ReadModel(modelPath);

    var beforeCompile = Stopwatch.GetTimestamp();
    using var compiledModel = model.Compile(DeviceName);
    var compileMilliseconds = ElapsedMilliseconds(beforeCompile);

    using var inferRequest = compiledModel.CreateInferRequest();
    var beforeFirstInference = Stopwatch.GetTimestamp();
    inferRequest.Infer();
    var firstInferenceMilliseconds = ElapsedMilliseconds(beforeFirstInference);

    for (var warmup = 0; warmup < WarmupCount; ++warmup)
    {
        inferRequest.Infer();
    }

    var elapsedMilliseconds = new List<double>(32 * 1024);
    var totalMilliseconds = 0.0;
    while (totalMilliseconds < TargetRunDurationMilliseconds || elapsedMilliseconds.Count < MinimumIterations)
    {
        var beforeInference = Stopwatch.GetTimestamp();
        inferRequest.Infer();
        var milliseconds = ElapsedMilliseconds(beforeInference);
        totalMilliseconds += milliseconds;
        elapsedMilliseconds.Add(milliseconds);
    }

    var meanPerBatchMilliseconds = totalMilliseconds / elapsedMilliseconds.Count;
    log($"{configuration.Name,-32};{BatchSize,9};{compileMilliseconds,12:F3};{firstInferenceMilliseconds,10:F3};" +
        $"{elapsedMilliseconds.Count,10};{meanPerBatchMilliseconds,11:F3};{meanPerBatchMilliseconds / BatchSize,11:F3}");

    if (!configuration.EnableProfiling)
    {
        return [];
    }

    var nodeNameToProfile = new Dictionary<string, NodeProfile>(StringComparer.Ordinal);
    for (var sample = 0; sample < ProfilingSamples; ++sample)
    {
        inferRequest.Infer();
        foreach (var profilingInfo in inferRequest.GetProfilingInfo())
        {
            nodeNameToProfile.AddOrUpdate(profilingInfo);
        }
    }

    return nodeNameToProfile.Values.ToArray();
}

static void RunModelConcurrent(
    string modelPath,
    ProfilingConfiguration configuration,
    int[] threadCounts,
    TimeSpan duration,
    Action<string> log)
{
    using var core = CreateProfilingCore(configuration);
    using var model = core.ReadModel(modelPath);
    using var compiledModel = model.Compile(DeviceName);

    using (var warmupRequest = compiledModel.CreateInferRequest())
    {
        warmupRequest.Infer();
    }

    foreach (var threadCount in threadCounts)
    {
        using var barrier = new Barrier(threadCount + 1);
        var iterationsPerThread = new long[threadCount];
        var totalMillisecondsPerThread = new double[threadCount];
        var running = 1;
        var threads = new Thread[threadCount];

        for (var threadIndex = 0; threadIndex < threadCount; ++threadIndex)
        {
            var index = threadIndex;
            threads[index] = new Thread(() =>
            {
                using var inferRequest = compiledModel.CreateInferRequest();
                for (var warmup = 0; warmup < WarmupCount; ++warmup)
                {
                    inferRequest.Infer();
                }

                barrier.SignalAndWait();
                var iterations = 0L;
                var totalMilliseconds = 0.0;
                while (Volatile.Read(ref running) != 0)
                {
                    var beforeInference = Stopwatch.GetTimestamp();
                    inferRequest.Infer();
                    totalMilliseconds += ElapsedMilliseconds(beforeInference);
                    ++iterations;
                }

                iterationsPerThread[index] = iterations;
                totalMillisecondsPerThread[index] = totalMilliseconds;
            })
            {
                IsBackground = true,
            };
            threads[index].Start();
        }

        barrier.SignalAndWait();
        var beforeAllInferences = Stopwatch.GetTimestamp();
        Thread.Sleep(duration);
        Volatile.Write(ref running, 0);

        foreach (var thread in threads)
        {
            thread.Join();
        }

        var elapsedMilliseconds = ElapsedMilliseconds(beforeAllInferences);
        var totalIterations = iterationsPerThread.Sum();
        var meanCallMilliseconds = Enumerable.Range(0, threadCount)
            .Select(index => iterationsPerThread[index] == 0
                ? 0.0
                : totalMillisecondsPerThread[index] / iterationsPerThread[index])
            .ToArray();
        var throughputPerSecond = totalIterations / (elapsedMilliseconds / 1000.0);

        log($"{configuration.Name,-32};{threadCount,7};{totalIterations,10};{throughputPerSecond,20:F1};" +
            $"{meanCallMilliseconds.Min(),18:F3};{meanCallMilliseconds.Average(),18:F3};{meanCallMilliseconds.Max(),18:F3}");
    }
}

static OvCore CreateProfilingCore(ProfilingConfiguration configuration)
{
    var core = new OvCore();
    if (configuration.EnableProfiling)
    {
        core.SetProperty(DeviceName, EnableProfilingProperty, EnableProfilingValue);
    }
    if (configuration.InferenceThreadCount is { } inferenceThreadCount)
    {
        core.SetProperty(DeviceName, InferenceThreadCountProperty, inferenceThreadCount.ToString());
    }
    if (configuration.StreamCount is { } streamCount)
    {
        core.SetProperty(DeviceName, NumberOfStreamsProperty, streamCount.ToString());
    }
    return core;
}

static void WriteNodeProfileSummary(
    string configurationName,
    IReadOnlyList<NodeProfile> profilingInfo,
    Action<string> log)
{
    log(string.Empty);
    log($"## CPU node profile: `{configurationName}`");
    log(string.Empty);
    log($"| Node | Type | Execution | Status | Calls | Total [ms] | Mean [ms/call] |");
    log("|:---|:---|:---|:---|---:|---:|---:|");
    foreach (var item in profilingInfo)
    {
        log($"| {EscapeMarkdown(item.NodeName),-76} | {EscapeMarkdown(item.NodeType),-12} | " +
            $"{EscapeMarkdown(item.ExecutionType),-20} | {item.Status,-8} | {item.CallCount,5} | " +
            $"{item.RealTimeMicroseconds / 1000.0,10:F3} | {item.MeanRealTimeMilliseconds,13:F3} |");
    }
}

static string EscapeMarkdown(string value) => value.Replace("|", "\\|");

static double ElapsedMilliseconds(long beforeTimestamp) =>
    (Stopwatch.GetTimestamp() - beforeTimestamp) * 1000.0 / Stopwatch.Frequency;

sealed record ProfilingConfiguration(
    string Name,
    int? InferenceThreadCount,
    int? StreamCount,
    bool EnableProfiling);

sealed class NodeProfile
{
    public NodeProfile(OvProfilingInfo profilingInfo)
    {
        NodeName = profilingInfo.NodeName;
        NodeType = profilingInfo.NodeType;
        ExecutionType = profilingInfo.ExecutionType;
        Status = profilingInfo.Status;
    }

    public string NodeName { get; }
    public string NodeType { get; }
    public string ExecutionType { get; }
    public OvProfilingStatus Status { get; }
    public int CallCount { get; private set; }
    public long RealTimeMicroseconds { get; private set; }
    public double MeanRealTimeMilliseconds => RealTimeMicroseconds / 1000.0 / CallCount;

    public void Add(OvProfilingInfo profilingInfo)
    {
        ++CallCount;
        RealTimeMicroseconds += profilingInfo.RealTimeMicroseconds;
    }
}

static class NodeProfileExtensions
{
    public static void AddOrUpdate(this Dictionary<string, NodeProfile> nodeNameToProfile, OvProfilingInfo profilingInfo)
    {
        if (!nodeNameToProfile.TryGetValue(profilingInfo.NodeName, out var profile))
        {
            profile = new NodeProfile(profilingInfo);
            nodeNameToProfile.Add(profilingInfo.NodeName, profile);
        }
        profile.Add(profilingInfo);
    }
}
