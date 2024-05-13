using System.Text.RegularExpressions;

namespace Wkg.Data.Validation;

/// <summary>
/// Describes a regular expression and its pattern.
/// </summary>
/// <param name="Regex">The regular expression.</param>
/// <param name="Pattern">The pattern of the regular expression.</param>
public readonly record struct RegexDescriptor(Regex Regex, string Pattern);
