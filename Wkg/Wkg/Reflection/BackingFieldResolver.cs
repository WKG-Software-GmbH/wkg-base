using System.Reflection;

namespace Wkg.Reflection;

/// <summary>
/// Provides methods to resolve backing fields of properties.
/// </summary>
public static class BackingFieldResolver
{
    /// <summary>
    /// Gets the compiler-generated backing field of the property represented by the specified <paramref name="propertyInfo"/>.
    /// </summary>
    /// <param name="propertyInfo">The <see cref="PropertyInfo"/> of the property to get the backing field of.</param>
    /// <returns>The backing field of the property represented by the specified <paramref name="propertyInfo"/> or <see langword="null"/> if the property is not compiler-generated or the backing field is not found.</returns>
    public static FieldInfo? GetBackingField(this PropertyInfo propertyInfo) =>
        propertyInfo.DeclaringType?.GetField($"<{propertyInfo.Name}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
}