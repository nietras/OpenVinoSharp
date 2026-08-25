using System;
using System.Runtime.InteropServices;
#pragma warning disable CA1401 // P/Invokes should not be visible

namespace OpenVinoSharp;

public static partial class Ov
{
    const string LibraryName = "openvino_c";

    public readonly record struct CoreHandle(nint Value);
    public readonly record struct ModelHandle(nint Value);
    public readonly record struct CompiledModelHandle(nint Value);
    public readonly record struct InferRequestHandle(nint Value);
    public readonly record struct TensorHandle(nint Value);

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProfilingInfo
    {
        public int Status;
        public long RealTime;
        public long CpuTime;
        public nint NodeName;
        public nint ExecutionType;
        public nint NodeType;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProfilingInfoList
    {
        public nint ProfilingInfos;
        public nuint Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Shape
    {
        public long Rank;
        public long* Dimensions;
        public readonly ReadOnlySpan<long> Span => new(Dimensions, checked((int)Rank));
    }

    public enum Status
    {
        OK = 0,
        GENERAL_ERROR = -1,
        NOT_IMPLEMENTED = -2,
        NETWORK_NOT_LOADED = -3,
        PARAMETER_MISMATCH = -4,
        NOT_FOUND = -5,
        OUT_OF_BOUNDS = -6,
        UNEXPECTED = -7,
        REQUEST_BUSY = -8,
        RESULT_NOT_READY = -9,
        NOT_ALLOCATED = -10,
        INFER_NOT_STARTED = -11,
        NETWORK_NOT_READ = -12,
        INFER_CANCELLED = -13,
        INVALID_C_PARAM = -14,
        UNKNOWN_C_ERROR = -15,
        NOT_IMPLEMENT_C_METHOD = -16,
        UNKNOW_EXCEPTION = -17,
    }

    extension(Status result)
    {
        public void Ok()
        {
            if (result != Status.OK)
            {
                Throws.Throw(result, result.ToStringFast());
            }
        }
        public bool IsOk() => result == Status.OK;
        public bool IsError() => result != Status.OK;
        public string ToStringFast() => result switch
        {
            Status.OK => nameof(Status.OK),
            Status.GENERAL_ERROR => nameof(Status.GENERAL_ERROR),
            Status.NOT_IMPLEMENTED => nameof(Status.NOT_IMPLEMENTED),
            Status.NETWORK_NOT_LOADED => nameof(Status.NETWORK_NOT_LOADED),
            Status.PARAMETER_MISMATCH => nameof(Status.PARAMETER_MISMATCH),
            Status.NOT_FOUND => nameof(Status.NOT_FOUND),
            Status.OUT_OF_BOUNDS => nameof(Status.OUT_OF_BOUNDS),
            Status.UNEXPECTED => nameof(Status.UNEXPECTED),
            Status.REQUEST_BUSY => nameof(Status.REQUEST_BUSY),
            Status.RESULT_NOT_READY => nameof(Status.RESULT_NOT_READY),
            Status.NOT_ALLOCATED => nameof(Status.NOT_ALLOCATED),
            Status.INFER_NOT_STARTED => nameof(Status.INFER_NOT_STARTED),
            Status.NETWORK_NOT_READ => nameof(Status.NETWORK_NOT_READ),
            Status.INFER_CANCELLED => nameof(Status.INFER_CANCELLED),
            Status.INVALID_C_PARAM => nameof(Status.INVALID_C_PARAM),
            Status.UNKNOWN_C_ERROR => nameof(Status.UNKNOWN_C_ERROR),
            Status.NOT_IMPLEMENT_C_METHOD => nameof(Status.NOT_IMPLEMENT_C_METHOD),
            Status.UNKNOW_EXCEPTION => nameof(Status.UNKNOW_EXCEPTION),
            _ => $"Unknown:{(int)result}",
        };
    }

    public enum ElementType
    {
        DYNAMIC = 0,
        BOOLEAN,
        BF16,
        F16,
        F32,
        F64,
        I4,
        I8,
        I16,
        I32,
        I64,
        U1,
        U2,
        U3,
        U4,
        U6,
        U8,
        U16,
        U32,
        U64,
        NF4,
        F8E4M3,
        F8E5M3,
        STRING,
        F4E2M1,
        F8E8M0,
    }

    [LibraryImport(LibraryName)]
    public static partial Status ov_core_create(out CoreHandle core);
    [LibraryImport(LibraryName)]
    public static partial void ov_core_free(CoreHandle core);
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial Status ov_core_set_property(CoreHandle core, string deviceName,
        string propertyKey, string propertyValue);
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial Status ov_core_read_model(CoreHandle core,
        string modelPath, string? binPath, out ModelHandle model);
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial Status ov_core_compile_model_from_file(CoreHandle core,
        string modelPath, string deviceName, nuint propertyArgsSize,
        out CompiledModelHandle compiledModel);
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial Status ov_core_compile_model(CoreHandle core, ModelHandle model,
        string deviceName, nuint propertyArgsSize, out CompiledModelHandle compiledModel);
    [LibraryImport(LibraryName)]
    public static partial void ov_model_free(ModelHandle model);
    [LibraryImport(LibraryName)]
    public static partial Status ov_compiled_model_create_infer_request(
        CompiledModelHandle compiledModel, out InferRequestHandle inferRequest);
    [LibraryImport(LibraryName)]
    public static partial void ov_compiled_model_free(CompiledModelHandle compiledModel);
    [LibraryImport(LibraryName)]
    public static partial Status ov_infer_request_get_input_tensor(
        InferRequestHandle inferRequest, out TensorHandle tensor);
    [LibraryImport(LibraryName)]
    public static partial Status ov_infer_request_set_input_tensor(
        InferRequestHandle inferRequest, TensorHandle tensor);
    [LibraryImport(LibraryName)]
    public static partial Status ov_infer_request_infer(InferRequestHandle inferRequest);
    [LibraryImport(LibraryName)]
    internal static partial Status ov_infer_request_get_profiling_info(
        InferRequestHandle inferRequest, out ProfilingInfoList profilingInfos);
    [LibraryImport(LibraryName)]
    internal static partial void ov_profiling_info_list_free(ref ProfilingInfoList profilingInfos);
    [LibraryImport(LibraryName)]
    public static partial Status ov_infer_request_get_output_tensor(
        InferRequestHandle inferRequest, out TensorHandle tensor);
    [LibraryImport(LibraryName)]
    public static partial void ov_infer_request_free(InferRequestHandle inferRequest);
    [LibraryImport(LibraryName)]
    public static partial Status ov_tensor_data(TensorHandle tensor, out nint data);
    [LibraryImport(LibraryName)]
    public static partial Status ov_tensor_get_shape(TensorHandle tensor, out Shape shape);
    [LibraryImport(LibraryName)]
    public static partial void ov_tensor_free(TensorHandle tensor);
    [LibraryImport(LibraryName)]
    public static partial Status ov_shape_free(ref Shape shape);
    [LibraryImport(LibraryName)]
    public static partial nint ov_get_error_info(Status status);
    [LibraryImport(LibraryName)]
    public static partial nint ov_get_last_err_msg();
}
