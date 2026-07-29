using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenVinoSharp.Test;

[TestClass]
public unsafe class OvTest
{
    const string TestModelFileName = "mnist-8.onnx";

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void OvTest_GetErrorInfo()
    {
        Assert.AreEqual("general error", Marshal.PtrToStringUTF8(Ov.ov_get_error_info(Ov.Status.GENERAL_ERROR)));
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void OvTest_Raw_MnistInferenceSmoke()
    {
        var modelPath = Path.Combine(AppContext.BaseDirectory, TestModelFileName);
        Assert.IsTrue(File.Exists(modelPath), $"Missing model at '{modelPath}'.");
        Ov.CoreHandle core = default;
        Ov.CompiledModelHandle compiledModel = default;
        Ov.InferRequestHandle inferRequest = default;
        Ov.TensorHandle inputTensor = default;
        Ov.TensorHandle outputTensor = default;
        Ov.Shape inputShape = default;
        Ov.Shape outputShape = default;
        try
        {
            Ov.ov_core_create(out core).Ok();
            Ov.ov_core_compile_model_from_file(core, modelPath, "CPU", 0, out compiledModel).Ok();
            Ov.ov_compiled_model_create_infer_request(compiledModel, out inferRequest).Ok();
            Ov.ov_infer_request_get_input_tensor(inferRequest, out inputTensor).Ok();
            Ov.ov_tensor_get_shape(inputTensor, out inputShape).Ok();
            TraceShape("Input", inputShape.Span);
            var inputLength = GetElementCount(inputShape.Span);
            Ov.ov_tensor_data(inputTensor, out var inputData).Ok();
            new Span<float>(inputData.ToPointer(), inputLength).Clear();
            Ov.ov_infer_request_set_input_tensor(inferRequest, inputTensor).Ok();
            Ov.ov_infer_request_infer(inferRequest).Ok();
            Ov.ov_infer_request_get_output_tensor(inferRequest, out outputTensor).Ok();
            Ov.ov_tensor_get_shape(outputTensor, out outputShape).Ok();
            TraceShape("Output", outputShape.Span);
            var outputLength = GetElementCount(outputShape.Span);
            Ov.ov_tensor_data(outputTensor, out var outputData).Ok();
            var values = new ReadOnlySpan<float>(outputData.ToPointer(), outputLength);
            foreach (var value in values)
            {
                Assert.IsTrue(float.IsFinite(value));
            }
        }
        finally
        {
            if (outputShape.Dimensions != null)
            {
                Ov.ov_shape_free(ref outputShape).Ok();
            }
            if (inputShape.Dimensions != null)
            {
                Ov.ov_shape_free(ref inputShape).Ok();
            }
            if (outputTensor.Value != 0)
            {
                Ov.ov_tensor_free(outputTensor);
            }
            if (inputTensor.Value != 0)
            {
                Ov.ov_tensor_free(inputTensor);
            }
            if (inferRequest.Value != 0)
            {
                Ov.ov_infer_request_free(inferRequest);
            }
            if (compiledModel.Value != 0)
            {
                Ov.ov_compiled_model_free(compiledModel);
            }
            if (core.Value != 0)
            {
                Ov.ov_core_free(core);
            }
        }
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void OvTest_OvModel_MnistInferenceSmoke()
    {
        var modelPath = Path.Combine(AppContext.BaseDirectory, TestModelFileName);
        using var core = new OvCore();
        using var model = core.ReadModel(modelPath);
        using var compiledModel = model.Compile();
        using var inferRequest = compiledModel.CreateInferRequest();
        using var inputTensor = inferRequest.GetInputTensor();
        using var inputShape = inputTensor.GetShape();
        TraceShape("Input", inputShape.Span);
        var dataSpan = inputTensor.GetData<float>();
        dataSpan.Clear();
        inferRequest.Infer();
        using var outputTensor = inferRequest.GetOutputTensor();
        using var outputShape = outputTensor.GetShape();
        TraceShape("Output", outputShape.Span);
        var values = outputTensor.GetData<float>();
        foreach (var value in values)
        {
            Assert.IsTrue(float.IsFinite(value));
        }
    }

    static void TraceShape(string name, ReadOnlySpan<long> shape)
    {
        Trace.WriteLine($"{name} shape: [{string.Join(", ", shape.ToArray())}]");
    }

    static int GetElementCount(ReadOnlySpan<long> shape)
    {
        long count = 1;
        foreach (var dimension in shape)
        {
            checked
            {
                count *= dimension;
            }
        }
        return checked((int)count);
    }
}
