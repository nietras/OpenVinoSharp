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
}
