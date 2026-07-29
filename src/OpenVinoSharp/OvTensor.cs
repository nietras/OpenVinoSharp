using System;
using System.Runtime.InteropServices;

namespace OpenVinoSharp;

public sealed unsafe class OvTensor : SafeHandle
{
    readonly OvInferRequest _inferRequest;
    readonly IntPtr _data;
    readonly int _length;

    internal OvTensor(OvInferRequest inferRequest, Ov.TensorHandle tensor)
        : base(nint.Zero, ownsHandle: true)
    {
        ArgumentNullException.ThrowIfNull(inferRequest);
        _inferRequest = inferRequest;
        Ov.ov_tensor_data(tensor, out _data).Ok();
        SetHandle(tensor.Value);
        Ov.Shape shape = default;
        try
        {
            Ov.ov_tensor_get_shape(tensor, out shape).Ok();
            long length = 1;
            foreach (var dimension in shape.Span)
            {
                checked
                {
                    length *= dimension;
                }
            }
            _length = checked((int)length);
        }
        finally
        {
            if (shape.Dimensions != null)
            {
                Ov.ov_shape_free(ref shape).Ok();
            }
        }
    }

    public IntPtr Data => _data;

    public Span<T> GetData<T>() where T : unmanaged =>
        new(_data.ToPointer(), _length);

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
