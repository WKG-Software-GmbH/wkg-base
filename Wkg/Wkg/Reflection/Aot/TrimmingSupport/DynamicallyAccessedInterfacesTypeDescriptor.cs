using System.Diagnostics.CodeAnalysis;

namespace Wkg.Reflection.Aot.TrimmingSupport;

/// <summary>
/// Descriptor marking the specified <see cref="Type"/> with the <see cref="DynamicallyAccessedMemberTypes.Interfaces"/> flag for AOT trimming analysis.
/// </summary>
/// <param name="type">The type to mark.</param>
public readonly struct DynamicallyAccessedInterfacesTypeDescriptor(Type type)
{
    /// <summary>
    /// The type decorated with the <see cref="DynamicallyAccessedMemberTypes.Interfaces"/> flag.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
    public readonly Type Type = type;
}