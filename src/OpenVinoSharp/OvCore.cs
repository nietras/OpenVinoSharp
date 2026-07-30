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

    public void SetProperty(string deviceName, string propertyKey, string propertyValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyKey);
        ArgumentNullException.ThrowIfNull(propertyValue);
        Ov.ov_core_set_property(new Ov.CoreHandle(handle), deviceName, propertyKey, propertyValue).Ok();
    }

    internal Ov.CoreHandle CoreHandle => new(handle);

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        Ov.ov_core_free(new Ov.CoreHandle(handle));
        return true;
    }
}
