using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.Common.Extensions;
using Wkg.Tests;

namespace Wkg.Tests.Extensions.Common;

[TestClass]
public class GuidExtensionsTests : BaseTest
{
    [TestMethod]
    public void ToStringBigEndianTest()
    {
        const string EXPECTED = "2e2aff92-b697-40f0-9f5e-107999392b51";
        Guid guid = new(Convert.FromHexString(EXPECTED.Replace("-", string.Empty)));
        Assert.AreEqual(EXPECTED, guid.ToStringBigEndian());
    }
}