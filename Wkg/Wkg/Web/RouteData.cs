namespace Wkg.Web;

/// <summary>
/// Represents a route data key-value pair.
/// </summary>
public readonly struct RouteData
{
    /// <summary>
    /// The key of the route data.
    /// </summary>
    public readonly string Key;

    /// <summary>
    /// The value of the route data.
    /// </summary>
    public readonly string Value;

    private RouteData(string key, string value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    /// Creates a new instance of <see cref="RouteData"/> with the specified <paramref name="key"/> and <paramref name="value"/>.
    /// </summary>
    /// <param name="key">The key of the route data.</param>
    /// <param name="value">The value of the route data.</param>
    /// <returns>A new instance of <see cref="RouteData"/>.</returns>
    public static RouteData Create(string key, string? value) => new(key, value ?? string.Empty);

    /// <summary>
    /// Creates a new instance of <see cref="RouteData"/> with the specified <paramref name="key"/> and <paramref name="value"/>.
    /// </summary>
    /// <remarks>
    /// The <paramref name="value"/> will be converted to a string using the <see cref="object.ToString"/> method.
    /// </remarks>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key of the route data.</param>
    /// <param name="value">The value of the route data.</param>
    /// <returns>A new instance of <see cref="RouteData"/>.</returns>
    public static RouteData Create<T>(string key, T value) => new(key, value?.ToString() ?? string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="RouteData"/> to a <see cref="RouteDataRef"/>.
    /// </summary>
    /// <param name="routeValue">The <see cref="RouteData"/> to convert.</param>
    /// <returns>A new instance of <see cref="RouteDataRef"/>.</returns>

    public static implicit operator RouteDataRef(RouteData routeValue) => RouteDataRef.Create(routeValue.Key, routeValue.Value);
}
