using System.Text.RegularExpressions;

namespace Wkg.Data.Validation;

/// <summary>
/// Provides methods for validating data.
/// </summary>
public static partial class DataValidationService
{
    /// <summary>
    /// Checks if the given string is a valid email address conforming to the email address format specified in RFC 5322.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <returns><see langword="true"/> if the given string is a valid email address; otherwise, <see langword="false"/>.</returns>
    public static bool IsEmailAddress(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }
        Regex regex = GetEmailRegex();
        return regex.IsMatch(email);
    }

    /// <summary>
    /// Retrieves a regular expression to validate email addresses against the email address format specified in RFC 5322.
    /// </summary>
    public static RegexDescriptor EmailAddress => new(GetEmailRegex(), EMAIL_ADDRESS_PATTERN);

    /// <summary>
    /// Checks if the given string is a valid phone number.
    /// </summary>
    /// <param name="phoneNumber">The phone number to validate.</param>
    /// <returns><see langword="true"/> if the given string is a valid phone number; otherwise, <see langword="false"/>.</returns>
    public static bool IsPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return false;
        }
        Regex regex = GetPhoneRegex();
        return regex.IsMatch(phoneNumber);
    }

    /// <summary>
    /// Retrieves a regular expression to validate phone numbers.
    /// </summary>
    public static RegexDescriptor PhoneNumber => new(GetPhoneRegex(), PHONE_NUMBER_PATTERN);

    /// <summary>
    /// Checks if the given string is a valid HTTP, HTTPS, or FTP URL as defined by RFC 3986.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns><see langword="true"/> if the given string is a valid URL; otherwise, <see langword="false"/>.</returns>
    public static bool IsUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }
        Regex regex = GetUrlRegex();
        return regex.IsMatch(url);
    }

    /// <summary>
    /// Retrieves a regular expression to validate HTTP, HTTPS, or FTP URLs against the URL format specified in RFC 3986.
    /// </summary>
    public static RegexDescriptor Url => new(GetUrlRegex(), URL_PATTERN);
}
