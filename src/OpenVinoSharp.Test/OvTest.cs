using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenVinoSharp.Test;

[TestClass]
public class OvTest
{
    [TestMethod]
    public void OvTest_Empty()
    {
        Ov.Empty();
    }

    [TestMethod]
    public void OvTest_MnistInferenceSmoke()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("OpenVINO runtime smoke test is only configured for Windows.");
        }

        string modelPath = Path.Combine(AppContext.BaseDirectory, "mnist-8.onnx");
        Assert.IsTrue(File.Exists(modelPath), $"Missing model at '{modelPath}'.");

        using var core = new Ov.Core();
        using var compiledModel = core.CompileModelFromFile(modelPath);
        using var inferRequest = compiledModel.CreateInferRequest();

        int inputLength = GetElementCount(inferRequest.InputShape.Span);
        float[] input = new float[inputLength];
        input[0] = 1.0f;

        float[] output = inferRequest.Infer(input);
        ReadOnlySpan<long> outputShape = inferRequest.OutputShape.Span;

        Assert.AreEqual(GetElementCount(outputShape), output.Length);
        Assert.IsTrue(outputShape.Length > 0);
        Assert.IsTrue(Array.TrueForAll(output, static value => float.IsFinite(value)));
    }

    private static int GetElementCount(ReadOnlySpan<long> dimensions)
    {
        long count = 1;
        foreach (long dimension in dimensions)
        {
            checked
            {
                count *= dimension;
            }
        }

        return checked((int)count);
    }
}
