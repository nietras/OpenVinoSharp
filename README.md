# OpenVinoSharp
![.NET](https://img.shields.io/badge/net10.0-5C2D91?logo=.NET&labelColor=gray)
![C#](https://img.shields.io/badge/C%23-14.0-239120?labelColor=gray)
[![Build Status](https://github.com/nietras/OpenVinoSharp/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/nietras/OpenVinoSharp/actions/workflows/dotnet.yml)
[![Super-Linter](https://github.com/nietras/OpenVinoSharp/actions/workflows/super-linter.yml/badge.svg)](https://github.com/marketplace/actions/super-linter)
[![codecov](https://codecov.io/gh/nietras/OpenVinoSharp/branch/main/graph/badge.svg?token=WN56CR3X0D)](https://codecov.io/gh/nietras/OpenVinoSharp)
[![CodeQL](https://github.com/nietras/OpenVinoSharp/workflows/CodeQL/badge.svg)](https://github.com/nietras/OpenVinoSharp/actions?query=workflow%3ACodeQL)
[![Nuget](https://img.shields.io/nuget/v/OpenVinoSharp?color=purple)](https://www.nuget.org/packages/OpenVinoSharp/)
[![Release](https://img.shields.io/github/v/release/nietras/OpenVinoSharp)](https://github.com/nietras/OpenVinoSharp/releases/)
[![downloads](https://img.shields.io/nuget/dt/OpenVinoSharp)](https://www.nuget.org/packages/OpenVinoSharp)
![Size](https://img.shields.io/github/repo-size/nietras/OpenVinoSharp.svg)
[![License](https://img.shields.io/github/license/nietras/OpenVinoSharp)](https://github.com/nietras/OpenVinoSharp/blob/main/LICENSE)
[![Blog](https://img.shields.io/badge/blog-nietras.com-4993DD)](https://nietras.com)
![GitHub Repo stars](https://img.shields.io/github/stars/nietras/OpenVinoSharp?style=flat)

Low-level OpenVino interop in modern C#. Cross-platform, trimmable and
AOT/NativeAOT compatible.

⭐ Please star this project if you like it. ⭐

[Example](#example) | [Example Catalogue](#example-catalogue) | [Public API Reference](#public-api-reference)

## Example
```csharp
Ov.Empty();

// Above example code is for demonstration purposes only.
// Short names and repeated constants are only for demonstration.
```

For more examples see [Example Catalogue](#example-catalogue).

## Benchmarks
Benchmarks.

### Detailed Benchmarks

#### Comparison Benchmarks

## Example Catalogue
The following examples are available in [ReadMeTest.cs](src/OpenVinoSharp.XyzTest/ReadMeTest.cs).

### Example - Empty
```csharp
Ov.Empty();

// Above example code is for demonstration purposes only.
// Short names and repeated constants are only for demonstration.
```

## Public API Reference
```csharp
[assembly: System.CLSCompliant(false)]
[assembly: System.Reflection.AssemblyMetadata("IsAotCompatible", "True")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/nietras/OpenVinoSharp/")]
[assembly: System.Resources.NeutralResourcesLanguage("en")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("OpenVinoSharp.Benchmarks")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("OpenVinoSharp.ComparisonBenchmarks")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("OpenVinoSharp.Test")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("OpenVinoSharp.XyzTest")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName=".NET 10.0")]
namespace OpenVinoSharp
{
    public static class Ov
    {
        [System.Runtime.CompilerServices.SkipLocalsInit]
        [System.Runtime.InteropServices.LibraryImport("openvino_c")]
        public static OpenVinoSharp.Ov.Status ov_compiled_model_create_infer_request(OpenVinoSharp.Ov.CompiledModelHandle compiledModel, out OpenVinoSharp.Ov.InferRequestHandle inferRequest) { }
        [System.Runtime.InteropServices.LibraryImport("openvino_c")]
        public static void ov_compiled_model_free(OpenVinoSharp.Ov.CompiledModelHandle compiledModel) { }
        [System.Runtime.CompilerServices.SkipLocalsInit]
        [System.Runtime.InteropServices.LibraryImport("openvino_c", StringMarshalling=System.Runtime.InteropServices.StringMarshalling.Utf8)]
        public static OpenVinoSharp.Ov.Status ov_core_compile_model(OpenVinoSharp.Ov.CoreHandle core, OpenVinoSharp.Ov.ModelHandle model, string deviceName, nuint propertyArgsSize, out OpenVinoSharp.Ov.CompiledModelHandle compiledModel) { }
        [System.Runtime.CompilerServices.SkipLocalsInit]
        [System.Runtime.InteropServices.LibraryImport("openvino_c", StringMarshalling=System.Runtime.InteropServices.StringMarshalling.Utf8)]
        public static OpenVinoSharp.Ov.Status ov_core_compile_model_from_file(OpenVinoSharp.Ov.CoreHandle core, string modelPath, string deviceName, nuint propertyArgsSize, out OpenVinoSharp.Ov.CompiledModelHandle compiledModel) { }
        [System.Runtime.CompilerServices.SkipLocalsInit]
        [System.Runtime.InteropServices.LibraryImport("openvino_c")]
        public static OpenVinoSharp.Ov.Status ov_core_create(out OpenVinoSharp.Ov.CoreHandle core) { }
        [System.Runtime.InteropServices.LibraryImport("openvino_c")]
        public static void ov_core_free(OpenVinoSharp.Ov.CoreHandle core) { }
        [System.Runtime.CompilerServices.SkipLocalsInit]
        [System.Runtime.InteropServices.LibraryImport("openvino_c", StringMarshalling=System.Runtime.InteropServices.StringMarshalling.Utf8)]
        public static OpenVinoSharp.Ov.Status ov_core_read_model(OpenVinoSharp.Ov.CoreHandle core, string modelPath, string? binPath, out OpenVinoSharp.Ov.ModelHandle model) { }
        [System.Runtime.InteropServices.LibraryImport("openvino_c")]
        public static nint ov_get_last_err_msg() { }
        [System.Runtime.InteropServices.LibraryImport("openvino_c")]
        public static void ov_infer_request_free(OpenVinoSharp.Ov.InferRequestHandle inferRequest) { }
        [System.Runtime.CompilerServices.SkipLocalsInit]
        [System.Runtime.InteropServices.LibraryImport("openvino_c")]
        public static OpenVinoSharp.Ov.Status ov_infer_request_get_input_tensor(OpenVinoSharp.Ov.InferRequestHandle inferRequest, out OpenVinoSharp.Ov.TensorHandle tensor) { }
        [System.Runtime.CompilerServices.SkipLocalsInit]
        [System.Runtime.InteropServices.LibraryImport("openvino_c")]
        public static OpenVinoSharp.Ov.Status ov_infer_request_get_output_tensor(OpenVinoSharp.Ov.InferRequestHandle inferRequest, out OpenVinoSharp.Ov.TensorHandle tensor) { }
        [System.Runtime.InteropServices.LibraryImport("openvino_c")]
        public static OpenVinoSharp.Ov.Status ov_infer_request_infer(OpenVinoSharp.Ov.InferRequestHandle inferRequest) { }
        [System.Runtime.InteropServices.LibraryImport("openvino_c")]
        public static OpenVinoSharp.Ov.Status ov_infer_request_set_input_tensor(OpenVinoSharp.Ov.InferRequestHandle inferRequest, OpenVinoSharp.Ov.TensorHandle tensor) { }
        [System.Runtime.InteropServices.LibraryImport("openvino_c")]
        public static void ov_model_free(OpenVinoSharp.Ov.ModelHandle model) { }
        [System.Runtime.CompilerServices.SkipLocalsInit]
        [System.Runtime.InteropServices.LibraryImport("openvino_c")]
        public static OpenVinoSharp.Ov.Status ov_shape_free(ref OpenVinoSharp.Ov.Shape shape) { }
        [System.Runtime.CompilerServices.SkipLocalsInit]
        [System.Runtime.InteropServices.LibraryImport("openvino_c")]
        public static OpenVinoSharp.Ov.Status ov_tensor_data(OpenVinoSharp.Ov.TensorHandle tensor, out System.IntPtr data) { }
        [System.Runtime.InteropServices.LibraryImport("openvino_c")]
        public static void ov_tensor_free(OpenVinoSharp.Ov.TensorHandle tensor) { }
        [System.Runtime.CompilerServices.SkipLocalsInit]
        [System.Runtime.InteropServices.LibraryImport("openvino_c")]
        public static OpenVinoSharp.Ov.Status ov_tensor_get_shape(OpenVinoSharp.Ov.TensorHandle tensor, out OpenVinoSharp.Ov.Shape shape) { }
        public readonly struct CompiledModelHandle : System.IEquatable<OpenVinoSharp.Ov.CompiledModelHandle>
        {
            public CompiledModelHandle(nint Value) { }
            public System.IntPtr Value { get; init; }
        }
        public readonly struct CoreHandle : System.IEquatable<OpenVinoSharp.Ov.CoreHandle>
        {
            public CoreHandle(nint Value) { }
            public System.IntPtr Value { get; init; }
        }
        public enum ElementType
        {
            Dynamic = 0,
            Boolean = 1,
            Bf16 = 2,
            F16 = 3,
            F32 = 4,
            F64 = 5,
            I4 = 6,
            I8 = 7,
            I16 = 8,
            I32 = 9,
            I64 = 10,
            U1 = 11,
            U2 = 12,
            U3 = 13,
            U4 = 14,
            U6 = 15,
            U8 = 16,
            U16 = 17,
            U32 = 18,
            U64 = 19,
            Nf4 = 20,
            F8E4M3 = 21,
            F8E5M3 = 22,
            String = 23,
            F4E2M1 = 24,
            F8E8M0 = 25,
        }
        public readonly struct InferRequestHandle : System.IEquatable<OpenVinoSharp.Ov.InferRequestHandle>
        {
            public InferRequestHandle(nint Value) { }
            public System.IntPtr Value { get; init; }
        }
        public readonly struct ModelHandle : System.IEquatable<OpenVinoSharp.Ov.ModelHandle>
        {
            public ModelHandle(nint Value) { }
            public System.IntPtr Value { get; init; }
        }
        public struct Shape
        {
            public unsafe long* Dimensions;
            public long Rank;
            public System.ReadOnlySpan<long> Span { get; }
        }
        public enum Status
        {
            Ok = 0,
            GeneralError = -1,
            NotImplemented = -2,
            NetworkNotLoaded = -3,
            ParameterMismatch = -4,
            NotFound = -5,
            OutOfBounds = -6,
            Unexpected = -7,
            RequestBusy = -8,
            ResultNotReady = -9,
            NotAllocated = -10,
            InferNotStarted = -11,
            NetworkNotRead = -12,
            InferCancelled = -13,
            InvalidCParameter = -14,
            UnknownCError = -15,
            NotImplementedCMethod = -16,
            UnknownException = -17,
        }
        public readonly struct TensorHandle : System.IEquatable<OpenVinoSharp.Ov.TensorHandle>
        {
            public TensorHandle(nint Value) { }
            public System.IntPtr Value { get; init; }
        }
        extension(OpenVinoSharp.Ov.Status result)
        {
            public void Ok() { }
            public bool IsOk() { }
            public bool IsError() { }
            public string ToStringFast() { }
        }
    }
    public sealed class OvCompiledModel : System.Runtime.InteropServices.SafeHandle
    {
        public override bool IsInvalid { get; }
        public OpenVinoSharp.OvInferRequest CreateInferRequest() { }
        protected override bool ReleaseHandle() { }
    }
    public sealed class OvCore : System.Runtime.InteropServices.SafeHandle
    {
        public OvCore() { }
        public override bool IsInvalid { get; }
        public OpenVinoSharp.OvModel ReadModel(string modelPath, string? binPath = null) { }
        protected override bool ReleaseHandle() { }
    }
    [System.Serializable]
    public class OvException : System.Exception
    {
        public OvException(string message) { }
    }
    public sealed class OvInferRequest : System.Runtime.InteropServices.SafeHandle
    {
        public override bool IsInvalid { get; }
        public OpenVinoSharp.OvTensor GetInputTensor() { }
        public OpenVinoSharp.OvTensor GetOutputTensor() { }
        public void Infer() { }
        protected override bool ReleaseHandle() { }
    }
    public sealed class OvModel : System.Runtime.InteropServices.SafeHandle
    {
        public override bool IsInvalid { get; }
        public OpenVinoSharp.OvCompiledModel Compile(string deviceName = "CPU") { }
        protected override bool ReleaseHandle() { }
    }
    public sealed class OvShape : System.Runtime.InteropServices.SafeHandle
    {
        public override bool IsInvalid { get; }
        public long Rank { get; }
        public System.ReadOnlySpan<long> Span { get; }
        protected override bool ReleaseHandle() { }
    }
    [System.Serializable]
    public sealed class OvStatusException : OpenVinoSharp.OvException
    {
        public OvStatusException(OpenVinoSharp.Ov.Status status, string message) { }
        public OpenVinoSharp.Ov.Status Status { get; }
    }
    public sealed class OvTensor : System.Runtime.InteropServices.SafeHandle
    {
        public System.IntPtr Data { get; }
        public override bool IsInvalid { get; }
        public System.Span<T> GetData<T>()
            where T :  unmanaged { }
        public OpenVinoSharp.OvShape GetShape() { }
        protected override bool ReleaseHandle() { }
    }
}
```
