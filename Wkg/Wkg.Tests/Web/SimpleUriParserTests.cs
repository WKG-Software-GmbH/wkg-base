using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.Web;

namespace Wkg.Tests.Web;

[TestClass]
public class SimpleUriParserTests
{
    [TestMethod]
    public void MoveNext_Empty_Test()
    {
        string uri = string.Empty;
        SimpleUriParser parser = SimpleUriParser.Parse(uri);
        SimpleUriParser.Enumerator enumerator = parser.GetEnumerator();
        Assert.IsFalse(enumerator.MoveNext());
        Assert.AreEqual(string.Empty, parser.Uri.ToString());
        Assert.AreEqual(string.Empty, parser.SchemaHostPath.ToString());
        Assert.AreEqual(string.Empty, parser.Schema.ToString());
        Assert.AreEqual(string.Empty, parser.Host.ToString());
        Assert.AreEqual(string.Empty, parser.SchemaHost.ToString());
        Assert.AreEqual(string.Empty, parser.Path.ToString());
        Assert.AreEqual(string.Empty, parser.Query.ToString());
    }

    [TestMethod]
    public void MoveNext_NoQuery_Test()
    {
        string uri = "https://example.com";
        SimpleUriParser parser = SimpleUriParser.Parse(uri);
        SimpleUriParser.Enumerator enumerator = parser.GetEnumerator();
        Assert.IsFalse(enumerator.MoveNext());
        Assert.AreEqual(uri, parser.Uri.ToString());
        Assert.AreEqual("https://example.com", parser.SchemaHostPath.ToString());
        Assert.AreEqual("https", parser.Schema.ToString());
        Assert.AreEqual("example.com", parser.Host.ToString());
        Assert.AreEqual("https://example.com", parser.SchemaHost.ToString());
        Assert.AreEqual("https://example.com", parser.SchemaHostPath.ToString());
        Assert.AreEqual(string.Empty, parser.Path.ToString());
        Assert.AreEqual(string.Empty, parser.Query.ToString());
    }

    [TestMethod]
    public void MoveNext_OneQuery_Test()
    {
        string uri = "https://example.com?key=value";
        SimpleUriParser parser = SimpleUriParser.Parse(uri);
        SimpleUriParser.Enumerator enumerator = parser.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key", enumerator.Current.Key.ToString());
        Assert.AreEqual("value", enumerator.Current.Value.ToString());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.AreEqual(uri, parser.Uri.ToString());
        Assert.AreEqual("https://example.com", parser.SchemaHostPath.ToString());
        Assert.AreEqual("https", parser.Schema.ToString());
        Assert.AreEqual("example.com", parser.Host.ToString());
        Assert.AreEqual("https://example.com", parser.SchemaHost.ToString());
        Assert.AreEqual("https://example.com", parser.SchemaHostPath.ToString());
        Assert.AreEqual(string.Empty, parser.Path.ToString());
        Assert.AreEqual("key=value", parser.Query.ToString());
    }

    [TestMethod]
    public void MoveNext_TwoQueries_Test()
    {
        string uri = "https://example.com/foo?key1=value1&key2=value2";
        SimpleUriParser parser = SimpleUriParser.Parse(uri);
        SimpleUriParser.Enumerator enumerator = parser.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key1", enumerator.Current.Key.ToString());
        Assert.AreEqual("value1", enumerator.Current.Value.ToString());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key2", enumerator.Current.Key.ToString());
        Assert.AreEqual("value2", enumerator.Current.Value.ToString());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.AreEqual(uri, parser.Uri.ToString());
        Assert.AreEqual("https", parser.Schema.ToString());
        Assert.AreEqual("example.com", parser.Host.ToString());
        Assert.AreEqual("https://example.com", parser.SchemaHost.ToString());
        Assert.AreEqual("https://example.com/foo", parser.SchemaHostPath.ToString());
        Assert.AreEqual("/foo", parser.Path.ToString());
        Assert.AreEqual("key1=value1&key2=value2", parser.Query.ToString());
    }

    [TestMethod]
    public void MoveNext_ThreeQueries_Test()
    {
        string uri = "https://example.com?key1=value1&key2=value2&key3=value3";
        SimpleUriParser parser = SimpleUriParser.Parse(uri);
        SimpleUriParser.Enumerator enumerator = parser.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key1", enumerator.Current.Key.ToString());
        Assert.AreEqual("value1", enumerator.Current.Value.ToString());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key2", enumerator.Current.Key.ToString());
        Assert.AreEqual("value2", enumerator.Current.Value.ToString());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key3", enumerator.Current.Key.ToString());
        Assert.AreEqual("value3", enumerator.Current.Value.ToString());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.AreEqual(string.Empty, parser.Path.ToString());
        Assert.AreEqual("key1=value1&key2=value2&key3=value3", parser.Query.ToString());
    }

    [TestMethod]
    public void MoveNext_ThreeQueriesWithEmptyValue_Test()
    {
        string uri = "https://example.com?key1=&key2=value2&key3=";
        SimpleUriParser parser = SimpleUriParser.Parse(uri);
        SimpleUriParser.Enumerator enumerator = parser.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key1", enumerator.Current.Key.ToString());
        Assert.AreEqual(string.Empty, enumerator.Current.Value.ToString());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key2", enumerator.Current.Key.ToString());
        Assert.AreEqual("value2", enumerator.Current.Value.ToString());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key3", enumerator.Current.Key.ToString());
        Assert.AreEqual(string.Empty, enumerator.Current.Value.ToString());
        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public void MoveNext_ThreeQueriesWithEmptyKey_Test()
    {
        string uri = "https://example.com?=value1&=value2&=value3";
        SimpleUriParser parser = SimpleUriParser.Parse(uri);
        SimpleUriParser.Enumerator enumerator = parser.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(string.Empty, enumerator.Current.Key.ToString());
        Assert.AreEqual("value1", enumerator.Current.Value.ToString());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(string.Empty, enumerator.Current.Key.ToString());
        Assert.AreEqual("value2", enumerator.Current.Value.ToString());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(string.Empty, enumerator.Current.Key.ToString());
        Assert.AreEqual("value3", enumerator.Current.Value.ToString());
        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public void MoveNext_EmptyQuery_Test()
    {
        string uri = "https://example.com?";
        SimpleUriParser parser = SimpleUriParser.Parse(uri);
        SimpleUriParser.Enumerator enumerator = parser.GetEnumerator();
        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public void MoveNext_InvalidQuery_Test()
    {
        string uri = "https://example.com&key=value";
        SimpleUriParser parser = SimpleUriParser.Parse(uri);
        SimpleUriParser.Enumerator enumerator = parser.GetEnumerator();
        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public void NoSchemaNoPathUri_Test()
    {
        string uri = "example.com?key1=value1&key2=value2&key3=value3";
        SimpleUriParser parser = SimpleUriParser.Parse(uri);
        SimpleUriParser.Enumerator enumerator = parser.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key1", enumerator.Current.Key.ToString());
        Assert.AreEqual("value1", enumerator.Current.Value.ToString());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key2", enumerator.Current.Key.ToString());
        Assert.AreEqual("value2", enumerator.Current.Value.ToString());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key3", enumerator.Current.Key.ToString());
        Assert.AreEqual("value3", enumerator.Current.Value.ToString());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.AreEqual(uri, parser.Uri.ToString());
        Assert.AreEqual("example.com", parser.SchemaHostPath.ToString());
        Assert.AreEqual("example.com", parser.Host.ToString());
        Assert.AreEqual("example.com", parser.SchemaHost.ToString());
        Assert.AreEqual(string.Empty, parser.Schema.ToString());
        Assert.AreEqual(string.Empty, parser.Path.ToString());
    }

    [TestMethod]
    public void NoSchemaWithPathUri_Test()
    {
        string uri = "example.com:69/foo/bar?key1=value1&key2=value2&key3=value3";
        SimpleUriParser parser = SimpleUriParser.Parse(uri);
        SimpleUriParser.Enumerator enumerator = parser.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key1", enumerator.Current.Key.ToString());
        Assert.AreEqual("value1", enumerator.Current.Value.ToString());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key2", enumerator.Current.Key.ToString());
        Assert.AreEqual("value2", enumerator.Current.Value.ToString());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key3", enumerator.Current.Key.ToString());
        Assert.AreEqual("value3", enumerator.Current.Value.ToString());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.AreEqual(uri, parser.Uri.ToString());
        Assert.AreEqual("example.com:69/foo/bar", parser.SchemaHostPath.ToString());
        Assert.AreEqual("example.com:69", parser.Host.ToString());
        Assert.AreEqual("example.com:69", parser.SchemaHost.ToString());
        Assert.AreEqual(string.Empty, parser.Schema.ToString());
        Assert.AreEqual("/foo/bar", parser.Path.ToString());
    }

    [TestMethod]
    public void PathOnlyTest()
    {
        string uri = "/foo/bar?key1=value1&key2=value2&key3=value3";
        SimpleUriParser parser = SimpleUriParser.Parse(uri);
        SimpleUriParser.Enumerator enumerator = parser.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key1", enumerator.Current.Key.ToString());
        Assert.AreEqual("value1", enumerator.Current.Value.ToString());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key2", enumerator.Current.Key.ToString());
        Assert.AreEqual("value2", enumerator.Current.Value.ToString());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key3", enumerator.Current.Key.ToString());
        Assert.AreEqual("value3", enumerator.Current.Value.ToString());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.AreEqual(uri, parser.Uri.ToString());
        Assert.AreEqual("/foo/bar", parser.SchemaHostPath.ToString());
        Assert.AreEqual(string.Empty, parser.Host.ToString());
        Assert.AreEqual(string.Empty, parser.SchemaHost.ToString());
        Assert.AreEqual(string.Empty, parser.Schema.ToString());
        Assert.AreEqual("/foo/bar", parser.Path.ToString());
    }
}