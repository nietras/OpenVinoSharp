using System;
using System.Runtime.InteropServices;

namespace OpenVinoSharp;

public sealed class OvModel : SafeHandle
{
    readonly OvCore _core;

    internal OvModel(OvCore core, string modelPath, string? binPath)
        : base(nint.Zero, ownsHandle: true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);
        ArgumentNullException.ThrowIfNull(core);
        _core = core;
        Ov.ov_core_read_model(core.CoreHandle, modelPath, binPath, out var model).Ok();
        SetHandle(model.Value);
    }

    public OvCompiledModel Compile(string deviceName = "CPU") => new(_core, this, deviceName);

    internal Ov.ModelHandle ModelHandle => new(handle);

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        Ov.ov_model_free(new Ov.ModelHandle(handle));
        return true;
    }
}
