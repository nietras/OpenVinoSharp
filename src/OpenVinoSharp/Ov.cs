using System;
using System.Runtime.InteropServices;
#pragma warning disable CA1401 // P/Invokes should not be visible

namespace OpenVinoSharp;

public static partial class Ov
{
    const string LibraryName = "openvino_c";

    public static void Empty() { }

    public readonly record struct CoreHandle(nint Value);
    public readonly record struct ModelHandle(nint Value);
    public readonly record struct CompiledModelHandle(nint Value);
    public readonly record struct InferRequestHandle(nint Value);
    public readonly record struct TensorHandle(nint Value);

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Shape
    {
        public long Rank;
        public long* Dimensions;
        public readonly ReadOnlySpan<long> Span => new(Dimensions, checked((int)Rank));
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

    extension(Status result)
    {
        public void Ok()
        {
            if (result != Status.Ok)
            {
                Throws.Throw(result, result.ToStringFast());
            }
        }
        public bool IsOk() => result == Status.Ok;
        public bool IsError() => result != Status.Ok;
        public string ToStringFast() => result switch
        {
            Status.Ok => nameof(Status.Ok),
            Status.GeneralError => nameof(Status.GeneralError),
            Status.NotImplemented => nameof(Status.NotImplemented),
            Status.NetworkNotLoaded => nameof(Status.NetworkNotLoaded),
            Status.ParameterMismatch => nameof(Status.ParameterMismatch),
            Status.NotFound => nameof(Status.NotFound),
            Status.OutOfBounds => nameof(Status.OutOfBounds),
            Status.Unexpected => nameof(Status.Unexpected),
            Status.RequestBusy => nameof(Status.RequestBusy),
            Status.ResultNotReady => nameof(Status.ResultNotReady),
            Status.NotAllocated => nameof(Status.NotAllocated),
            Status.InferNotStarted => nameof(Status.InferNotStarted),
            Status.NetworkNotRead => nameof(Status.NetworkNotRead),
            Status.InferCancelled => nameof(Status.InferCancelled),
            Status.InvalidCParameter => nameof(Status.InvalidCParameter),
            Status.UnknownCError => nameof(Status.UnknownCError),
            Status.NotImplementedCMethod => nameof(Status.NotImplementedCMethod),
            Status.UnknownException => nameof(Status.UnknownException),
            _ => $"Unknown:{(int)result}",
        };
    }

    public enum ElementType
    {
        Dynamic = 0,
        Boolean,
        Bf16,
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
        Nf4,
        F8E4M3,
        F8E5M3,
        String,
        F4E2M1,
        F8E8M0,
    }

    [LibraryImport(LibraryName)]
    public static partial Status ov_core_create(out CoreHandle core);
    [LibraryImport(LibraryName)]
    public static partial void ov_core_free(CoreHandle core);
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial Status ov_core_read_model(CoreHandle core,
        string modelPath, string? binPath, out ModelHandle model);
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial Status ov_core_compile_model_from_file(CoreHandle core,
        string modelPath, string deviceName, nuint propertyArgsSize, out CompiledModelHandle compiledModel);
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial Status ov_core_compile_model(CoreHandle core, ModelHandle model,
        string deviceName, nuint propertyArgsSize, out CompiledModelHandle compiledModel);
    [LibraryImport(LibraryName)]
    public static partial void ov_model_free(ModelHandle model);
    [LibraryImport(LibraryName)]
    public static partial Status ov_compiled_model_create_infer_request(CompiledModelHandle compiledModel, out InferRequestHandle inferRequest);
    [LibraryImport(LibraryName)]
    public static partial void ov_compiled_model_free(CompiledModelHandle compiledModel);
    [LibraryImport(LibraryName)]
    public static partial Status ov_infer_request_get_input_tensor(InferRequestHandle inferRequest, out TensorHandle tensor);
    [LibraryImport(LibraryName)]
    public static partial Status ov_infer_request_set_input_tensor(InferRequestHandle inferRequest, TensorHandle tensor);
    [LibraryImport(LibraryName)]
    public static partial Status ov_infer_request_infer(InferRequestHandle inferRequest);
    [LibraryImport(LibraryName)]
    public static partial Status ov_infer_request_get_output_tensor(InferRequestHandle inferRequest, out TensorHandle tensor);
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
    public static partial nint ov_get_last_err_msg();
}
