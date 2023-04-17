namespace Wkg.Reflection;

/// <summary>
/// Provides methods to create an array of <see cref="Type"/>s.
/// </summary>
public static class TypeArray
{
    /// <summary>
    /// Creates an array of <see cref="Type"/>s with a single element.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <returns>An array of <see cref="Type"/>s with a single element.</returns>
    public static Type[] Of<T1>() => new[] { typeof(T1) };
    /// <summary>
    /// Creates an array of <see cref="Type"/>s with two elements.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <returns>An array of <see cref="Type"/>s with two elements.</returns>
    public static Type[] Of<T1, T2>() => new[] { typeof(T1), typeof(T2) };
    /// <summary>
    /// Creates an array of <see cref="Type"/>s with three elements.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <typeparam name="T3">The type of the third element.</typeparam>
    /// <returns>An array of <see cref="Type"/>s with three elements.</returns>
    public static Type[] Of<T1, T2, T3>() => new[] { typeof(T1), typeof(T2), typeof(T3) };
    /// <summary>
    /// Creates an array of <see cref="Type"/>s with four elements.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <typeparam name="T3">The type of the third element.</typeparam>
    /// <typeparam name="T4">The type of the fourth element.</typeparam>
    /// <returns>An array of <see cref="Type"/>s with four elements.</returns>
    public static Type[] Of<T1, T2, T3, T4>() => new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) };
    /// <summary>
    /// Creates an array of <see cref="Type"/>s with five elements.
    /// </summary>
    ///     /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <typeparam name="T3">The type of the third element.</typeparam>
    /// <typeparam name="T4">The type of the fourth element.</typeparam>
    /// <typeparam name="T5">The type of the fifth element.</typeparam>
    /// <returns>An array of <see cref="Type"/>s with five elements.</returns>
    public static Type[] Of<T1, T2, T3, T4, T5>() => new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) };
    /// <summary>
    /// Creates an array of <see cref="Type"/>s with six elements.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <typeparam name="T3">The type of the third element.</typeparam>
    /// <typeparam name="T4">The type of the fourth element.</typeparam>
    /// <typeparam name="T5">The type of the fifth element.</typeparam>
    /// <typeparam name="T6">The type of the sixth element.</typeparam>
    /// <returns>An array of <see cref="Type"/>s with six elements.</returns>
    public static Type[] Of<T1, T2, T3, T4, T5, T6>() => new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) };
    /// <summary>
    /// Creates an array of <see cref="Type"/>s with seven elements.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <typeparam name="T3">The type of the third element.</typeparam>
    /// <typeparam name="T4">The type of the fourth element.</typeparam>
    /// <typeparam name="T5">The type of the fifth element.</typeparam>
    /// <typeparam name="T6">The type of the sixth element.</typeparam>
    /// <typeparam name="T7">The type of the seventh element.</typeparam>
    /// <returns>An array of <see cref="Type"/>s with seven elements.</returns>
    public static Type[] Of<T1, T2, T3, T4, T5, T6, T7>() => new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7) };
    /// <summary>
    /// Creates an array of <see cref="Type"/>s with eight elements.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <typeparam name="T3">The type of the third element.</typeparam>
    /// <typeparam name="T4">The type of the fourth element.</typeparam>
    /// <typeparam name="T5">The type of the fifth element.</typeparam>
    /// <typeparam name="T6">The type of the sixth element.</typeparam>
    /// <typeparam name="T7">The type of the seventh element.</typeparam>
    /// <typeparam name="T8">The type of the eighth element.</typeparam>
    /// <returns>An array of <see cref="Type"/>s with eight elements.</returns>
    public static Type[] Of<T1, T2, T3, T4, T5, T6, T7, T8>() => new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8) };
}