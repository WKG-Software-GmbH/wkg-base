namespace Wkg.Logging;

/// <summary>
/// Represents compile-time information about the caller.
/// </summary>
/// <param name="filePath">The path of the source file that contains the caller. This is the file path at compile time.</param>
/// <param name="memberName">The name of the member that contains the caller. This is the method name at compile time.</param>
/// <param name="lineNumber">The line number in the source file at which the method is called.</param>
public readonly struct CallerInfo(string filePath, string memberName, int lineNumber)
{
    /// <summary>
    /// Gets the name of the source file that contains the caller. This is the file path at compile time.
    /// </summary>
    public readonly string FilePath = filePath;
    /// <summary>
    /// Gets the name of the member that contains the caller. This is the method name at compile time.
    /// </summary>
    public readonly string MemberName = memberName;
    /// <summary>
    /// Gets the line number in the source file at which the method is called.
    /// </summary>
    public readonly int LineNumber = lineNumber;

    /// <summary>
    /// Returns the index of the start of the file name in the <see cref="FilePath"/>.
    /// </summary>
    public int GetFileNameStartIndex()
    {
        Span<char> direcoryChars = ['/', '\\'];
        return FilePath.AsSpan().LastIndexOfAny(direcoryChars) + 1;
    }
}
