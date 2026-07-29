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
        public sealed class CompiledModel : System.IDisposable
        {
            public OpenVinoSharp.Ov.InferRequest CreateInferRequest() { }
            public void Dispose() { }
            protected override void Finalize() { }
            public float[] Infer(System.ReadOnlySpan<float> input) { }
        }
        public sealed class Core : System.IDisposable
        {
            public Core() { }
            public OpenVinoSharp.Ov.CompiledModel CompileModel(OpenVinoSharp.Ov.Model model, string deviceName = "CPU") { }
            public OpenVinoSharp.Ov.CompiledModel CompileModelFromFile(string modelPath, string deviceName = "CPU") { }
            public void Dispose() { }
            protected override void Finalize() { }
            public OpenVinoSharp.Ov.Model ReadModel(string modelPath) { }
        }
        public sealed class InferRequest : System.IDisposable
        {
            public System.ReadOnlyMemory<long> InputShape { get; }
            public System.ReadOnlyMemory<long> OutputShape { get; }
            public void Dispose() { }
            protected override void Finalize() { }
            public float[] Infer(System.ReadOnlySpan<float> input) { }
        }
        public sealed class Model : System.IDisposable
        {
            public void Dispose() { }
            protected override void Finalize() { }
        }
        public sealed class OpenVinoException : System.InvalidOperationException
        {
            public OpenVinoSharp.Ov.OvStatus Status { get; }
        }
        public enum OvStatus
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
            InvalidCParam = -14,
            UnknownCError = -15,
            NotImplementCMethod = -16,
            UnknowException = -17,
        }
    }
}
```
