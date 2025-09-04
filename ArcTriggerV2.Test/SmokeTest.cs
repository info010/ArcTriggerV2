using Microsoft.VisualStudio.TestTools.UnitTesting;
using ArcTriggerV2.IB;

namespace ArcTriggerV2.Test;

[TestClass]
public class SmokeTest
{
    [TestMethod]
    public void CanConnectToTws()
    {
        var client = new IbClient();
        client.Connect();
        Assert.IsNotNull(client);
    }
}
