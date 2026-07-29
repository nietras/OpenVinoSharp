using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System.Text;

namespace OpenVinoSharp;

public static partial class Ov
{
    private const string CpuDeviceName = "CPU";
    private const string NativeLibraryName = "openvino_c";
    private static readonly object NativeResolverLock = new();
    private static bool s_nativeResolverInitialized;

    internal static void Empty() { }

    public sealed class OpenVinoException : InvalidOperationException
    {
        internal OpenVinoException(OvStatus status, string message) : base(message) => Status = status;

        public OvStatus Status { get; }
    }

    public sealed class Core : IDisposable
    {
        private nint _handle;

        public Core()
        {
            EnsureNativeResolverInitialized();
            ThrowIfError(NativeMethods.ov_core_create(out _handle));
        }

        ~Core() => DisposeCore();

        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        public Model ReadModel(string modelPath)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);
            ThrowIfError(NativeMethods.ov_core_read_model_unicode(_handle, Path.GetFullPath(modelPath), null, out nint modelHandle));
            return new Model(modelHandle);
        }

        public CompiledModel CompileModel(Model model, string deviceName = CpuDeviceName)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(model);
            model.ThrowIfDisposed();

            byte[] deviceNameUtf8 = ToUtf8NullTerminated(deviceName);
            unsafe
            {
                fixed (byte* deviceNamePtr = deviceNameUtf8)
                {
                    ThrowIfError(NativeMethods.ov_core_compile_model(_handle,
                                                                     model.Handle,
                                                                     deviceNamePtr,
                                                                     0,
                                                                     out nint compiledModelHandle));
                    return new CompiledModel(compiledModelHandle);
                }
            }
        }

        public CompiledModel CompileModelFromFile(string modelPath, string deviceName = CpuDeviceName)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);

            byte[] deviceNameUtf8 = ToUtf8NullTerminated(deviceName);
            unsafe
            {
                fixed (byte* deviceNamePtr = deviceNameUtf8)
                {
                    ThrowIfError(NativeMethods.ov_core_compile_model_from_file_unicode(_handle,
                                                                                       Path.GetFullPath(modelPath),
                                                                                       deviceNamePtr,
                                                                                       0,
                                                                                       out nint compiledModelHandle));
                    return new CompiledModel(compiledModelHandle);
                }
            }
        }

        private void DisposeCore()
        {
            nint handle = _handle;
            if (handle == 0)
            {
                return;
            }

            _handle = 0;
            NativeMethods.ov_core_free(handle);
        }

        private void ThrowIfDisposed()
        {
            if (_handle == 0)
            {
                throw new ObjectDisposedException(nameof(Core));
            }
        }
    }

    public sealed class Model : IDisposable
    {
        private nint _handle;

        internal Model(nint handle) => _handle = handle;

        internal nint Handle => _handle;

        ~Model() => DisposeModel();

        public void Dispose()
        {
            DisposeModel();
            GC.SuppressFinalize(this);
        }

        internal void ThrowIfDisposed()
        {
            if (_handle == 0)
            {
                throw new ObjectDisposedException(nameof(Model));
            }
        }

        private void DisposeModel()
        {
            nint handle = _handle;
            if (handle == 0)
            {
                return;
            }

            _handle = 0;
            NativeMethods.ov_model_free(handle);
        }
    }

    public sealed class CompiledModel : IDisposable
    {
        private nint _handle;

        internal CompiledModel(nint handle) => _handle = handle;

        ~CompiledModel() => DisposeCompiledModel();

        public void Dispose()
        {
            DisposeCompiledModel();
            GC.SuppressFinalize(this);
        }

        public InferRequest CreateInferRequest()
        {
            ThrowIfDisposed();
            ThrowIfError(NativeMethods.ov_compiled_model_create_infer_request(_handle, out nint inferRequestHandle));
            return new InferRequest(this, inferRequestHandle);
        }

        public float[] Infer(ReadOnlySpan<float> input)
        {
            using InferRequest request = CreateInferRequest();
            return request.Infer(input);
        }

        internal void ThrowIfDisposed()
        {
            if (_handle == 0)
            {
                throw new ObjectDisposedException(nameof(CompiledModel));
            }
        }

        private void DisposeCompiledModel()
        {
            nint handle = _handle;
            if (handle == 0)
            {
                return;
            }

            _handle = 0;
            NativeMethods.ov_compiled_model_free(handle);
        }
    }

    public sealed class InferRequest : IDisposable
    {
        private readonly CompiledModel _owner;
        private readonly long[] _inputShape;
        private readonly long[] _outputShape;
        private readonly OvElementType _inputElementType;
        private readonly OvElementType _outputElementType;
        private nint _handle;

        internal InferRequest(CompiledModel owner, nint handle)
        {
            _owner = owner;
            _handle = handle;

            nint inputTensorHandle = 0;
            nint outputTensorHandle = 0;
            try
            {
                ThrowIfError(NativeMethods.ov_infer_request_get_input_tensor(_handle, out inputTensorHandle));
                _inputShape = GetTensorShape(inputTensorHandle);
                ThrowIfError(NativeMethods.ov_tensor_get_element_type(inputTensorHandle, out _inputElementType));

                ThrowIfError(NativeMethods.ov_infer_request_get_output_tensor(_handle, out outputTensorHandle));
                _outputShape = GetTensorShape(outputTensorHandle);
                ThrowIfError(NativeMethods.ov_tensor_get_element_type(outputTensorHandle, out _outputElementType));
            }
            finally
            {
                if (outputTensorHandle != 0)
                {
                    NativeMethods.ov_tensor_free(outputTensorHandle);
                }

                if (inputTensorHandle != 0)
                {
                    NativeMethods.ov_tensor_free(inputTensorHandle);
                }
            }

            if (_inputElementType != OvElementType.F32)
            {
                throw new NotSupportedException($"Only float32 input tensors are supported. Native input element type: {_inputElementType}.");
            }

            if (_outputElementType != OvElementType.F32)
            {
                throw new NotSupportedException($"Only float32 output tensors are supported. Native output element type: {_outputElementType}.");
            }
        }

        ~InferRequest() => DisposeInferRequest();

        public ReadOnlyMemory<long> InputShape => _inputShape;

        public ReadOnlyMemory<long> OutputShape => _outputShape;

        public void Dispose()
        {
            DisposeInferRequest();
            GC.SuppressFinalize(this);
        }

        public float[] Infer(ReadOnlySpan<float> input)
        {
            ThrowIfDisposed();
            int expectedInputLength = GetElementCount(_inputShape);
            if (input.Length != expectedInputLength)
            {
                throw new ArgumentException($"Expected {expectedInputLength} float values for input shape [{string.Join(", ", _inputShape)}], but received {input.Length}.", nameof(input));
            }

            nint inputTensorHandle = 0;
            nint outputTensorHandle = 0;
            unsafe
            {
                fixed (long* dimsPtr = _inputShape)
                {
                    OvShape inputShape = new() { rank = _inputShape.Length, dims = dimsPtr };
                    ThrowIfError(NativeMethods.ov_tensor_create(OvElementType.F32, inputShape, out inputTensorHandle));
                }

                try
                {
                    ThrowIfError(NativeMethods.ov_tensor_data(inputTensorHandle, out nint inputData));
                    input.CopyTo(new Span<float>(inputData.ToPointer(), input.Length));

                    ThrowIfError(NativeMethods.ov_infer_request_set_input_tensor(_handle, inputTensorHandle));
                    ThrowIfError(NativeMethods.ov_infer_request_infer(_handle));
                    ThrowIfError(NativeMethods.ov_infer_request_get_output_tensor(_handle, out outputTensorHandle));

                    long[] outputShape = GetTensorShape(outputTensorHandle);
                    int outputLength = GetElementCount(outputShape);
                    ThrowIfError(NativeMethods.ov_tensor_data(outputTensorHandle, out nint outputData));

                    float[] output = GC.AllocateUninitializedArray<float>(outputLength);
                    new ReadOnlySpan<float>(outputData.ToPointer(), outputLength).CopyTo(output);
                    return output;
                }
                finally
                {
                    if (outputTensorHandle != 0)
                    {
                        NativeMethods.ov_tensor_free(outputTensorHandle);
                    }

                    if (inputTensorHandle != 0)
                    {
                        NativeMethods.ov_tensor_free(inputTensorHandle);
                    }
                }
            }
        }

        private static unsafe long[] GetTensorShape(nint tensorHandle)
        {
            OvShape shape = default;
            try
            {
                ThrowIfError(NativeMethods.ov_tensor_get_shape(tensorHandle, out shape));
                return CopyShape(shape);
            }
            finally
            {
                if (shape.dims != null)
                {
                    ThrowIfError(NativeMethods.ov_shape_free(ref shape));
                }
            }
        }

        private static long[] CopyShape(OvShape shape)
        {
            int rank = checked((int)shape.rank);
            if (rank == 0)
            {
                return [];
            }

            long[] managedShape = GC.AllocateUninitializedArray<long>(rank);
            unsafe
            {
                new ReadOnlySpan<long>(shape.dims, rank).CopyTo(managedShape);
            }

            return managedShape;
        }

        private static int GetElementCount(ReadOnlySpan<long> shape)
        {
            long count = 1;
            foreach (long dimension in shape)
            {
                if (dimension < 0)
                {
                    throw new NotSupportedException("Dynamic tensor dimensions are not supported.");
                }

                checked
                {
                    count *= dimension;
                }
            }

            return checked((int)count);
        }

        private void DisposeInferRequest()
        {
            nint handle = _handle;
            if (handle == 0)
            {
                return;
            }

            _handle = 0;
            NativeMethods.ov_infer_request_free(handle);
        }

        private void ThrowIfDisposed()
        {
            _owner.ThrowIfDisposed();
            if (_handle == 0)
            {
                throw new ObjectDisposedException(nameof(InferRequest));
            }
        }
    }

    private static void EnsureNativeResolverInitialized()
    {
        lock (NativeResolverLock)
        {
            if (s_nativeResolverInitialized)
            {
                return;
            }

            NativeLibrary.SetDllImportResolver(typeof(Ov).Assembly, ResolveLibrary);
            if (TryGetNativeDirectory(out string? nativeDirectory))
            {
                string nativeDirectoryPath = nativeDirectory!;
                string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                string[] pathSegments = path.Split(';', StringSplitOptions.RemoveEmptyEntries);
                if (!Array.Exists(pathSegments, segment => string.Equals(segment, nativeDirectoryPath, StringComparison.OrdinalIgnoreCase)))
                {
                    Environment.SetEnvironmentVariable("PATH", string.IsNullOrEmpty(path) ? nativeDirectoryPath : $"{nativeDirectoryPath};{path}");
                }
                PreloadNativeLibraries(nativeDirectoryPath);
            }

            s_nativeResolverInitialized = true;
        }
    }

    private static nint ResolveLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, NativeLibraryName, StringComparison.Ordinal))
        {
            return 0;
        }

        if (!TryGetNativeDirectory(out string? nativeDirectory))
        {
            return 0;
        }

        string nativeDirectoryPath = nativeDirectory!;
        string fileName = OperatingSystem.IsWindows() ? $"{libraryName}.dll" : OperatingSystem.IsMacOS() ? $"lib{libraryName}.dylib" : $"lib{libraryName}.so";
        string candidate = Path.Combine(nativeDirectoryPath, fileName);
        return File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out nint handle) ? handle : 0;
    }

    private static void PreloadNativeLibraries(string nativeDirectoryPath)
    {
        if (OperatingSystem.IsWindows())
        {
            TryLoadNativeLibrary(Path.Combine(nativeDirectoryPath, "tbb12.dll"));
        }

        TryLoadNativeLibrary(Path.Combine(nativeDirectoryPath, OperatingSystem.IsWindows() ? "openvino.dll" : OperatingSystem.IsMacOS() ? "libopenvino.dylib" : "libopenvino.so"));
        TryLoadNativeLibrary(Path.Combine(nativeDirectoryPath, OperatingSystem.IsWindows() ? "openvino_c.dll" : OperatingSystem.IsMacOS() ? "libopenvino_c.dylib" : "libopenvino_c.so"));
    }

    private static void TryLoadNativeLibrary(string candidate)
    {
        if (File.Exists(candidate))
        {
            NativeLibrary.TryLoad(candidate, out _);
        }
    }

    private static bool TryGetNativeDirectory(out string? nativeDirectory)
    {
        string runtimeId;
        if (OperatingSystem.IsWindows())
        {
            runtimeId = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "win-x64",
                Architecture.X86 => "win-x86",
                Architecture.Arm64 => "win-arm64",
                _ => string.Empty,
            };
        }
        else if (OperatingSystem.IsMacOS())
        {
            runtimeId = RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "osx-x64" : string.Empty;
        }
        else if (OperatingSystem.IsLinux())
        {
            runtimeId = RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "linux-x64" : string.Empty;
        }
        else
        {
            runtimeId = string.Empty;
        }

        if (string.IsNullOrEmpty(runtimeId))
        {
            nativeDirectory = null;
            return false;
        }

        nativeDirectory = Path.Combine(AppContext.BaseDirectory, "runtimes", runtimeId, "native");
        return Directory.Exists(nativeDirectory);
    }

    private static void ThrowIfError(OvStatus status)
    {
        if (status == OvStatus.Ok)
        {
            return;
        }

        string statusMessage = PtrToStringUtf8(NativeMethods.ov_get_error_info(status)) ?? status.ToString();
        string? lastError = PtrToStringUtf8(NativeMethods.ov_get_last_err_msg());
        string message = string.IsNullOrWhiteSpace(lastError) || string.Equals(statusMessage, lastError, StringComparison.Ordinal)
            ? $"OpenVINO call failed with status {status} ({(int)status}): {statusMessage}"
            : $"OpenVINO call failed with status {status} ({(int)status}): {statusMessage} {lastError}";
        throw new OpenVinoException(status, message);
    }

    private static string? PtrToStringUtf8(nint value) => value == 0 ? null : Marshal.PtrToStringUTF8(value);

    private static byte[] ToUtf8NullTerminated(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        int byteCount = Encoding.UTF8.GetByteCount(value);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(byteCount + 1);
        try
        {
            int written = Encoding.UTF8.GetBytes(value, buffer);
            buffer[written] = 0;
            byte[] exact = GC.AllocateUninitializedArray<byte>(written + 1);
            buffer.AsSpan(0, written + 1).CopyTo(exact);
            return exact;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
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

    private enum OvElementType
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

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct OvShape
    {
        public long rank;
        public long* dims;
    }

    private static partial class NativeMethods
    {
        private const string LibraryName = "openvino_c";

        [LibraryImport(LibraryName, EntryPoint = "ov_core_create")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial OvStatus ov_core_create(out nint core);

        [LibraryImport(LibraryName, EntryPoint = "ov_core_free")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void ov_core_free(nint core);

        [LibraryImport(LibraryName, EntryPoint = "ov_core_read_model_unicode", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial OvStatus ov_core_read_model_unicode(nint core, string modelPath, string? binPath, out nint model);

        [LibraryImport(LibraryName, EntryPoint = "ov_core_compile_model")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial OvStatus ov_core_compile_model(nint core,
                                                                      nint model,
                                                                      byte* deviceName,
                                                                      nuint propertyArgsSize,
                                                                      out nint compiledModel);

        [LibraryImport(LibraryName, EntryPoint = "ov_core_compile_model_from_file_unicode", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial OvStatus ov_core_compile_model_from_file_unicode(nint core,
                                                                                         string modelPath,
                                                                                         byte* deviceName,
                                                                                         nuint propertyArgsSize,
                                                                                         out nint compiledModel);

        [LibraryImport(LibraryName, EntryPoint = "ov_model_free")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void ov_model_free(nint model);

        [LibraryImport(LibraryName, EntryPoint = "ov_compiled_model_create_infer_request")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial OvStatus ov_compiled_model_create_infer_request(nint compiledModel, out nint inferRequest);

        [LibraryImport(LibraryName, EntryPoint = "ov_compiled_model_free")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void ov_compiled_model_free(nint compiledModel);

        [LibraryImport(LibraryName, EntryPoint = "ov_infer_request_get_input_tensor")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial OvStatus ov_infer_request_get_input_tensor(nint inferRequest, out nint tensor);

        [LibraryImport(LibraryName, EntryPoint = "ov_infer_request_set_input_tensor")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial OvStatus ov_infer_request_set_input_tensor(nint inferRequest, nint tensor);

        [LibraryImport(LibraryName, EntryPoint = "ov_infer_request_infer")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial OvStatus ov_infer_request_infer(nint inferRequest);

        [LibraryImport(LibraryName, EntryPoint = "ov_infer_request_get_output_tensor")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial OvStatus ov_infer_request_get_output_tensor(nint inferRequest, out nint tensor);

        [LibraryImport(LibraryName, EntryPoint = "ov_infer_request_free")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void ov_infer_request_free(nint inferRequest);

        [LibraryImport(LibraryName, EntryPoint = "ov_tensor_create")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial OvStatus ov_tensor_create(OvElementType type, OvShape shape, out nint tensor);

        [LibraryImport(LibraryName, EntryPoint = "ov_tensor_data")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial OvStatus ov_tensor_data(nint tensor, out nint data);

        [LibraryImport(LibraryName, EntryPoint = "ov_tensor_get_shape")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial OvStatus ov_tensor_get_shape(nint tensor, out OvShape shape);

        [LibraryImport(LibraryName, EntryPoint = "ov_tensor_get_element_type")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial OvStatus ov_tensor_get_element_type(nint tensor, out OvElementType type);

        [LibraryImport(LibraryName, EntryPoint = "ov_tensor_free")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void ov_tensor_free(nint tensor);

        [LibraryImport(LibraryName, EntryPoint = "ov_shape_free")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial OvStatus ov_shape_free(ref OvShape shape);

        [LibraryImport(LibraryName, EntryPoint = "ov_get_error_info")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint ov_get_error_info(OvStatus status);

        [LibraryImport(LibraryName, EntryPoint = "ov_get_last_err_msg")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint ov_get_last_err_msg();
    }
}
