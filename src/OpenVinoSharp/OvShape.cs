using System;
using System.Runtime.InteropServices;

namespace OpenVinoSharp;

public sealed unsafe class OvShape : SafeHandle
{
    internal OvShape(Ov.Shape shape)
        : base(nint.Zero, ownsHandle: true)
    {
        Rank = shape.Rank;
        SetHandle((nint)shape.Dimensions);
    }

    public long Rank { get; }

    public ReadOnlySpan<long> Span => new((long*)handle, checked((int)Rank));

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        var shape = new Ov.Shape { Rank = Rank, Dimensions = (long*)handle };
        return Ov.ov_shape_free(ref shape).IsOk();
    }
}
