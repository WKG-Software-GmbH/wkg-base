using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using Wkg.Text;

namespace Wkg.Tests.Text;

[TestClass]
public class Base64StringTests
{
    [TestMethod]
    public void DecodeToUtf8TestSimpleAscii()
    {
        const string TEST_STRING = "Hello World!";
        string base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(TEST_STRING));
        string actual = Base64String.DecodeToUtf8(base64String);
        Assert.AreEqual(TEST_STRING, actual);
    }

    [TestMethod]
    public void DecodeToUtf8TestLongerAscii()
    {
        const string TEST_STRING = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed non risus. Suspendisse";
        string base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(TEST_STRING));
        string actual = Base64String.DecodeToUtf8(base64String);
        Assert.AreEqual(TEST_STRING, actual);
    }

    [TestMethod]
    public void DecodeToUtf8TestSimpleUtf8()
    {
        const string TEST_STRING = "äöüÄÖÜß";
        string base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(TEST_STRING));
        string actual = Base64String.DecodeToUtf8(base64String);
        Assert.AreEqual(TEST_STRING, actual);
    }

    [TestMethod]
    public void DecodeToUtf8TestLongerUtf8()
    {
        // some random japanese text
        const string TEST_STRING = "\u30A4\u30ED\u30CF\u30CB\u30DB\u30D8\u30C8\u0020\u30C1\u30EA\u30CC\u30EB\u30F2\u0020\u30EF\u30AB\u30E8\u30BF\u30EC\u30BD\u0020\u30C4\u30CD\u30CA\u30E9\u30E0";
        string base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(TEST_STRING));
        string actual = Base64String.DecodeToUtf8(base64String);
        Assert.AreEqual(TEST_STRING, actual);
    }

    [TestMethod()]
    public void EncodeFromUtf8TestSimpleAscii()
    {
        const string TEST_STRING = "Hello World!";
        string base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(TEST_STRING));
        string actual = Base64String.EncodeFromUtf8(TEST_STRING);
        Assert.AreEqual(base64String, actual);
    }

    [TestMethod()]
    public void EncodeFromUtf8TestLongerAscii()
    {
        const string TEST_STRING = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed non risus. Suspendisse";
        string base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(TEST_STRING));
        string actual = Base64String.EncodeFromUtf8(TEST_STRING);
        Assert.AreEqual(base64String, actual);
    }

    [TestMethod()]
    public void EncodeFromUtf8TestSimpleUtf8()
    {
        const string TEST_STRING = "äöüÄÖÜß";
        string base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(TEST_STRING));
        string actual = Base64String.EncodeFromUtf8(TEST_STRING);
        Assert.AreEqual(base64String, actual);
    }

    [TestMethod()]
    public void EncodeFromUtf8TestLongerUtf8()
    {
        // some random japanese text
        const string TEST_STRING = "\u30A4\u30ED\u30CF\u30CB\u30DB\u30D8\u30C8\u0020\u30C1\u30EA\u30CC\u30EB\u30F2\u0020\u30EF\u30AB\u30E8\u30BF\u30EC\u30BD\u0020\u30C4\u30CD\u30CA\u30E9\u30E0";
        string base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(TEST_STRING));
        string actual = Base64String.EncodeFromUtf8(TEST_STRING);
        Assert.AreEqual(base64String, actual);
    }

    [TestMethod()]
    public void EncodeFromUtf8TestEmptyString()
    {
        const string TEST_STRING = "";
        string base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(TEST_STRING));
        string actual = Base64String.EncodeFromUtf8(TEST_STRING);
        Assert.AreEqual(base64String, actual);
    }

    [TestMethod()]
    public void EncodeFromUtf8TestTransitive()
    {
        const string TEST_STRING = "Hello World!";
        string base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(TEST_STRING));
        string encoded = Base64String.EncodeFromUtf8(TEST_STRING);
        string decoded = Base64String.DecodeToUtf8(encoded);
        Assert.AreEqual(base64String, encoded);
        Assert.AreEqual(TEST_STRING, decoded);
    }
}