using System.Text;

namespace Wkg.Common.Extensions;

/// <summary>
/// Contains extension methods for instances of <see cref="StringBuilder"/>.
/// </summary>
public static class StringBuilderExtensions
{
    /// <summary>
    /// Appends the specified <paramref name="indentLevel"/> number of spaces (* 2) to the specified <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="StringBuilder"/> to append to.</param>
    /// <param name="indentLevel">The number of spaces (* 2) to append.</param>
    /// <returns>The specified <paramref name="builder"/>.</returns>
    public static StringBuilder AppendIndent(this StringBuilder builder, int indentLevel)
    {
        builder.Append(' ', indentLevel * 2);
        return builder;
    }
}
