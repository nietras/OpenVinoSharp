using System;
using System.Runtime.InteropServices;

namespace OpenVinoSharp;

public sealed class OvCompiledModel : SafeHandle
{
    readonly OvCore _core;
    readonly OvModel _model;

    internal OvCompiledModel(OvCore core, OvModel model, string deviceName)
        : base(nint.Zero, ownsHandle: true)
    {
        ArgumentNullException.ThrowIfNull(core);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceName);
        _core = core;
        _model = model;
        Ov.ov_core_compile_model(core.CoreHandle, model.ModelHandle, deviceName, 0, out var compiledModel).Ok();
        SetHandle(compiledModel.Value);
    }

    public OvInferRequest CreateInferRequest() => new(this);

    internal Ov.CompiledModelHandle CompiledModelHandle => new(handle);

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        Ov.ov_compiled_model_free(new Ov.CompiledModelHandle(handle));
        return true;
    }
}
