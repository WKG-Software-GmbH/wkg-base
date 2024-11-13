using System.Diagnostics.CodeAnalysis;
using Wkg.Reflection.Aot.TrimmingSupport;

namespace Wkg.Reflection.Extensions;

public static partial class TypeExtensions
{
    public static partial IEnumerable<Type> GetDirectInterfaces(this Type type)
    {
        Type[] allInterfaces = type.GetInterfaces();
        return allInterfaces
            .Except(allInterfaces.SelectMany(t => t.GetInterfaces()))               // Remove all interfaces that are inherited from other interfaces
            .Except(type.BaseType?.GetInterfaces() ?? Enumerable.Empty<Type>());    // Remove all interfaces that are inherited from the base type
    }

    public static partial IEnumerable<Type[]> GetGenericTypeArgumentsOfInterfaces(this Type type, Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        return type
            .GetInterfaces()
            .GetGenericTypeArgumentsOfInterfacesCore(interfaceType);
    }

    public static partial IEnumerable<Type[]> GetGenericTypeArgumentsOfDirectInterfaces(this Type type, Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        return type
            .GetDirectInterfaces()
            .GetGenericTypeArgumentsOfInterfacesCore(interfaceType);
    }

    private static IEnumerable<Type[]> GetGenericTypeArgumentsOfInterfacesCore(this IEnumerable<Type> interfaces, Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(interfaceType, nameof(interfaceType));

        return interfaces
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType)
            .Select(i => i.GetGenericArguments());
    }

    public static partial Type[]? GetGenericTypeArgumentsOfSingleInterface(this Type type, Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        return type
            .GetInterfaces()
            .GetGenericTypeArgumentsOfSingleInterfaceCore(interfaceType);
    }

    public static partial Type? GetGenericTypeArgumentOfSingleInterface(this Type type, Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        return type
            .GetInterfaces()
            .GetGenericTypeArgumentsOfSingleInterfaceCore(interfaceType)?
            .SingleOrDefault();
    }

    public static partial Type[]? GetGenericTypeArgumentsOfSingleDirectInterface(this Type type, Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        return type
            .GetDirectInterfaces()
            .GetGenericTypeArgumentsOfSingleInterfaceCore(interfaceType);
    }

    public static partial Type? GetGenericTypeArgumentOfSingleDirectInterface(this Type type, Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        return type
            .GetDirectInterfaces()
            .GetGenericTypeArgumentsOfSingleInterfaceCore(interfaceType)?
            .SingleOrDefault();
    }

    private static Type[]? GetGenericTypeArgumentsOfSingleInterfaceCore(this IEnumerable<Type> interfaces, Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(interfaceType, nameof(interfaceType));

        return interfaces
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType)
            .Select(i => i.GetGenericArguments())
            .SingleOrDefault();
    }

    public static partial bool ImplementsGenericInterfaceWithTypeParameter(this Type type, Type interfaceType, Type typeParam) => type
        .GetInterfaces()
        .ImplementsGenericInterfaceWithTypeParametersCore(interfaceType, typeParam);

    public static partial bool ImplementsDirectGenericInterfaceWithTypeParameter(this Type type, Type interfaceType, Type typeParam) => type
        .GetDirectInterfaces()
        .ImplementsGenericInterfaceWithTypeParametersCore(interfaceType, typeParam);

    public static partial bool ImplementsGenericInterfaceWithTypeParameters(this Type type, Type interfaceType, Type[] typeParams) =>
        type.ImplementsGenericInterfaceWithTypeParameters(interfaceType, typeParams.AsSpan());

    public static partial bool ImplementsDirectGenericInterfaceWithTypeParameters(this Type type, Type interfaceType, Type[] typeParams) =>
        type.ImplementsDirectGenericInterfaceWithTypeParameters(interfaceType, typeParams.AsSpan());

    public static partial bool ImplementsGenericInterfaceWithTypeParameters(this Type type, Type interfaceType, params ReadOnlySpan<Type> typeParams) => type
        .GetInterfaces()
        .ImplementsGenericInterfaceWithTypeParametersCore(interfaceType, typeParams);

    public static partial bool ImplementsDirectGenericInterfaceWithTypeParameters(this Type type, Type interfaceType, params ReadOnlySpan<Type> typeParams) => type
        .GetDirectInterfaces()
        .ImplementsGenericInterfaceWithTypeParametersCore(interfaceType, typeParams);

    private static bool ImplementsGenericInterfaceWithTypeParametersCore(this IEnumerable<Type> interfaces, Type interfaceType, params ReadOnlySpan<Type> argumentTypes)
    {
        foreach (Type iface in interfaces)
        {
            if (iface.IsGenericType
                && iface.GetGenericTypeDefinition() == interfaceType
                && iface.GetGenericArguments().AsSpan().SequenceEqual(argumentTypes))
            {
                return true;
            }
        }
        return false;
    }

    public static partial bool ExtendsGenericBaseClass(this Type type, Type genericBaseClass)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(genericBaseClass, nameof(genericBaseClass));

        if (!genericBaseClass.IsClass || !genericBaseClass.IsGenericTypeDefinition)
        {
            throw new ArgumentException("The generic base class must be a generic class definition.", nameof(genericBaseClass));
        }
        if (type.IsClass)
        {
            Type? baseType = type.BaseType;
            while (baseType is not null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == genericBaseClass)
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }
        }
        return false;
    }

    public static partial bool ExtendsGenericBaseClassDirectly(this Type type, Type genericBaseClass)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(genericBaseClass, nameof(genericBaseClass));

        if (!genericBaseClass.IsClass || !genericBaseClass.IsGenericTypeDefinition)
        {
            throw new ArgumentException("The generic base class must be a generic class definition.", nameof(genericBaseClass));
        }
        return type.IsClass 
            && type.BaseType is not null 
            && type.BaseType.IsGenericType 
            && type.BaseType.GetGenericTypeDefinition() == genericBaseClass;
    }
    
    public static partial bool TryGetGenericBaseClassTypeArguments(this Type type, Type genericBaseClass, out Type[]? typeArguments)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(genericBaseClass, nameof(genericBaseClass));

        if (!genericBaseClass.IsClass || !genericBaseClass.IsGenericTypeDefinition)
        {
            throw new ArgumentException("The generic base class must be a generic class definition.", nameof(genericBaseClass));
        }
        if (type.IsClass)
        {
            Type? baseType = type.BaseType;
            while (baseType is not null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == genericBaseClass)
                {
                    typeArguments = baseType.GetGenericArguments();
                    return true;
                }
                baseType = baseType.BaseType;
            }
        }

        typeArguments = null;
        return false;
    }

    public static partial bool TryGetGenericBaseClassTypeArgument(this Type type, Type genericBaseClass, out Type? typeArgument)
    {
        if (TryGetGenericBaseClassTypeArguments(type, genericBaseClass, out Type[]? typeArguments))
        {
            typeArgument = typeArguments.Single();
            return true;
        }
        typeArgument = null;
        return false;
    }

    public static partial bool TryGetDirectGenericBaseClassTypeArguments(this Type type, Type genericBaseClass, out Type[]? typeArguments)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(genericBaseClass, nameof(genericBaseClass));

        if (!genericBaseClass.IsClass || !genericBaseClass.IsGenericTypeDefinition)
        {
            throw new ArgumentException("The generic base class must be a generic class definition.", nameof(genericBaseClass));
        }

        if (type.IsClass 
            && type.BaseType is not null 
            && type.BaseType.IsGenericType 
            && type.BaseType.GetGenericTypeDefinition() == genericBaseClass)
        {
            typeArguments = type.BaseType.GetGenericArguments();
            return true;
        }

        typeArguments = null;
        return false;
    }

    public static partial bool TryGetDirectGenericBaseClassTypeArgument(this Type type, Type genericBaseClass, out Type? typeArgument)
    {
        if (TryGetDirectGenericBaseClassTypeArguments(type, genericBaseClass, out Type[]? typeArguments))
        {
            typeArgument = typeArguments.Single();
            return true;
        }
        typeArgument = null;
        return false;
    }

    public static partial Type[]? GetGenericBaseClassTypeArguments(this Type type, Type genericBaseClass)
    {
        _ = TryGetGenericBaseClassTypeArguments(type, genericBaseClass, out Type[]? typeArguments);
        return typeArguments;
    }

    public static partial Type? GetGenericBaseClassTypeArgument(this Type type, Type genericBaseClass)
    {
        _ = TryGetGenericBaseClassTypeArgument(type, genericBaseClass, out Type? typeArgument);
        return typeArgument;
    }

    public static partial Type[]? GetDirectGenericBaseClassTypeArguments(this Type type, Type genericBaseClass)
    {
        _ = TryGetDirectGenericBaseClassTypeArguments(type, genericBaseClass, out Type[]? typeArguments);
        return typeArguments;
    }

    public static partial Type? GetDirectGenericBaseClassTypeArgument(this Type type, Type genericBaseClass)
    {
        _ = TryGetDirectGenericBaseClassTypeArgument(type, genericBaseClass, out Type? typeArgument);
        return typeArgument;
    }

    [SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Delegates to non-generic overload.")]
    public static partial bool ImplementsInterfaceDirectly<TInteface>(this Type type) => 
        type.ImplementsInterfaceDirectly(typeof(TInteface));

    public static partial bool ImplementsInterfaceDirectly(this Type type, Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(interfaceType, nameof(interfaceType));

        if (!interfaceType.IsInterface)
        {
            throw new ArgumentException("The interface type must be an interface.", nameof(interfaceType));
        }
        return type.GetDirectInterfaces().Contains(interfaceType);
    }

    [SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Delegates to non-generic overload.")]
    public static partial bool ImplementsInterface<TInteface>(this Type type) => 
        type.ImplementsInterface(typeof(TInteface));

    public static partial bool ImplementsInterface(this Type type, Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(interfaceType, nameof(interfaceType));

        if (!interfaceType.IsInterface)
        {
            throw new ArgumentException("The interface type must be an interface.", nameof(interfaceType));
        }
        return interfaceType.IsAssignableFrom(type);
    }

    public static partial bool ImplementsGenericInterface(this Type type, Type genericInterfaceType) =>
        ImplementsGenericInterfaceCore(type, genericInterfaceType, t => t.GetInterfaces());

    public static partial bool ImplementsGenericInterfaceDirectly(this Type type, Type genericInterfaceType) =>
        ImplementsGenericInterfaceCore(type, genericInterfaceType, t => t.GetDirectInterfaces());

    private static bool ImplementsGenericInterfaceCore(Type type, Type genericInterfaceType, Func<Type, IEnumerable<Type>> getInterfaces)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(genericInterfaceType, nameof(genericInterfaceType));

        if (!genericInterfaceType.IsInterface || !genericInterfaceType.IsGenericTypeDefinition)
        {
            throw new ArgumentException("The generic interface type must be a generic interface definition.", nameof(genericInterfaceType));
        }
        return getInterfaces(type).Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterfaceType);
    }

    public static partial Type[]? GetDirectGenericInterfaceTypeArguments(this Type type, Type genericInterfaceType)
    {
        _ = TryGetDirectGenericInterfaceTypeArguments(type, genericInterfaceType, out Type[]? typeArguments);
        return typeArguments;
    }

    public static partial Type? GetDirectGenericInterfaceTypeArgument(this Type type, Type genericInterfaceType)
    {
        if (TryGetDirectGenericInterfaceTypeArguments(type, genericInterfaceType, out Type[]? typeArguments))
        {
            return typeArguments.Single();
        }
        return null;
    }

    public static partial bool TryGetDirectGenericInterfaceTypeArguments(this Type type, Type genericInterfaceType, out Type[]? genericTypeArguments)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(genericInterfaceType, nameof(genericInterfaceType));

        if (!genericInterfaceType.IsInterface || !genericInterfaceType.IsGenericTypeDefinition)
        {
            throw new ArgumentException("The generic interface type must be a generic interface definition.", nameof(genericInterfaceType));
        }
        foreach (Type interfaceType in type.GetDirectInterfaces())
        {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == genericInterfaceType)
            {
                genericTypeArguments = interfaceType.GetGenericArguments();
                return true;
            }
        }

        genericTypeArguments = null;
        return false;
    }
}
