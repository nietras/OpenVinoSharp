using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace OpenVinoSharp;

static class Throws
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw(Ov.Status status, string message)
    {
        throw new OvStatusException(status, message);
    }
}
