using System;
using System.Runtime.InteropServices;

namespace OpenVinoSharp;

public sealed class OvTensor : SafeHandle
{
    readonly OvInferRequest _inferRequest;

    internal OvTensor(OvInferRequest inferRequest, Ov.TensorHandle tensor)
        : base(nint.Zero, ownsHandle: true)
    {
        ArgumentNullException.ThrowIfNull(inferRequest);
        _inferRequest = inferRequest;
        SetHandle(tensor.Value);
    }

    public nint Data
    {
        get
        {
            Ov.ov_tensor_data(new Ov.TensorHandle(handle), out var data).Ok();
            return data;
        }
    }

    public Ov.Shape GetShape()
    {
        Ov.ov_tensor_get_shape(new Ov.TensorHandle(handle), out var shape).Ok();
        return shape;
    }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        Ov.ov_tensor_free(new Ov.TensorHandle(handle));
        return true;
    }
}
