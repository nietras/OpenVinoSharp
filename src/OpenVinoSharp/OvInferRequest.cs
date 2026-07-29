using System;
using System.Runtime.InteropServices;

namespace OpenVinoSharp;

public sealed class OvInferRequest : SafeHandle
{
    readonly OvCompiledModel _compiledModel;

    internal OvInferRequest(OvCompiledModel compiledModel)
        : base(nint.Zero, ownsHandle: true)
    {
        ArgumentNullException.ThrowIfNull(compiledModel);
        _compiledModel = compiledModel;
        Ov.ov_compiled_model_create_infer_request(compiledModel.CompiledModelHandle, out var inferRequest).Ok();
        SetHandle(inferRequest.Value);
    }

    public OvTensor GetInputTensor()
    {
        Ov.ov_infer_request_get_input_tensor(new Ov.InferRequestHandle(handle), out var tensor).Ok();
        return new OvTensor(this, tensor);
    }

    public OvTensor GetOutputTensor()
    {
        Ov.ov_infer_request_get_output_tensor(new Ov.InferRequestHandle(handle), out var tensor).Ok();
        return new OvTensor(this, tensor);
    }

    public void Infer() => Ov.ov_infer_request_infer(new Ov.InferRequestHandle(handle)).Ok();

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        Ov.ov_infer_request_free(new Ov.InferRequestHandle(handle));
        return true;
    }
}
