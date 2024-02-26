using System.Diagnostics.CodeAnalysis;

namespace Wkg.Extensions.Reflection;

/// <summary>
/// Provides extension methods for <see cref="Type"/>.
/// </summary>
public static partial class TypeExtensions
{
    /// <summary>
    /// Gets all interfaces that are directly implemented by the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to get the interfaces of.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> containing all interfaces that are directly implemented by the specified <paramref name="type"/>.</returns>
    public static partial IEnumerable<Type> GetDirectInterfaces(this Type type);

    /// <summary>
    /// Gets all generic type arguments of all interfaces of type <paramref name="interfaceType"/> that are implemented by the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> implementing the interfaces to get the generic type arguments of.</param>
    /// <param name="interfaceType">The <see cref="Type"/> of the interfaces implemented by the specified <paramref name="type"/> to get the generic type arguments of.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> containing all generic type arguments of all interfaces of type <paramref name="interfaceType"/> that are implemented by the specified <paramref name="type"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="interfaceType"/> is <see langword="null"/>.</exception>
    public static partial IEnumerable<Type[]> GetGenericTypeArgumentsOfInterfaces(this Type type, Type interfaceType);

    /// <summary>
    /// Gets all generic type arguments of all interfaces of type <paramref name="interfaceType"/> that are directly implemented by the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> directly implementing the interfaces to get the generic type arguments of.</param>
    /// <param name="interfaceType">The <see cref="Type"/> of the interfaces implemented by the specified <paramref name="type"/> to get the generic type arguments of.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> containing all generic type arguments of all direct interfaces of type <paramref name="interfaceType"/> that are implemented by the specified <paramref name="type"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="interfaceType"/> is <see langword="null"/>.</exception>
    public static partial IEnumerable<Type[]> GetGenericTypeArgumentsOfDirectInterfaces(this Type type, Type interfaceType);

    /// <summary>
    /// Gets the generic type arguments of the single interface implementation of type <paramref name="interfaceType"/> that is implemented by the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> implementing the interface to get the generic type arguments of.</param>
    /// <param name="interfaceType">The <see cref="Type"/> of the interface implemented by the specified <paramref name="type"/> to get the generic type arguments of.</param>
    /// <returns>An array containing the generic type arguments of the single interface of type <paramref name="interfaceType"/> that is implemented by the specified <paramref name="type"/> or <see langword="null"/> if the specified <paramref name="type"/> does not implement the interface of type <paramref name="interfaceType"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="interfaceType"/> is <see langword="null"/>.</exception>
    public static partial Type[]? GetGenericTypeArgumentsOfSingleInterface(this Type type, Type interfaceType);

    /// <summary>
    /// Gets the single generic type argument of the single interface implementation of type <paramref name="interfaceType"/> that is implemented by the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> implementing the interface to get the generic type argument of.</param>
    /// <param name="interfaceType">The <see cref="Type"/> of the interface implemented by the specified <paramref name="type"/> to get the generic type argument of.</param>
    /// <returns>The generic type argument of the single interface implementation of type <paramref name="interfaceType"/> that is implemented by the specified <paramref name="type"/> or <see langword="null"/> if the specified <paramref name="type"/> does not implement exactly one interface of type <paramref name="interfaceType"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="interfaceType"/> is <see langword="null"/>.</exception>
    public static partial Type? GetGenericTypeArgumentOfSingleInterface(this Type type, Type interfaceType);

    /// <summary>
    /// Gets the generic type arguments of the single interface of type <paramref name="interfaceType"/> that is directly implemented by the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> directly implementing the interface to get the generic type arguments of.</param>
    /// <param name="interfaceType">The <see cref="Type"/> of the interface implemented by the specified <paramref name="type"/> to get the generic type arguments of.</param>
    /// <returns>An array containing the generic type arguments of the single direct interface of type <paramref name="interfaceType"/> that is implemented by the specified <paramref name="type"/> or <see langword="null"/> if the specified <paramref name="type"/> does not implement the interface of type <paramref name="interfaceType"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="interfaceType"/> is <see langword="null"/>.</exception>
    public static partial Type[]? GetGenericTypeArgumentsOfSingleDirectInterface(this Type type, Type interfaceType);

    /// <summary>
    /// Gets the single generic type argument of the single interface of type <paramref name="interfaceType"/> that is directly implemented by the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> directly implementing the interface to get the generic type argument of.</param>
    /// <param name="interfaceType">The <see cref="Type"/> of the interface implemented by the specified <paramref name="type"/> to get the generic type argument of.</param>
    /// <returns>The generic type argument of the single direct interface implementation of type <paramref name="interfaceType"/> that is implemented by the specified <paramref name="type"/> or <see langword="null"/> if the specified <paramref name="type"/> does not implement the interface of type <paramref name="interfaceType"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="interfaceType"/> is <see langword="null"/>.</exception>
    public static partial Type? GetGenericTypeArgumentOfSingleDirectInterface(this Type type, Type interfaceType);

    /// <summary>
    /// Checks whether the specified <paramref name="type"/> implements an interface of type <paramref name="interfaceType"/> with a single generic type parameter of type <paramref name="typeParam"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <param name="interfaceType">The <see cref="Type"/> of the generic interface to check for.</param>
    /// <param name="typeParam">The <see cref="Type"/> of the generic type parameter to check for.</param>
    /// <returns><see langword="true"/> if the specified <paramref name="type"/> implements an interface of type <paramref name="interfaceType"/> with a single generic type parameter of type <paramref name="typeParam"/>; otherwise, <see langword="false"/>.</returns>
    public static partial bool ImplementsGenericInterfaceWithTypeParameter(this Type type, Type interfaceType, Type typeParam);

    /// <summary>
    /// Checks whether the specified <paramref name="type"/> directly implements an interface of type <paramref name="interfaceType"/> with a single generic type parameter of type <paramref name="typeParam"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <param name="interfaceType">The <see cref="Type"/> of the generic interface to check for.</param>
    /// <param name="typeParam">The <see cref="Type"/> of the generic type parameter to check for.</param>
    /// <returns><see langword="true"/> if the specified <paramref name="type"/> directly implements an interface of type <paramref name="interfaceType"/> with a single generic type parameter of type <paramref name="typeParam"/>; otherwise, <see langword="false"/>.</returns>
    public static partial bool ImplementsDirectGenericInterfaceWithTypeParameter(this Type type, Type interfaceType, Type typeParam);

    /// <summary>
    /// Checks whether the specified <paramref name="type"/> implements an interface of type <paramref name="interfaceType"/> with generic type parameters of the specified <paramref name="typeParams"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <param name="interfaceType">The <see cref="Type"/> of the generic interface to check for.</param>
    /// <param name="typeParams">The <see cref="Type"/>s of the generic type parameters in the order they are defined in the interface to check for.</param>
    /// <returns><see langword="true"/> if the specified <paramref name="type"/> implements an interface of type <paramref name="interfaceType"/> with generic type parameters of the specified <paramref name="typeParams"/>; otherwise, <see langword="false"/>.</returns>
    public static partial bool ImplementsGenericInterfaceWithTypeParameters(this Type type, Type interfaceType, params Type[] typeParams);

    /// <summary>
    /// Checks whether the specified <paramref name="type"/> directly implements an interface of type <paramref name="interfaceType"/> with generic type parameters of the specified <paramref name="typeParams"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <param name="interfaceType">The <see cref="Type"/> of the generic interface to check for.</param>
    /// <param name="typeParams">The <see cref="Type"/>s of the generic type parameters in the order they are defined in the interface to check for.</param>
    /// <returns><see langword="true"/> if the specified <paramref name="type"/> directly implements an interface of type <paramref name="interfaceType"/> with generic type parameters of the specified <paramref name="typeParams"/>; otherwise, <see langword="false"/>.</returns>
    public static partial bool ImplementsDirectGenericInterfaceWithTypeParameters(this Type type, Type interfaceType, params Type[] typeParams);

    /// <summary>
    /// Checks whether the specified <paramref name="type"/> extends the specified <paramref name="genericBaseClass"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <param name="genericBaseClass">The generic base class definition to check for.</param>
    /// <returns><see langword="true"/> if the specified <paramref name="type"/> extends the specified <paramref name="genericBaseClass"/>; otherwise, <see langword="false"/>.</returns>
    public static partial bool ExtendsGenericBaseClass(this Type type, Type genericBaseClass);

    /// <summary>
    /// Checks whether the specified <paramref name="type"/> directly extends the specified <paramref name="genericBaseClass"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <param name="genericBaseClass">The generic base class definition to check for.</param>
    /// <returns><see langword="true"/> if the specified <paramref name="type"/> directly extends the specified <paramref name="genericBaseClass"/>; otherwise, <see langword="false"/>.</returns>
    public static partial bool ExtendsGenericBaseClassDirectly(this Type type, Type genericBaseClass);

    /// <summary>
    /// Attempts to retrieve the generic type arguments that <paramref name="type"/> uses to extend the specified <paramref name="genericBaseClass"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> extending the specified <paramref name="genericBaseClass"/> of which to retrieve the generic type arguments.</param>
    /// <param name="genericBaseClass">The <see cref="Type"/> of the generic base class of which to retrieve the generic type arguments.</param>
    /// <param name="typeArguments">The generic type arguments that <paramref name="type"/> uses to extend the specified <paramref name="genericBaseClass"/> if the operation was successful; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="type"/> extends the specified <paramref name="genericBaseClass"/>; otherwise, <see langword="false"/>.</returns>
    public static partial bool TryGetGenericBaseClassTypeArguments(this Type type, Type genericBaseClass, [NotNullWhen(true)] out Type[]? typeArguments);

    /// <summary>
    /// Attempts to retrieve the single generic type argument that <paramref name="type"/> uses to extend the specified <paramref name="genericBaseClass"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> extending the specified <paramref name="genericBaseClass"/> of which to retrieve the generic type argument.</param>
    /// <param name="genericBaseClass">The <see cref="Type"/> of the generic base class of which to retrieve the generic type argument.</param>
    /// <param name="typeArgument">The generic type argument that <paramref name="type"/> uses to extend the specified <paramref name="genericBaseClass"/> if the operation was successful; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="type"/> extends the specified <paramref name="genericBaseClass"/>; otherwise, <see langword="false"/>.</returns>
    public static partial bool TryGetGenericBaseClassTypeArgument(this Type type, Type genericBaseClass, [NotNullWhen(true)] out Type? typeArgument);

    /// <summary>
    /// Attempts to retrieve the generic type arguments that <paramref name="type"/> uses to directly extend the specified <paramref name="genericBaseClass"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> directly extending the specified <paramref name="genericBaseClass"/> of which to retrieve the generic type arguments.</param>
    /// <param name="genericBaseClass">The <see cref="Type"/> of the generic base class of which to retrieve the generic type arguments.</param>
    /// <param name="typeArguments">The generic type arguments that <paramref name="type"/> uses to directly extend the specified <paramref name="genericBaseClass"/> if the operation was successful; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="type"/> directly extends the specified <paramref name="genericBaseClass"/>; otherwise, <see langword="false"/>.</returns>
    public static partial bool TryGetDirectGenericBaseClassTypeArguments(this Type type, Type genericBaseClass, [NotNullWhen(true)] out Type[]? typeArguments);

    /// <summary>
    /// Attempts to retrieve the single generic type argument that <paramref name="type"/> uses to directly extend the specified <paramref name="genericBaseClass"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> directly extending the specified <paramref name="genericBaseClass"/> of which to retrieve the generic type argument.</param>
    /// <param name="genericBaseClass">The <see cref="Type"/> of the generic base class of which to retrieve the generic type argument.</param>
    /// <param name="typeArgument">The generic type argument that <paramref name="type"/> uses to directly extend the specified <paramref name="genericBaseClass"/> if the operation was successful; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="type"/> directly extends the specified <paramref name="genericBaseClass"/>; otherwise, <see langword="false"/>.</returns>
    public static partial bool TryGetDirectGenericBaseClassTypeArgument(this Type type, Type genericBaseClass, [NotNullWhen(true)] out Type? typeArgument);

    /// <summary>
    /// Retrieves the generic type arguments that <paramref name="type"/> uses to extend the specified <paramref name="genericBaseClass"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> extending the specified <paramref name="genericBaseClass"/> of which to retrieve the generic type arguments.</param>
    /// <param name="genericBaseClass">The <see cref="Type"/> of the generic base class of which to retrieve the generic type arguments.</param>
    /// <returns>The generic type arguments that <paramref name="type"/> uses to extend the specified <paramref name="genericBaseClass"/> or <see langword="null"/> if <paramref name="type"/> does not extend the specified <paramref name="genericBaseClass"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="genericBaseClass"/> is <see langword="null"/>.</exception>
    public static partial Type[]? GetGenericBaseClassTypeArguments(this Type type, Type genericBaseClass);

    /// <summary>
    /// Retrieves the single generic type argument that <paramref name="type"/> uses to extend the specified <paramref name="genericBaseClass"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> extending the specified <paramref name="genericBaseClass"/> of which to retrieve the generic type argument.</param>
    /// <param name="genericBaseClass">The <see cref="Type"/> of the generic base class of which to retrieve the generic type argument.</param>
    /// <returns>The generic type argument that <paramref name="type"/> uses to extend the specified <paramref name="genericBaseClass"/> or <see langword="null"/> if <paramref name="type"/> does not extend the specified <paramref name="genericBaseClass"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="genericBaseClass"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="type"/> extends the specified <paramref name="genericBaseClass"/> but the number of generic type arguments is not exactly one.</exception>
    public static partial Type? GetGenericBaseClassTypeArgument(this Type type, Type genericBaseClass);

    /// <summary>
    /// Retrieves the generic type arguments that <paramref name="type"/> uses to directly extend the specified <paramref name="genericBaseClass"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> directly extending the specified <paramref name="genericBaseClass"/> of which to retrieve the generic type arguments.</param>
    /// <param name="genericBaseClass">The <see cref="Type"/> of the generic base class of which to retrieve the generic type arguments.</param>
    /// <returns>The generic type arguments that <paramref name="type"/> uses to directly extend the specified <paramref name="genericBaseClass"/> or <see langword="null"/> if <paramref name="type"/> does not directly extend the specified <paramref name="genericBaseClass"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="genericBaseClass"/> is <see langword="null"/>.</exception>
    public static partial Type[]? GetDirectGenericBaseClassTypeArguments(this Type type, Type genericBaseClass);

    /// <summary>
    /// Retrieves the single generic type argument that <paramref name="type"/> uses to directly extend the specified <paramref name="genericBaseClass"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> directly extending the specified <paramref name="genericBaseClass"/> of which to retrieve the generic type argument.</param>
    /// <param name="genericBaseClass">The <see cref="Type"/> of the generic base class of which to retrieve the generic type argument.</param>
    /// <returns>The generic type argument that <paramref name="type"/> uses to directly extend the specified <paramref name="genericBaseClass"/> or <see langword="null"/> if <paramref name="type"/> does not directly extend the specified <paramref name="genericBaseClass"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="genericBaseClass"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="type"/> directly extends the specified <paramref name="genericBaseClass"/> but the number of generic type arguments is not exactly one.</exception>
    public static partial Type? GetDirectGenericBaseClassTypeArgument(this Type type, Type genericBaseClass);

    /// <summary>
    /// Checks whether <paramref name="type"/> implements the specified non-generic interface <paramref name="interfaceType"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check whether it implements the specified <paramref name="interfaceType"/>.</param>
    /// <param name="interfaceType">The <see cref="Type"/> of the interface to be checked for.</param>
    /// <returns><see langword="true"/> if <paramref name="type"/> implements the specified <paramref name="interfaceType"/>; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="interfaceType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="interfaceType"/> is not an interface.</exception>
    /// <remarks>
    /// <para>This method is equivalent to <see cref="Type.IsAssignableFrom(Type)"/> with the restriction that <paramref name="interfaceType"/> must be an interface.</para>
    /// </remarks>
    /// <seealso cref="Type.IsAssignableFrom(Type)"/>
    /// <seealso cref="Type.IsInterface"/>
    /// <seealso cref="Type.IsGenericType"/>
    public static partial bool ImplementsInterface(this Type type, Type interfaceType);

    /// <summary>
    /// Checks whether <paramref name="type"/> implements the specified generic interface <paramref name="genericInterfaceType"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check whether it implements the specified <paramref name="genericInterfaceType"/>.</param>
    /// <param name="genericInterfaceType">The <see cref="Type"/> of the interface to be checked for.</param>
    /// <returns><see langword="true"/> if <paramref name="type"/> implements the specified <paramref name="genericInterfaceType"/>; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="genericInterfaceType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="genericInterfaceType"/> is not a generic interface.</exception>
    /// <remarks>
    /// <para>This method is equivalent to <see cref="Type.IsAssignableFrom(Type)"/> with the restriction that <paramref name="genericInterfaceType"/> must be a generic interface.</para>
    /// </remarks>
    /// <seealso cref="Type.IsAssignableFrom(Type)"/>
    /// <seealso cref="Type.IsInterface"/>
    /// <seealso cref="Type.IsGenericType"/>
    public static partial bool ImplementsGenericInterface(this Type type, Type genericInterfaceType);

    /// <summary>
    /// Checks whether <paramref name="type"/> directly implements the specified generic interface <paramref name="genericInterfaceType"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check whether it directly implements the specified <paramref name="genericInterfaceType"/>.</param>
    /// <param name="genericInterfaceType">The <see cref="Type"/> of the interface to be checked for.</param>
    /// <returns><see langword="true"/> if <paramref name="type"/> directly implements the specified <paramref name="genericInterfaceType"/>; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="genericInterfaceType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="genericInterfaceType"/> is not a generic interface.</exception>
    /// <remarks>
    /// <para>This method is equivalent to <see cref="Type.IsAssignableFrom(Type)"/> with the restriction that <paramref name="genericInterfaceType"/> must be a generic interface and <paramref name="type"/> must directly implement the specified <paramref name="genericInterfaceType"/>.</para>
    /// </remarks>
    /// <seealso cref="Type.IsAssignableFrom(Type)"/>
    /// <seealso cref="Type.IsInterface"/>
    /// <seealso cref="Type.IsGenericType"/>
    /// <seealso cref="ImplementsGenericInterface(Type, Type)"/>
    public static partial bool ImplementsGenericInterfaceDirectly(this Type type, Type genericInterfaceType);

    /// <summary>
    /// Attempts to retrieve the generic type arguments that <paramref name="type"/> uses to directly implement the specified <paramref name="genericInterfaceType"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> implementing the specified <paramref name="genericInterfaceType"/> of which to retrieve the generic type arguments.</param>
    /// <param name="genericInterfaceType">The <see cref="Type"/> of the generic interface of which to retrieve the generic type arguments.</param>
    /// <param name="genericTypeArguments">The generic type arguments that <paramref name="type"/> uses to implement the specified <paramref name="genericInterfaceType"/> or <see langword="null"/> if <paramref name="type"/> does not implement the specified <paramref name="genericInterfaceType"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="type"/> implements the specified <paramref name="genericInterfaceType"/>; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="genericInterfaceType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="genericInterfaceType"/> is not a generic interface.</exception>
    public static partial bool TryGetDirectGenericInterfaceTypeArguments(this Type type, Type genericInterfaceType, [NotNullWhen(true)] out Type[]? genericTypeArguments);

    /// <summary>
    /// Retrieves the generic type arguments that <paramref name="type"/> uses to directly implement the specified <paramref name="genericInterfaceType"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> implementing the specified <paramref name="genericInterfaceType"/> of which to retrieve the generic type arguments.</param>
    /// <param name="genericInterfaceType">The <see cref="Type"/> of the generic interface of which to retrieve the generic type arguments.</param>
    /// <returns>The generic type arguments that <paramref name="type"/> uses to implement the specified <paramref name="genericInterfaceType"/> or <see langword="null"/> if <paramref name="type"/> does not implement the specified <paramref name="genericInterfaceType"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="genericInterfaceType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="genericInterfaceType"/> is not a generic interface.</exception>
    public static partial Type[]? GetDirectGenericInterfaceTypeArguments(this Type type, Type genericInterfaceType);

    /// <summary>
    /// Retrieves the single generic type argument that <paramref name="type"/> uses to implement the specified <paramref name="genericInterfaceType"/> or <see langword="null"/> if <paramref name="type"/> does not implement the specified <paramref name="genericInterfaceType"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> implementing the specified <paramref name="genericInterfaceType"/> of which to retrieve the generic type argument.</param>
    /// <param name="genericInterfaceType">The <see cref="Type"/> of the generic interface of which to retrieve the generic type argument.</param>
    /// <returns>The single generic type argument that <paramref name="type"/> uses to implement the specified <paramref name="genericInterfaceType"/> or <see langword="null"/> if <paramref name="type"/> does not implement the specified <paramref name="genericInterfaceType"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="genericInterfaceType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="genericInterfaceType"/> is not a generic interface.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="type"/> implements the specified <paramref name="genericInterfaceType"/> but does not use a single generic type argument.</exception>
    public static partial Type? GetDirectGenericInterfaceTypeArgument(this Type type, Type genericInterfaceType);
}