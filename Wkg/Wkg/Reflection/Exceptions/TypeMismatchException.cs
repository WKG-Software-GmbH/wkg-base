namespace Wkg.Reflection.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a type mismatch occurs. Commonly used when a <see cref="Type"/> is expected to be a certain type, but is not.
/// </summary>
public class TypeMismatchException : Exception
{
    /// <summary>
    /// Creates a new instance of the <see cref="TypeMismatchException"/> class.
    /// </summary>
    public TypeMismatchException()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TypeMismatchException"/> class with a specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TypeMismatchException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TypeMismatchException"/> class with a specified message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public TypeMismatchException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
