using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using OpenVinoSharp;

const string SearchPattern = "*.onnx";
const string DeviceName = "CPU";
const string EnableProfilingProperty = "PERF_COUNT";
const string EnableProfilingValue = "YES";
const string InferenceThreadCountProperty = "INFERENCE_NUM_THREADS";
const string NumberOfStreamsProperty = "NUM_STREAMS";
const string EnableCpuPinningProperty = "ENABLE_CPU_PINNING";
const int BatchSize = 1;
const int WarmupCount = 3;
const int MinimumIterations = 10;
const int ProfilingSamples = 10;
const double TargetRunDurationMilliseconds = 1000;
var concurrentTestDuration = TimeSpan.FromSeconds(1);
int[] concurrentThreadCountsToTest = [1, 2, 4, 8, 16];
ProfilingConfiguration[] configurations =
[
    new("CPU", 16, 8, true), // 16 threads / 8 streams = 2 thread(s) per stream
    //new("CPU", null, null, false),
    // NOTE: Without -DTHREADING=SEQ custom OpenVino build this is limited to 1
    //       internal thread and does not use calling thread for inference.
    //       There does not appear to be a dynamic option directly for calling
    //       thread execution only.
    //new("CPU 1*Thread 0*Stream", 1, 0, true),
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
log($"Found {modelPaths.Length} files for '{SearchPattern}': " +
    $"{string.Join(", ", modelPaths.Select(path => $"'{path}'"))}");

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
    report($"{"Configuration",-16};BatchSize;Compile [ms];First [ms];Iterations;Mean/b [ms];Mean/s [ms]");
    var configurationToProfilingInfo = new List<(ProfilingConfiguration Configuration, IReadOnlyList<NodeProfile> ProfilingInfo)>();
    foreach (var configuration in configurations)
    {
        configurationToProfilingInfo.Add((configuration, RunModel(modelPath, configuration, report)));
    }
    report("```");

    report(string.Empty);
    report("## Concurrent app-thread scaling (single shared compiled model)");
    report("```");
    report($"{"Configuration",-16};Threads;Iterations;Throughput [calls/s];Min Mean/call [ms];Avg Mean/call [ms];Max Mean/call [ms]");
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
    using var inputTensor = inferRequest.GetInputTensor();
    var beforeFirstInference = Stopwatch.GetTimestamp();
    inferRequest.Infer();
    var firstInferenceMilliseconds = ElapsedMilliseconds(beforeFirstInference);
    using var outputTensor = inferRequest.GetOutputTensor();

    for (var warmup = 0; warmup < WarmupCount; ++warmup)
    {
        Marshal.WriteByte(inputTensor.Data, 0, (byte)warmup);
        inferRequest.Infer();
        _ = Marshal.ReadByte(outputTensor.Data);
    }

    var iterations = 0;
    var totalMilliseconds = 0.0;
    var allocatedBytesBefore = GC.GetAllocatedBytesForCurrentThread();
    while (totalMilliseconds < TargetRunDurationMilliseconds || iterations < MinimumIterations)
    {
        Marshal.WriteByte(inputTensor.Data, 0, (byte)iterations);
        var beforeInference = Stopwatch.GetTimestamp();
        inferRequest.Infer();
        _ = Marshal.ReadByte(outputTensor.Data);
        totalMilliseconds += ElapsedMilliseconds(beforeInference);
        ++iterations;
    }
    var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesBefore;

    var meanPerBatchMilliseconds = totalMilliseconds / iterations;
    log($"{configuration.Name,-16};{BatchSize,9};{compileMilliseconds,12:F3};{firstInferenceMilliseconds,10:F3};" +
        $"{iterations,10};{meanPerBatchMilliseconds,11:F3};{meanPerBatchMilliseconds / BatchSize,11:F3}");
    if (allocatedBytes != 0)
    {
        log($"WARNING: `{configuration.Name}` single-request inference allocated {allocatedBytes} managed bytes.");
    }

    if (configuration.EnableProfiling)
    {
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
    else
    {
        return [];
    }
}

static void RunModelConcurrent(
    string modelPath,
    ProfilingConfiguration configuration,
    int[] threadCounts,
    TimeSpan duration,
    Action<string> log)
{
    foreach (var threadCount in threadCounts)
    {
        using var core = CreateProfilingCore(configuration);
        using var model = core.ReadModel(modelPath);
        using var compiledModel = model.Compile(DeviceName);

        using (var warmupRequest = compiledModel.CreateInferRequest())
        {
            warmupRequest.Infer();
        }

        using var barrier = new Barrier(threadCount + 1);
        var iterationsPerThread = new long[threadCount];
        var totalMillisecondsPerThread = new double[threadCount];
        var allocatedBytesPerThread = new long[threadCount];
        var running = 1;
        var threads = new Thread[threadCount];

        for (var threadIndex = 0; threadIndex < threadCount; ++threadIndex)
        {
            var index = threadIndex;
            threads[index] = new Thread(() =>
            {
                using var inferRequest = compiledModel.CreateInferRequest();
                using var inputTensor = inferRequest.GetInputTensor();
                inferRequest.Infer();
                using var outputTensor = inferRequest.GetOutputTensor();
                for (var warmup = 0; warmup < WarmupCount; ++warmup)
                {
                    Marshal.WriteByte(inputTensor.Data, 0, (byte)warmup);
                    inferRequest.Infer();
                    _ = Marshal.ReadByte(outputTensor.Data);
                }

                barrier.SignalAndWait();
                _ = GC.GetAllocatedBytesForCurrentThread();
                Marshal.WriteByte(inputTensor.Data, 0, 0);
                var beforePrimingInference = Stopwatch.GetTimestamp();
                inferRequest.Infer();
                _ = Marshal.ReadByte(outputTensor.Data);
                _ = ElapsedMilliseconds(beforePrimingInference);

                if (Volatile.Read(ref running) != 0)
                {
                    Marshal.WriteByte(inputTensor.Data, 0, 0);
                    var beforePrimingLoopInference = Stopwatch.GetTimestamp();
                    inferRequest.Infer();
                    _ = Marshal.ReadByte(outputTensor.Data);
                    _ = ElapsedMilliseconds(beforePrimingLoopInference);
                }

                var iterations = 0L;
                var totalMilliseconds = 0.0;
                var isSteadyState = false;
                var allocatedBytesBefore = GC.GetAllocatedBytesForCurrentThread();
                while (Volatile.Read(ref running) != 0)
                {
                    Marshal.WriteByte(inputTensor.Data, 0, (byte)iterations);
                    var beforeInference = Stopwatch.GetTimestamp();
                    inferRequest.Infer();
                    _ = Marshal.ReadByte(outputTensor.Data);
                    totalMilliseconds += ElapsedMilliseconds(beforeInference);
                    ++iterations;

                    if (!isSteadyState)
                    {
                        isSteadyState = true;
                        iterations = 0;
                        totalMilliseconds = 0.0;
                        _ = GC.GetAllocatedBytesForCurrentThread();
                        allocatedBytesBefore = GC.GetAllocatedBytesForCurrentThread();
                    }
                }
                iterationsPerThread[index] = iterations;
                totalMillisecondsPerThread[index] = totalMilliseconds;
                allocatedBytesPerThread[index] = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesBefore;
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

        log($"{configuration.Name,-16};{threadCount,7};{totalIterations,10};{throughputPerSecond,20:F1};" +
            $"{meanCallMilliseconds.Min(),18:F3};{meanCallMilliseconds.Average(),18:F3};{meanCallMilliseconds.Max(),18:F3}");
        if (allocatedBytesPerThread.Any(allocatedBytes => allocatedBytes != 0))
        {
            log($"WARNING: `{configuration.Name}` concurrent inference with {threadCount} threads allocated " +
                $"managed bytes per thread: {string.Join(", ", allocatedBytesPerThread)}.");
        }
    }
}

static OvCore CreateProfilingCore(ProfilingConfiguration configuration)
{
    var core = new OvCore();
    //core.SetProperty(DeviceName, EnableCpuPinningProperty, "NO");
    core.SetProperty(DeviceName, EnableCpuPinningProperty, "ON");
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
    const string NodeHeader = "Node";
    const string TypeHeader = "Type";
    const string ExecutionHeader = "Execution";
    const string StatusHeader = "Status";
    const string CallsHeader = "Calls";
    const string TotalMillisecondsHeader = "Total [ms]";
    const string MeanMillisecondsHeader = "Mean [ms/call]";
    const int CallsWidth = 5;
    const int TotalMillisecondsWidth = 10;
    const int MeanMillisecondsWidth = 13;

    var nodeWidth = Math.Max(NodeHeader.Length, profilingInfo.Max(item => item.NodeName.Length));
    var typeWidth = Math.Max(TypeHeader.Length, profilingInfo.Max(item => item.NodeType.Length));
    var executionWidth = Math.Max(ExecutionHeader.Length, profilingInfo.Max(item => item.ExecutionType.Length));
    var statusWidth = Math.Max(StatusHeader.Length, profilingInfo.Max(item => item.Status.ToString().Length));
    var headerFormat = CompositeFormat.Parse(
        $"{{0,-{nodeWidth}}};{{1,-{typeWidth}}};{{2,-{executionWidth}}};{{3,-{statusWidth}}};" +
        $"{{4,{CallsWidth}}};{{5,{TotalMillisecondsWidth}}};{{6,{MeanMillisecondsWidth}}}");
    var rowFormat = CompositeFormat.Parse(
        $"{{0,-{nodeWidth}}};{{1,-{typeWidth}}};{{2,-{executionWidth}}};{{3,-{statusWidth}}};" +
        $"{{4,{CallsWidth}}};{{5,{TotalMillisecondsWidth}:F3}};{{6,{MeanMillisecondsWidth}:F3}}");

    log(string.Empty);
    log($"## CPU node profile: `{configurationName}`");
    log("```");
    log(string.Format(null, headerFormat,
        NodeHeader, TypeHeader, ExecutionHeader, StatusHeader,
        CallsHeader, TotalMillisecondsHeader, MeanMillisecondsHeader));
    foreach (var item in profilingInfo)
    {
        log(string.Format(null, rowFormat,
            item.NodeName, item.NodeType, item.ExecutionType, item.Status,
            item.CallCount, item.RealTimeMicroseconds / 1000.0, item.MeanRealTimeMilliseconds));
    }
    log("```");
}

static double ElapsedMilliseconds(long beforeTimestamp) =>
    (Stopwatch.GetTimestamp() - beforeTimestamp) * 1000.0 / Stopwatch.Frequency;

sealed record ProfilingConfiguration(
    string Name,
    int? InferenceThreadCount,
    int? StreamCount,
    bool EnableProfiling);

sealed class NodeProfile(OvProfilingInfo profilingInfo)
{
    public string NodeName { get; } = profilingInfo.NodeName;
    public string NodeType { get; } = profilingInfo.NodeType;
    public string ExecutionType { get; } = profilingInfo.ExecutionType;
    public OvProfilingStatus Status { get; } = profilingInfo.Status;
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
