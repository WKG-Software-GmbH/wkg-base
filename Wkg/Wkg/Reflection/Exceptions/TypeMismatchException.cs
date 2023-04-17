namespace Wkg.Reflection.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a type mismatch occurs. Commonly used when a <see cref="Type"/> is expected to be a certain type, but is not.
/// </summary>
public class TypeMismatchException : Exception
{
    public TypeMismatchException()
    {
    }

    public TypeMismatchException(string? message) : base(message)
    {
    }

    public TypeMismatchException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
