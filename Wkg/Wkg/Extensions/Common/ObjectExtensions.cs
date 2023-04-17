using System.Runtime.CompilerServices;

namespace Wkg.Extensions.Common;

/// <summary>
/// Provides extension methods for instances of <see cref="object"/>.
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    /// Casts the specified <paramref name="obj"/> to the specified <typeparamref name="TInterface"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is designed to be used in cases where non-public interface members are accessed and replaces the need for excessive brackets when casting.
    /// </para>
    /// <para>
    /// This method does not perform any type checks and is intended to be used in cases where the type of <paramref name="obj"/> is known at compile time.
    /// </para>
    /// </remarks>
    /// <typeparam name="TInterface">The type to cast the specified <paramref name="obj"/> to.</typeparam>
    /// <param name="obj">The object to cast.</param>
    /// <returns>The specified <paramref name="obj"/> cast to the specified <typeparamref name="TInterface"/>.</returns>
    /// <exception cref="InvalidCastException">The specified <paramref name="obj"/> cannot be cast to the specified <typeparamref name="TInterface"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TInterface To<TInterface>(this object obj) => (TInterface)obj;
}