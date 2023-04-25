using System.Reflection;
using System.Runtime.CompilerServices;

namespace Wkg.Reflection;

/// <summary>
/// Provides reflective access to methods provided by the <see cref="Unsafe"/> class.
/// </summary>
/// <remarks>
/// This class is not intended to be used directly from your code. It is used for runtime code generation by dependent libraries.
/// </remarks>
public static class UnsafeReflection
{
    private static readonly MethodInfo _unsafeAs = typeof(Unsafe)
        .GetMethod(nameof(Unsafe.As), 1, TypeArray.Of<object>())!;

    private static readonly MethodInfo _unsafeAsFromTo = typeof(Unsafe)
        .GetMethods(BindingFlags.Static | BindingFlags.Public)
        .Where(m => m.Name is nameof(Unsafe.As) && m.GetGenericArguments().Length is 2)
        .Single();

    /// <summary>
    /// Gets the <see cref="MethodInfo"/> for the generic <see cref="Unsafe.As{T}(object)"/> method.
    /// </summary>
    public static MethodInfo As<T>() => _unsafeAs.MakeGenericMethod(typeof(T));

    /// <summary>
    /// Gets the <see cref="MethodInfo"/> for the generic <see cref="Unsafe.As{T}(object)"/> method with the specified type argument.
    /// </summary>
    /// <param name="type">The type argument for the generic method.</param>
    public static MethodInfo As(Type type) => _unsafeAs.MakeGenericMethod(type);

    /// <summary>
    /// Gets the <see cref="MethodInfo"/> for the generic <see cref="Unsafe.As{TFrom, TTo}(ref TFrom)"/> method.
    /// </summary>
    public static MethodInfo As<TFrom, TTo>() => _unsafeAsFromTo.MakeGenericMethod(typeof(TFrom), typeof(TTo));

    /// <summary>
    /// Gets the <see cref="MethodInfo"/> for the generic <see cref="Unsafe.As{TFrom, TTo}(ref TFrom)"/> method with the specified type arguments.
    /// </summary>
    public static MethodInfo As(Type from, Type to) => _unsafeAsFromTo.MakeGenericMethod(from, to);
}
