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

    /// <inheritdoc cref="Log.UseLogger(IProxyLogger)"/>
    [Obsolete("The debug logger should not be used for configuration. Use the Log class instead.", error: true)]
    public static void UseLogger(IProxyLogger logger) => 
        Log.UseLogger(logger);

    /// <inheritdoc cref="Log.WriteDebug(string, string, string, int)"/>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteDebug(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        Log._proxyLogger.LogInternal(message, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Debug);

    /// <inheritdoc cref="Log.WriteDebug(string, ILogWriter, string, string, int)"/>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteDebug(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        Log._proxyLogger.LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Debug);

    /// <inheritdoc cref="Log.WriteDiagnostic(string, string, string, int)"/>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteDiagnostic(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        Log._proxyLogger.LogInternal(message, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Diagnostic);

    /// <inheritdoc cref="Log.WriteDiagnostic(string, ILogWriter, string, string, int)"/>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteDiagnostic(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        Log._proxyLogger.LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Diagnostic);

    /// <inheritdoc cref="Log.WriteWarning(string, ILogWriter, string, string, int)"/>
    /// <remarks>
    /// Warnings written with this method will only be visible in debug builds.
    /// </remarks>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteInternalWarning(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        Log._proxyLogger.LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Warning);

    /// <inheritdoc cref="Log.WriteError(string, string, string, int)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteError(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        Log._proxyLogger.LogInternal(message, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Error);

    /// <inheritdoc cref="Log.WriteError(string, ILogWriter, string, string, int)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteError(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        Log._proxyLogger.LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Error);

    /// <inheritdoc cref="Log.WriteEvent(string, string, string, int)"/>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteEvent(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        Log._proxyLogger.LogInternal(message, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Event);

    /// <inheritdoc cref="Log.WriteEvent(string, ILogWriter, string, string, int)"/>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteEvent(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        Log._proxyLogger.LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Event);

    /// <inheritdoc cref="Log.WriteException(Exception, LogLevel, string, string, int)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        Log._proxyLogger.LogInternal(exception, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc cref="Log.WriteException(Exception, ILogWriter, LogLevel, string, string, int)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, string additionalInfo, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        Log._proxyLogger.LogInternal(exception, additionalInfo, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc cref="Log.WriteException(Exception, ILogWriter, LogLevel, string, string, int)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        Log._proxyLogger.LogInternal(exception, logWriter, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc cref="Log.WriteException(Exception, string, ILogWriter, LogLevel, string, string, int)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, string additionalInfo, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        Log._proxyLogger.LogInternal(exception, additionalInfo, logWriter, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc cref="Log.WriteFatal(string, string, string, int)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFatal(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        Log._proxyLogger.LogInternal(message, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Fatal);

    /// <inheritdoc cref="Log.WriteFatal(string, ILogWriter, string, string, int)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFatal(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        Log._proxyLogger.LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Fatal);

    /// <inheritdoc cref="Log.WriteInfo(string, string, string, int)"/>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteInfo(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        Log._proxyLogger.LogInternal(message, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Info);

    /// <inheritdoc cref="Log.WriteInfo(string, ILogWriter, string, string, int)"/>
    [StackTraceHidden]
    [Conditional(DEBUG)]
    public static void WriteInfo(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        Log._proxyLogger.LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Info);

    /// <inheritdoc cref="Log.WriteWarning(string, string, string, int)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteWarning(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        Log._proxyLogger.LogInternal(message, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Warning);

    /// <inheritdoc cref="Log.WriteWarning(string, ILogWriter, string, string, int)"/>
    /// <remarks>
    /// <see langword="WARNING"></see>: This method is *<c>NOT</c>* marked with the <see cref="ConditionalAttribute"/> and will be compiled in release builds.
    /// </remarks>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteWarning(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        Log._proxyLogger.LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Warning);
}