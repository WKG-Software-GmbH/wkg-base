namespace Wkg.Web;

/// <summary>
/// Represents a span-base route data key-value pair.
/// </summary>
public readonly ref struct RouteDataRef
{
    /// <summary>
    /// The key of the route data.
    /// </summary>
    public readonly ReadOnlySpan<char> Key;

    /// <summary>
    /// The value of the route data.
    /// </summary>
    public readonly ReadOnlySpan<char> Value;

    private RouteDataRef(ReadOnlySpan<char> key, ReadOnlySpan<char> value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    /// Creates a heap-based deep copy of the underlying data of this <see cref="RouteDataRef"/>.
    /// </summary>
    /// <returns>A new instance of <see cref="RouteData"/> with the same key and value as this <see cref="RouteDataRef"/>.</returns>
    public RouteData CreateDeepCopy() => RouteData.Create(Key.ToString(), Value.ToString());

    /// <summary>
    /// Creates a new instance of <see cref="RouteDataRef"/> with the specified <paramref name="key"/> and <paramref name="value"/>.
    /// </summary>
    /// <param name="key">The key of the route data.</param>
    /// <param name="value">The value of the route data.</param>
    /// <returns>A new instance of <see cref="RouteDataRef"/>.</returns>
    public static RouteDataRef Create(ReadOnlySpan<char> key, ReadOnlySpan<char> value) => new(key, value);

    /// <summary>
    /// Creates a new instance of <see cref="RouteDataRef"/> with the specified <paramref name="key"/> and <paramref name="value"/>.
    /// </summary>
    /// <remarks>
    /// The <paramref name="value"/> will be converted to a string using the <see cref="object.ToString"/> method.
    /// </remarks>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key of the route data.</param>
    /// <param name="value">The value of the route data.</param>
    /// <returns>A new instance of <see cref="RouteDataRef"/>.</returns>
    public static RouteDataRef Create<T>(ReadOnlySpan<char> key, T value) => new(key, value?.ToString() ?? string.Empty);

    /// <summary>
    /// Creates a heap-based deep copy of the underlying data of the specified <paramref name="routeDataRef"/>.
    /// </summary>
    /// <param name="routeDataRef">The <see cref="RouteDataRef"/> to copy.</param>
    /// <returns>A new instance of <see cref="RouteData"/> with the same key and value as the specified <paramref name="routeDataRef"/>.</returns>
    public static explicit operator RouteData(RouteDataRef routeDataRef) => routeDataRef.CreateDeepCopy();
}
