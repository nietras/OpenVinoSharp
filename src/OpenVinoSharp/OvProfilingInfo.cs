namespace OpenVinoSharp;

public sealed record OvProfilingInfo(
    OvProfilingStatus Status,
    long RealTimeMicroseconds,
    long CpuTimeMicroseconds,
    string NodeName,
    string ExecutionType,
    string NodeType);

public enum OvProfilingStatus
{
    NotRun,
    OptimizedOut,
    Executed,
}
