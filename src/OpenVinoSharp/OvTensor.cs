using System;
using System.Runtime.InteropServices;

namespace OpenVinoSharp;

public sealed class OvTensor : SafeHandle
{
    readonly OvInferRequest _inferRequest;
    readonly IntPtr _data;

    internal OvTensor(OvInferRequest inferRequest, Ov.TensorHandle tensor)
        : base(nint.Zero, ownsHandle: true)
    {
        ArgumentNullException.ThrowIfNull(inferRequest);
        _inferRequest = inferRequest;
        Ov.ov_tensor_data(tensor, out _data).Ok();
        SetHandle(tensor.Value);
    }

    public IntPtr Data => _data;

    public OvShape GetShape()
    {
        Ov.ov_tensor_get_shape(new Ov.TensorHandle(handle), out var shape).Ok();
        return new OvShape(shape);
    }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        Ov.ov_tensor_free(new Ov.TensorHandle(handle));
        return true;
    }
}
