using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq.Expressions;
using System.Reflection;

namespace Wkg.Reflection.Tests;

[TestClass]
public class ExpressionExtensionsTests
{
    [TestMethod]
    public void GetPropertyAccessListTest1()
    {
        LambdaExpression expression = CreateExpression(x => x.SimpleProperty);
        List<PropertyInfo> result = expression.GetPropertyAccessList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(typeof(TestClass).GetProperty(nameof(TestClass.SimpleProperty)), result[0]);
        Assert.AreEqual(nameof(TestClass.SimpleProperty), result[0].Name);
    }

    [TestMethod]
    public void GetPropertyAccessListTest2()
    {
        LambdaExpression expression = CreateExpression(x => x.NestedProperty.SimpleProperty);
        List<PropertyInfo> result = expression.GetPropertyAccessList();
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(nameof(TestClass.NestedProperty), result[0].Name);
        Assert.AreEqual(nameof(TestClass.SimpleProperty), result[1].Name);
    }

    [TestMethod]
    public void GetPropertyAccessListTest3()
    {
        LambdaExpression expression = CreateExpression(x => x.NestedProperty.NestedProperty.SimpleProperty);
        List<PropertyInfo> result = expression.GetPropertyAccessList();
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual(nameof(TestClass.NestedProperty), result[0].Name);
        Assert.AreEqual(nameof(TestClass.NestedProperty), result[1].Name);
        Assert.AreEqual(nameof(TestClass.SimpleProperty), result[2].Name);
    }

    private static LambdaExpression CreateExpression<TProperty>(Expression<Func<TestClass, TProperty>> expression) => expression;
}

internal class TestClass
{
    public int SimpleProperty { get; set; }

    public TestClass NestedProperty => this;
}