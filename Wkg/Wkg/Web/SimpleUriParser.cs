using System.Diagnostics;
using System.Collections;
using Wkg.Common;

namespace Wkg.Web;

/// <summary>
/// A lightweight URI parser to extract schema, host, path, and query data from a URI string.
/// </summary>
[DebuggerDisplay("Schema = {Schema}, Host = {Host}, Path = {Path}, Query = {Query}")]
public readonly ref struct SimpleUriParser
{
    private const string SCHEMA_DELIMITER = "://";

    private readonly int _queryStartIndex;
    private readonly int _hostStartIndex;
    private readonly int _pathStartIndex;

    private SimpleUriParser(ReadOnlySpan<char> uri, int hostStartIndex, int pathStartIndex, int queryStartIndex)
    {
        Uri = uri;
        _queryStartIndex = queryStartIndex;
        _hostStartIndex = hostStartIndex;
        _pathStartIndex = pathStartIndex;
    }

    /// <summary>
    /// Parses the specified <paramref name="uri"/> string into a new instance of <see cref="SimpleUriParser"/>.
    /// </summary>
    /// <param name="uri">The URI string to parse.</param>
    /// <returns>A new instance of <see cref="SimpleUriParser"/>.</returns>
    public static SimpleUriParser Parse(ReadOnlySpan<char> uri)
    {
        if (uri.IsWhiteSpace())
        {
            return default;
        }
        int queryStartIndex = uri.IndexOf('?');
        if (queryStartIndex == -1)
        {
            queryStartIndex = uri.Length;
        }
        int hostStartIndex = uri.IndexOf(SCHEMA_DELIMITER);
        int probablePathStartIndex = hostStartIndex + SCHEMA_DELIMITER.Length;
        if (hostStartIndex == -1)
        {
            hostStartIndex = 0;
            probablePathStartIndex = 0;
        }
        int pathStartIndex = uri[probablePathStartIndex..].IndexOf('/');
        if (pathStartIndex == -1)
        {
            pathStartIndex = uri.Length;
        }
        else
        {
            pathStartIndex += probablePathStartIndex;
        }
        return new SimpleUriParser(uri, hostStartIndex, pathStartIndex, queryStartIndex);
    }

    /// <summary>
    /// The original URI string.
    /// </summary>
    public readonly ReadOnlySpan<char> Uri { get; }

    /// <summary>
    /// The schema, e.g., <c>http</c>, <c>https</c>, or <c>ftp</c>, of the URI or an empty span if no schema is present.
    /// </summary>
    public readonly ReadOnlySpan<char> Schema => Uri[.._hostStartIndex];

    /// <summary>
    /// The schema and host of the URI, the host only if no schema is present, or an empty span if neither is present, or an empty span if no schema is present, e.g., <c>http://example.com</c>, <c>example.com</c>, or an empty span (the parsed URI was a path only).
    /// </summary>
    public readonly ReadOnlySpan<char> SchemaHost => Uri[..FastMath.Min(_pathStartIndex, _queryStartIndex)];

    /// <summary>
    /// The host of the URI, e.g., <c>example.com</c>, or an empty span if no host is present (the parsed URI was a path only).
    /// </summary>
    public readonly ReadOnlySpan<char> Host
    {
        get
        {
            int hostStartIndex = _hostStartIndex;
            if (hostStartIndex != 0)
            {
                hostStartIndex += SCHEMA_DELIMITER.Length;
            }
            return Uri[hostStartIndex..FastMath.Min(_pathStartIndex, _queryStartIndex)];
        }
    }

    /// <summary>
    /// The schema, host, and path of the URI, where schema and host are omitted if not present, e.g., <c>http://example.com/path</c>, <c>example.com/path</c>, or <c>/path</c>.
    /// </summary>
    public readonly ReadOnlySpan<char> SchemaHostPath => Uri[.._queryStartIndex];

    /// <summary>
    /// The path of the URI, e.g., <c>/path</c>, or an empty span if no path is present (the parsed URI did not contain a path).
    /// </summary>
    public readonly ReadOnlySpan<char> Path => Uri[FastMath.Min(_pathStartIndex, _queryStartIndex).._queryStartIndex];

    /// <summary>
    /// The query string of the URI, e.g., <c>?key1=value1&amp;key2=value2</c>, or an empty span if no query string is present.
    /// </summary>
    public readonly ReadOnlySpan<char> Query => Uri[FastMath.Min(_queryStartIndex + 1, Uri.Length)..];

    /// <summary>
    /// Checks if the URI contains a query parameter with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key of the query parameter to check for.</param>
    public readonly bool ContainsQueryParameter(string key) => TryGetQueryParameter(key, out _);

    /// <summary>
    /// Attempts to create a new instance of <see cref="RouteDataRef"/> with the specified <paramref name="key"/> and the value of the query parameter.
    /// </summary>
    /// <param name="key">The key of the query parameter to get.</param>
    /// <param name="value">The value of the query parameter if found; otherwise, an empty reference.</param>
    /// <returns><see langword="true"/> if the query parameter was found; otherwise, <see langword="false"/>.</returns>
    public readonly bool TryGetQueryParameter(string key, out RouteDataRef value)
    {
        foreach (RouteDataRef routeValue in this)
        {
            if (routeValue.Key.Equals(key, StringComparison.Ordinal))
            {
                value = routeValue;
                return true;
            }
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Retrieves the <see cref="Enumerator"/> for the query parameters of the URI.
    /// </summary>
    /// <returns></returns>
    public readonly Enumerator GetEnumerator() => new(Query);

    /// <summary>
    /// Represents an enumerator for the query parameters of a URI.
    /// </summary>
    public ref struct Enumerator
    {
        private readonly ReadOnlySpan<char> _query;

        private int _tupleStartIndex = -1;
        private int _valueStartIndex = -1;
        private int _tupleEndIndex = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator"/> struct.
        /// </summary>
        /// <param name="query">The query string to enumerate.</param>
        internal Enumerator(ReadOnlySpan<char> query)
        {
            _query = query;
        }

        /// <inheritdoc cref="IEnumerator{T}.Current"/>
        public readonly RouteDataRef Current
        {
            get
            {
                ReadOnlySpan<char> key = _query[_tupleStartIndex..(_valueStartIndex - 1)];
                ReadOnlySpan<char> value = _query[_valueStartIndex..(_tupleEndIndex - 1)];
                return RouteDataRef.Create(key.ToString(), value.ToString());
            }
        }

        /// <inheritdoc cref="IEnumerator.MoveNext" />
        public bool MoveNext()
        {
            if (_query.Length == 0)
            {
                return false;
            }
            if (_tupleStartIndex == -1)
            {
                _tupleStartIndex = 0;
            }
            else if (_tupleEndIndex < _query.Length)
            {
                _tupleStartIndex = _tupleEndIndex;
            }
            else
            {
                return false;
            }
            int relativeValueStartIndex = _query[_tupleStartIndex..].IndexOf('=');
            if (relativeValueStartIndex == -1)
            {
                return false;
            }
            _valueStartIndex = relativeValueStartIndex + _tupleStartIndex + 1;
            int relativeTupleEndIndex = _query[_valueStartIndex..].IndexOf('&');
            if (relativeTupleEndIndex == -1)
            {
                _tupleEndIndex = _query.Length + 1;
            }
            else
            {
                _tupleEndIndex = relativeTupleEndIndex + _valueStartIndex + 1;
            }
            return true;
        }

        /// <inheritdoc cref="IEnumerator.Reset" />
        public void Reset()
        {
            _tupleStartIndex = -1;
            _valueStartIndex = -1;
            _tupleEndIndex = -1;
        }
    }
}