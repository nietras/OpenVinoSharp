using System;
using System.Collections.Generic;
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

    public unsafe IReadOnlyList<OvProfilingInfo> GetProfilingInfo()
    {
        Ov.ov_infer_request_get_profiling_info(new Ov.InferRequestHandle(handle), out var nativeProfilingInfos).Ok();
        try
        {
            var profilingInfos = new List<OvProfilingInfo>(checked((int)nativeProfilingInfos.Size));
            foreach (var profilingInfo in new ReadOnlySpan<Ov.ProfilingInfo>(
                nativeProfilingInfos.ProfilingInfos.ToPointer(), checked((int)nativeProfilingInfos.Size)))
            {
                profilingInfos.Add(new OvProfilingInfo(
                    (OvProfilingStatus)profilingInfo.Status,
                    profilingInfo.RealTime,
                    profilingInfo.CpuTime,
                    Marshal.PtrToStringUTF8(profilingInfo.NodeName) ?? string.Empty,
                    Marshal.PtrToStringUTF8(profilingInfo.ExecutionType) ?? string.Empty,
                    Marshal.PtrToStringUTF8(profilingInfo.NodeType) ?? string.Empty));
            }
            return profilingInfos;
        }
        finally
        {
            Ov.ov_profiling_info_list_free(ref nativeProfilingInfos);
        }
    }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        Ov.ov_infer_request_free(new Ov.InferRequestHandle(handle));
        return true;
    }
}
