using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Wkg.Extensions.Common.Tests;

[TestClass]
public class GuidExtensionsTests
{
    [TestMethod]
    public void ToStringBigEndianTest()
    {
        const string expected = "2e2aff92-b697-40f0-9f5e-107999392b51";
        Guid guid = new(Convert.FromHexString(expected.Replace("-", string.Empty)));
        Assert.AreEqual(expected, guid.ToStringBigEndian());
    }
}