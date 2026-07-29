using System;

namespace OpenVinoSharp;

[Serializable]
public class OvException(string message) : Exception(message);

[Serializable]
public sealed class OvStatusException(Ov.Status status, string message) : Exception(message)
{
    public Ov.Status Status { get; } = status;
}
