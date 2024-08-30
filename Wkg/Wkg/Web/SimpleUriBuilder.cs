using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Web;
using Wkg.Text;

namespace Wkg.Web;

/// <summary>
/// A lightweight URI builder that can be used to build URIs with query strings, supporting URL encoding.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public struct SimpleUriBuilder : IDisposable
{
    private const int DEFAULT_CAPACITY = 512;
    private readonly StringBuilder _builder;
    private bool _hasQuery;

    private SimpleUriBuilder(StringBuilder builder, bool hasQuery)
    {
        _builder = builder;
        _hasQuery = hasQuery;
    }

    /// <summary>
    /// Creates a new instance of <see cref="SimpleUriBuilder"/> with the specified <paramref name="baseUrl"/> and string builder <paramref name="capacity"/>.
    /// </summary>
    /// <param name="baseUrl">The base URL to start with.</param>
    /// <param name="capacity">The initial capacity of the string builder.</param>
    /// <returns>A new instance of <see cref="SimpleUriBuilder"/>.</returns>
    public static SimpleUriBuilder Create(ReadOnlySpan<char> baseUrl, int capacity = DEFAULT_CAPACITY)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity, nameof(capacity));

        StringBuilder builder = StringBuilderPool.Shared.Rent(capacity).Clear();
        if (baseUrl.Length > 0)
        {
            builder.Append(baseUrl);
        }
        bool hasQuery = baseUrl.Contains('?');
        return new SimpleUriBuilder(builder, hasQuery);
    }

    /// <summary>
    /// Appends the specified <paramref name="path"/> to the URL.
    /// </summary>
    /// <param name="path">The path to append to the URL.</param>
    /// <returns>The current instance of <see cref="SimpleUriBuilder"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the URL already has a query string.</exception>
    public readonly SimpleUriBuilder AppendPath(ReadOnlySpan<char> path)
    {
        if (_hasQuery)
        {
            throw new InvalidOperationException("Cannot append path to URL with query.");
        }
        if (path.IsEmpty)
        {
            return this;
        }
        bool builderHasDelimiter = _builder.Length > 0 && _builder[^1] == '/';
        bool pathHasDelimiter = path[0] == '/';
        _ = (builderHasDelimiter, pathHasDelimiter) switch
        {
            (true, true) => _builder.Append(path[1..]),
            (false, false) => _builder.Append('/').Append(path),
            _ => _builder.Append(path)
        };
        return this;
    }

    /// <summary>
    /// Appends a query string with the specified <paramref name="key"/> and <paramref name="value"/> to the URL.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the query string.</param>
    /// <param name="value">The value of the query string.</param>
    /// <param name="urlEncode">Determines whether the key and value should be URL encoded.</param>
    /// <returns>The current instance of <see cref="SimpleUriBuilder"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="key"/> is empty or whitespace.</exception>
    public SimpleUriBuilder AppendQuery<TValue>(ReadOnlySpan<char> key, TValue value, bool urlEncode = true) =>
        AppendQuery(key, (value?.ToString() ?? string.Empty).AsSpan(), urlEncode);

    /// <summary>
    /// Appends a query string with the specified <paramref name="key"/> and <paramref name="value"/> to the URL.
    /// </summary>
    /// <param name="key">The key of the query string.</param>
    /// <param name="value">The value of the query string.</param>
    /// <returns>The current instance of <see cref="SimpleUriBuilder"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="key"/> is empty or whitespace.</exception>
    public SimpleUriBuilder AppendQuery(ReadOnlySpan<char> key, bool value) => 
        AppendQuery(key, (value ? "true" : "false").AsSpan());

    /// <summary>
    /// Appends the specified <paramref name="routeValue"/> to the URL.
    /// </summary>
    /// <param name="routeValue">The route data to append to the URL.</param>
    /// <param name="urlEncode">Determines whether the key and value should be URL encoded.</param>
    /// <returns>The current instance of <see cref="SimpleUriBuilder"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="routeValue"/> key is empty or whitespace.</exception>
    public SimpleUriBuilder AppendQuery(RouteDataRef routeValue, bool urlEncode = true) => 
        AppendQuery(routeValue.Key, routeValue.Value, urlEncode);

    /// <summary>
    /// Appends the specified <paramref name="routeValue"/> to the URL.
    /// </summary>
    /// <param name="routeValue">The route data to append to the URL.</param>
    /// <param name="urlEncode">Determines whether the key and value should be URL encoded.</param>
    /// <returns>The current instance of <see cref="SimpleUriBuilder"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="routeValue"/> key is empty or whitespace.</exception>
    public SimpleUriBuilder AppendQuery(RouteData routeValue, bool urlEncode = true) => 
        AppendQuery(routeValue.Key.AsSpan(), routeValue.Value.AsSpan(), urlEncode);

    /// <summary>
    /// Appends a query string with the specified <paramref name="key"/> and <paramref name="value"/> to the URL.
    /// </summary>
    /// <param name="key">The key of the query string.</param>
    /// <param name="value">The value of the query string.</param>
    /// <param name="urlEncode">Determines whether the key and value should be URL encoded.</param>
    /// <returns>The current instance of <see cref="SimpleUriBuilder"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="key"/> is empty or whitespace.</exception>
    public SimpleUriBuilder AppendQuery(ReadOnlySpan<char> key, ReadOnlySpan<char> value, bool urlEncode = true)
    {
        if (key.Length == 0 || key.IsWhiteSpace())
        {
            throw new ArgumentException("Key cannot be empty or whitespace.", nameof(key));
        }
        if (!_hasQuery)
        {
            _builder.Append('?');
            _hasQuery = true;
        }
        else
        {
            _builder.Append('&');
        }
        if (!urlEncode)
        {
            _builder.Append(key);
            _builder.Append('=');
            _builder.Append(value);
            return this;
        }
        int keyByteCount = Encoding.UTF8.GetByteCount(key);
        int valueByteCount = Encoding.UTF8.GetByteCount(value);
        byte[] keyBytes = ArrayPool<byte>.Shared.Rent(keyByteCount);
        byte[] valueBytes = ArrayPool<byte>.Shared.Rent(valueByteCount);
        int keyBytesWritten = Encoding.UTF8.GetBytes(key, keyBytes);
        int valueBytesWritten = Encoding.UTF8.GetBytes(value, valueBytes);
        Debug.Assert(keyByteCount == keyBytesWritten);
        Debug.Assert(valueByteCount == valueBytesWritten);
        _builder.Append(HttpUtility.UrlEncode(keyBytes, 0, keyByteCount));
        _builder.Append('=');
        _builder.Append(HttpUtility.UrlEncode(valueBytes, 0, valueByteCount));
        ArrayPool<byte>.Shared.Return(keyBytes);
        ArrayPool<byte>.Shared.Return(valueBytes);
        return this;
    }

    /// <inheritdoc cref="Build" />
    public readonly override string ToString() => _builder.ToString();

    /// <summary>
    /// Builds the URI string.
    /// </summary>
    /// <returns>The URI string.</returns>
    public readonly string Build() => _builder.ToString();

    /// <summary>
    /// Disposes the resources used by this <see cref="SimpleUriBuilder"/>.
    /// </summary>
    public readonly void Dispose() => StringBuilderPool.Shared.Return(_builder);
}