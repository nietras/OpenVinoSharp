using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenVinoSharp;

static class Throws
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw(Ov.Status status, string message)
    {
        var errorInfo = Marshal.PtrToStringUTF8(Ov.ov_get_error_info(status));
        var lastError = Marshal.PtrToStringUTF8(Ov.ov_get_last_err_msg());
        var errorMessage = string.IsNullOrWhiteSpace(errorInfo) ? message : errorInfo;
        var exceptionMessage = string.IsNullOrWhiteSpace(lastError) ? errorMessage : $"{errorMessage}: {lastError}";
        throw new OvStatusException(status, exceptionMessage);
    }
}
