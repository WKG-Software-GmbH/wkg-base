using System.Reflection;
using System.Runtime.CompilerServices;

namespace Wkg.Reflection;

public static class UnsafeReflection
{
    private static readonly MethodInfo _unsafeAs = typeof(Unsafe)
        .GetMethod(nameof(Unsafe.As), 1, TypeArray.Of<object>())!;

    private static readonly MethodInfo _unsafeAsFromTo = typeof(Unsafe)
        .GetMethods(BindingFlags.Static | BindingFlags.Public)
        .Where(m => m.Name is nameof(Unsafe.As) && m.GetGenericArguments().Length is 2)
        .Single();

    public static MethodInfo As<T>() => _unsafeAs.MakeGenericMethod(typeof(T));

    public static MethodInfo As(Type type) => _unsafeAs.MakeGenericMethod(type);

    public static MethodInfo As<TFrom, TTo>() => _unsafeAsFromTo.MakeGenericMethod(typeof(TFrom), typeof(TTo));

    public static MethodInfo As(Type from, Type to) => _unsafeAsFromTo.MakeGenericMethod(from, to);
}
