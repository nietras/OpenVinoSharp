using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenVinoSharp.Test;

[TestClass]
public unsafe class OvTest
{
    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void OvTest_MnistInferenceSmoke()
    {
        var modelPath = Path.Combine(AppContext.BaseDirectory, "mnist-8.onnx");
        Assert.IsTrue(File.Exists(modelPath), $"Missing model at '{modelPath}'.");
        Ov.CoreHandle core = default;
        Ov.CompiledModelHandle compiledModel = default;
        Ov.InferRequestHandle inferRequest = default;
        Ov.TensorHandle inputTensor = default;
        Ov.TensorHandle outputTensor = default;
        Ov.Shape outputShape = default;
        try
        {
            AssertStatus(Ov.ov_core_create(out core));
            AssertStatus(Ov.ov_core_compile_model_from_file(core, modelPath, "CPU", 0, out compiledModel));
            AssertStatus(Ov.ov_compiled_model_create_infer_request(compiledModel, out inferRequest));
            AssertStatus(Ov.ov_infer_request_get_input_tensor(inferRequest, out inputTensor));
            AssertStatus(Ov.ov_tensor_data(inputTensor, out var inputData));
            ((float*)inputData)[0] = 1.0f / byte.MaxValue;
            AssertStatus(Ov.ov_infer_request_set_input_tensor(inferRequest, inputTensor));
            AssertStatus(Ov.ov_infer_request_infer(inferRequest));
            AssertStatus(Ov.ov_infer_request_get_output_tensor(inferRequest, out outputTensor));
            AssertStatus(Ov.ov_tensor_get_shape(outputTensor, out outputShape));
            var outputLength = GetElementCount(outputShape);
            AssertStatus(Ov.ov_tensor_data(outputTensor, out var outputData));
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
                Assert.AreEqual(Ov.Status.Ok, Ov.ov_shape_free(ref outputShape));
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
    private static void AssertStatus(Ov.Status status)
    {
        Assert.AreEqual(Ov.Status.Ok, status, Marshal.PtrToStringUTF8(Ov.ov_get_last_err_msg()));
    }
    private static int GetElementCount(Ov.Shape shape)
    {
        long count = 1;
        foreach (var dimension in new ReadOnlySpan<long>(shape.Dimensions, checked((int)shape.Rank)))
        {
            checked
            {
                count *= dimension;
            }
        }
        return checked((int)count);
    }
}
