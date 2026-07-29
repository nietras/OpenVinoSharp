using System;
using System.Runtime.InteropServices;

namespace OpenVinoSharp;

public sealed class OvCore : SafeHandle
{
    public OvCore()
        : base(nint.Zero, ownsHandle: true)
    {
        Ov.ov_core_create(out var core).Ok();
        SetHandle(core.Value);
    }

    public OvModel ReadModel(string modelPath, string? binPath = null) => new(this, modelPath, binPath);

    internal Ov.CoreHandle CoreHandle => new(handle);

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        Ov.ov_core_free(new Ov.CoreHandle(handle));
        return true;
    }
}
