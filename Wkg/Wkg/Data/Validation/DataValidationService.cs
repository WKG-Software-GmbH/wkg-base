using System.Text.RegularExpressions;

namespace Wkg.Data.Validation;

/// <summary>
/// Provides methods for validating data.
/// </summary>
public static partial class DataValidationService
{
    /// <summary>
    /// Checks if the given string is a valid email address.
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
    /// Checks if the given string is a valid URL.
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
}
