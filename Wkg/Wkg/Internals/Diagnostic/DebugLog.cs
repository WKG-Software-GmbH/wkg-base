using System.Diagnostics;
using System.Runtime.CompilerServices;
using Wkg.Logging;
using Wkg.Logging.Loggers;
using Wkg.Logging.Writers;

namespace Wkg.Internals.Diagnostic;

[DebuggerStepThrough]
internal class DebugLog /*: ILog*/ // We cannot implement ILog because we use conditional compilation to trim the code in release builds.
{
    private const string DEBUG = "DEBUG";

    /// <inheritdoc cref="Log.CurrentLogger"/>
    [Obsolete("The debug logger should not be used for configuration. Use the Log class instead.", error: true)]
    public static ILogger CurrentLogger => 
        Log.CurrentLogger;

    /// <inheritdoc cref="Log.UseLogger(ILogger)"/>
    [Obsolete("The debug logger should not be used for configuration. Use the Log class instead.", error: true)]
    public static void UseLogger(ILogger logger) => 
        Log.UseLogger(logger);

    /// <inheritdoc cref="Log.WriteDebug(string)"/>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteDebug(string message) => 
        Log.WriteDebug(message);

    /// <inheritdoc cref="Log.WriteDebug(string, ILogWriter)"/>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteDebug(string message, ILogWriter logWriter) =>
        Log.WriteDebug(message, logWriter);

    /// <inheritdoc cref="Log.WriteDiagnostic(string)"/>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteDiagnostic(string message) => 
        Log.WriteDiagnostic(message);

    /// <inheritdoc cref="Log.WriteDiagnostic(string, ILogWriter)"/>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteDiagnostic(string message, ILogWriter logWriter) =>
        Log.WriteDiagnostic(message, logWriter);

    /// <inheritdoc cref="Log.WriteWarning(string, ILogWriter)"/>
    /// <remarks>
    /// Warnings written with this method will only be visible in debug builds.
    /// </remarks>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteInternalWarning(string message, ILogWriter logWriter) =>
        Log.WriteWarning(message, logWriter);

    /// <inheritdoc cref="Log.WriteError(string)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteError(string message) => 
        Log.WriteError(message);

    /// <inheritdoc cref="Log.WriteError(string, ILogWriter)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteError(string message, ILogWriter logWriter) =>
        Log.WriteError(message, logWriter);

    /// <inheritdoc cref="Log.WriteEvent(string)"/>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteEvent(string message) => 
        Log.WriteEvent(message);

    /// <inheritdoc cref="Log.WriteEvent(string, ILogWriter)"/>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteEvent(string message, ILogWriter logWriter) =>
        Log.WriteEvent(message, logWriter);

    /// <inheritdoc cref="Log.WriteException(Exception, LogLevel)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, LogLevel logLevel = LogLevel.Error) =>
        Log.WriteException(exception, logLevel);

    /// <inheritdoc cref="Log.WriteException(Exception, ILogWriter, LogLevel)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, string additionalInfo, LogLevel logLevel = LogLevel.Error) =>
        Log.WriteException(exception, additionalInfo, logLevel);

    /// <inheritdoc cref="Log.WriteException(Exception, ILogWriter, LogLevel)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error) =>
        Log.WriteException(exception, logWriter, logLevel);

    /// <inheritdoc cref="Log.WriteException(Exception, string, ILogWriter, LogLevel)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, string additionalInfo, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error) =>
        Log.WriteException(exception, additionalInfo, logWriter, logLevel);

    /// <inheritdoc cref="Log.WriteFatal(string)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFatal(string message) =>
        Log.WriteFatal(message);

    /// <inheritdoc cref="Log.WriteFatal(string, ILogWriter)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFatal(string message, ILogWriter logWriter) =>
        Log.WriteFatal(message, logWriter);

    /// <inheritdoc cref="Log.WriteInfo(string)"/>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteInfo(string message) =>
        Log.WriteInfo(message);

    /// <inheritdoc cref="Log.WriteInfo(string, ILogWriter)"/>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteInfo(string message, ILogWriter logWriter) =>
        Log.WriteInfo(message, logWriter);

    /// <inheritdoc cref="Log.WriteWarning(string)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteWarning(string message) =>
        Log.WriteWarning(message);

    /// <inheritdoc cref="Log.WriteWarning(string, ILogWriter)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteWarning(string message, ILogWriter logWriter) =>
        Log.WriteWarning(message, logWriter);
}