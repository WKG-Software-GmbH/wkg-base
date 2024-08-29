using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.Common.Extensions;

namespace Wkg.Tests.Extensions.Common;

[TestClass]
public class NullableValueTypeExtensionsTests
{
    [TestMethod]
    public void IsNullOrDefaultTest()
    {
        Guid? g;

        g = null;
        Assert.IsTrue(g.IsNullOrDefault());

        g = Guid.Empty;
        Assert.IsTrue(g.IsNullOrDefault());

        g = default(Guid);
        Assert.IsTrue(g.IsNullOrDefault());

        g = Guid.NewGuid();
        Assert.IsFalse(g.IsNullOrDefault());

        g = new Guid("00000000-0000-0000-0000-000000000000");
        Assert.IsTrue(g.IsNullOrDefault());

        int? i;

        i = null;
        Assert.IsTrue(i.IsNullOrDefault());

        i = 0;
        Assert.IsTrue(i.IsNullOrDefault());

        i = 1;
        Assert.IsFalse(i.IsNullOrDefault());
    }

    [TestMethod]
    public void IsNotNullOrDefaultTest()
    {
        Guid? g;

        g = null;
        Assert.IsFalse(g.HasDefinedValue());

        g = Guid.Empty;
        Assert.IsFalse(g.HasDefinedValue());

        g = default(Guid);
        Assert.IsFalse(g.HasDefinedValue());

        g = Guid.NewGuid();
        Assert.IsTrue(g.HasDefinedValue());

        g = new Guid("00000000-0000-0000-0000-000000000000");
        Assert.IsFalse(g.HasDefinedValue());

        int? i;
        i = null;
        Assert.IsFalse(i.HasDefinedValue());

        i = 0;
        Assert.IsFalse(i.HasDefinedValue());

        i = 1;
        Assert.IsTrue(i.HasDefinedValue());
    }
}