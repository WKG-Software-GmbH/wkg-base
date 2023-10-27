using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wkg.Tests;

namespace Wkg.Data.Validation.Tests;

[TestClass]
public class DataValidationServiceTests : BaseTest
{
    private static readonly string[] _validEmailAddresses = new string[]
    {
        "email@example.com",
        "firstname.lastname@example.com",
        "email@subdomain.example.com",
        "firstname+lastname@example.com",
        "“email”@example.com",
        "1234567890@example.com",
        "email@example-one.com",
        "_______@example.com",
        "あいうえお@example.com",
        "email@example.name",
        "email@example.museum",
        "email@example.co.jp",
        "firstname-lastname@example.com",
    };

    private static readonly string?[] _invalidEmailAddresses = new string?[]
    {
        "    ",
        null,
        string.Empty,
        "plainaddress",
        "#@%^%#$@#$@#.com",
        "@example.com",
        "Joe Smith <email@example.com>",
        "email.example.com",
        "email@example@example.com",
        ".email@example.com",
        "email.@example.com",
        "email..email@example.com",
        "email@example.com (Joe Smith)",
        "email@example",
        "email@-example.com",
        "email@111.222.333.44444",
        "email@example..com",
        "Abc..123@example.com",
        "“(),:;<>[\\]@example.com",
        "just\"not\"right @example.com",
        "this\\ is\"really\"not\\allowed @example.com",
    };

    [TestMethod]
    public void IsEmailAddressTest_ValidEmails()
    {
        foreach (string mail in _validEmailAddresses)
        {
            Assert.IsTrue(DataValidationService.IsEmailAddress(mail), mail);
        }
    }

    [TestMethod]
    public void IsEmailAddressTest_InvalidEmails()
    {
        foreach (string? mail in _invalidEmailAddresses)
        {
            Assert.IsFalse(DataValidationService.IsEmailAddress(mail), mail);
        }
    }

    private static readonly string[] _validPhoneNumbers = new string[]
    {
        "+49 123 456 789",
        "+49 123 456",
        "+1 (555) 123-4567",
        "(555) 555-5555",
        "+44 7911 123456",
        "020 7123 4567",
        "555-123-4567",
        "+81 3-1234-5678",
        "(123) 456-7890",
        "0800 123 456",
        "+49 89 12345678",
        "333-444-5555",
    };

    private static readonly string[] _invalidPhoneNumbers = new string[]
    {
        "+1 (555) 123-4567-",
        "(555) 555-5555-",
        "+44 7911 123456-",
        "020 7123 4567-",
        "555-123-4567-",
        "+81 3-1234-5678-",
        "(123) 456-7890-",
        "0800 123 456-",
        "+49 89 12345678-",
        "333-444-5555-",
        "333-444- 5555",
        "555-ABC-1234",
    };

    [TestMethod]
    public void IsPhoneNumberTest_ValidPhoneNumbers()
    {
        foreach (string phone in _validPhoneNumbers)
        {
            Assert.IsTrue(DataValidationService.IsPhoneNumber(phone), phone);
        }
    }

    [TestMethod]
    public void IsPhoneNumberTest_InvalidPhoneNumbers()
    {
        foreach (string phone in _invalidPhoneNumbers)
        {
            Assert.IsFalse(DataValidationService.IsPhoneNumber(phone), phone);
        }
    }

    private static readonly string[] _validUrls = new string[]
    {
        "https://www.example.com/",
        "https://www.example.com/path/to/resource",
        "https://www.example.com/path/to/resource?query=string",
        "https://www.example.com:8080/path/to/resource",
        "http://www.example.com/",
        "ftp://ftp.example.com/",
        "https://www.example.co.uk/",
        "https://www.example.com.br/",
        "https://example.com:8443/",
        "https://www.example.travel/",
        "https://www.example.museum/",
        "https://www.example.tel/",
        "https://www.example.info/",
        "https://www.example.xxx/",
        "https://www.example.onion/",
        "http://127.0.0.1/",
        "http://example.com/path/../file",
        "http://example.com/path/./file",
        "http://user@example.com/path/./file",
        "http://example.com/тест",
        "http://example.com/%20",
        "http://example.com/?param=value#fragment",
    };

    private static readonly string?[] _invalidUrls = new string?[]
    {
        string.Empty,
        null,
        "example.com",
        "example",
        "http://",
        "http://.",
        "http://..",
        "http://../",
        "http://example..com",
        "http://example .com:path",
        "http://example.com/path with space/file",
        "http://example.com/{}",
    };

    [TestMethod]
    public void IsUrlTest_ValidUrls()
    {
        foreach (string url in _validUrls)
        {
            Assert.IsTrue(DataValidationService.IsUrl(url), url);
        }
    }

    [TestMethod]
    public void IsUrlTest_InvalidUrls()
    {
        foreach (string? url in _invalidUrls)
        {
            Assert.IsFalse(DataValidationService.IsUrl(url), url);
        }
    }
}