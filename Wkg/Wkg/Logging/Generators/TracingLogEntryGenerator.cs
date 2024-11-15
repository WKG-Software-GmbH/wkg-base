using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Wkg.Logging.Configuration;
using Wkg.Logging.Generators.Helpers;
using Wkg.Logging.Intrinsics.CallStack;
using Wkg.Text;

namespace Wkg.Logging.Generators;

/// <summary>
/// A diagnostic log entry generator that generates log entries in the format of:
/// <code>
/// 2023-05-31 14:14:24.626 (UTC) MyAssembly: [Info->Thread_0x1(MAIN THREAD)] (MyClass::MyMethod(String[], Boolean)) ==> Output: 'This is a log message'
/// 2023-05-31 14:14:24.626 (UTC) MyAssembly: [ERROR->Thread_0x1(MAIN THREAD)] (MyClass::MyMethod(String[], Boolean)) ==> [NullReferenceException] info: 'while trying to do a thing' original: 'Object reference not set to an instance of an object.' at: 
///    StackTrace line 1
/// 2023-05-31 14:14:24.626 (UTC) MyAssembly: [Info->Thread_0x1(MAIN THREAD)] (MyClass::ByButton) ==> OnClick(MyEventType: { "Property": "JSON serialized model", "foo": 1234 })
/// </code>
/// </summary>
/// <remarks>
/// This class requires reflective enumeration of target site information and stack unwinding, resulting in a performance penalty for extensive logging.
/// It is recommended to use this class only in development environments.
/// </remarks>
[RequiresUnreferencedCode("Requires reflective access to the event args type for JSON serialization.")]
public class TracingLogEntryGenerator : ILogEntryGenerator<TracingLogEntryGenerator>
{
    // tracing log entries can get rather large, so we use a capacity that should be sufficient for most cases
    private const int DEFAULT_STRING_BUILDER_CAPACITY = 1024;

    /// <summary>
    /// The <see cref="CompiledLoggerConfiguration"/> used to create this <see cref="TracingLogEntryGenerator"/>
    /// </summary>
    protected readonly CompiledLoggerConfiguration _config;

    /// <summary>
    /// A lookup table and reflection cache for target site information.
    /// </summary>
    protected readonly ConcurrentDictionary<MethodBase, string> _targetSiteLookup = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TracingLogEntryGenerator"/> class.
    /// </summary>
    /// <param name="config">The <see cref="CompiledLoggerConfiguration"/> used to create this <see cref="TracingLogEntryGenerator"/></param>
    protected TracingLogEntryGenerator(CompiledLoggerConfiguration config) =>
        _config = config;

    /// <inheritdoc/>
    public static TracingLogEntryGenerator Create(CompiledLoggerConfiguration config) =>
        new(config);

    /// <inheritdoc/>
    [StackTraceHidden]
    public virtual void Generate(ref LogEntry entry, string title, string message)
    {
        StringBuilder builder = StringBuilderPool.Shared.Rent(DEFAULT_STRING_BUILDER_CAPACITY);

        GenerateHeader(ref entry, builder, null, out MethodBase? method);
        AddTargetSite(ref entry, builder, method);
        builder.Append(title).Append(": \'")
            .Append(message)
            .Append('\'');
        entry.LogMessage = builder.ToString();

        StringBuilderPool.Shared.Return(builder);
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    public virtual void Generate(ref LogEntry entry, Exception exception, string? additionalInfo)
    {
        // stack traces can get rather large, so we use a capacity that should be sufficient for most cases
        StringBuilder builder = StringBuilderPool.Shared.Rent(8192);

        GenerateHeader(ref entry, builder, null, out MethodBase? method);
        AddTargetSite(ref entry, builder, method);
        builder.Append('[')
            .Append(exception.GetType().Name)
            .Append("] ");
        entry.Exception = exception;
        if (additionalInfo is not null)
        {
            entry.AdditionalInfo = additionalInfo;
            builder.Append("info: \'")
                .Append(additionalInfo)
                .Append("\' ");
        }
        AddStackTrace(ref entry, builder);
        entry.LogMessage = builder.ToString();

        StringBuilderPool.Shared.Return(builder);
    }

    /// <summary>
    /// Adds the stack trace of the <paramref name="entry"/> to the <paramref name="builder"/>.
    /// </summary>
    /// <param name="entry">The <see cref="LogEntry"/> to add the stack trace of.</param>
    /// <param name="builder">The <see cref="StringBuilder"/> to add the stack trace to.</param>
    protected virtual void AddStackTrace(ref LogEntry entry, StringBuilder builder)
    {
        if (entry.Exception is null)
        {
            return;
        }

        builder.Append("original: \'")
            .Append(entry.Exception.Message)
            .Append("\' at: ");

        AddStackTraceOrPlaceholder(builder, entry.Exception.StackTrace);

        for (Exception? inner = entry.Exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            builder.AppendLine()
                .Append("caused by ")
                .Append(inner.GetType().Name)
                .Append(": '")
                .Append(inner.Message)
                .Append("' at:");

            AddStackTraceOrPlaceholder(builder, inner.StackTrace);
        }
    }

    /// <summary>
    /// Adds the <paramref name="stackTrace"/> to the <paramref name="builder"/> or a placeholder if the <paramref name="stackTrace"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="builder">The <see cref="StringBuilder"/> to add the <paramref name="stackTrace"/> to.</param>
    /// <param name="stackTrace">The stack trace to add to the <paramref name="builder"/>.</param>
    protected virtual void AddStackTraceOrPlaceholder(StringBuilder builder, string? stackTrace)
    {
        if (stackTrace is null)
        {
            builder.Append("<stacktrace unavailable>");
        }
        else
        {
            builder.AppendLine()
                .Append(stackTrace);
        }
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    public virtual void Generate<TEventArgs>(ref LogEntry entry, string? className, string instanceName, string eventName, TEventArgs eventArgs)
    {
        StringBuilder builder = StringBuilderPool.Shared.Rent(DEFAULT_STRING_BUILDER_CAPACITY);

        GenerateHeader(ref entry, builder, entry.AssemblyName, out MethodBase? method);
        builder.Append('(');
        className ??= method?.DeclaringType?.Name ?? "<UnknownType>";
        entry.ClassName = className;
        builder
            .Append(className)
            .Append("::")
            .Append(instanceName)
            .Append(") ==> ")
            .Append(eventName)
            .Append('(');

        entry.InstanceName = instanceName;
        entry.EventName = eventName;
        entry.EventArgs = eventArgs;
        AddEventArgs(eventArgs, builder);
        builder.Append(')');

        entry.LogMessage = builder.ToString();

        StringBuilderPool.Shared.Return(builder);
    }

    /// <summary>
    /// Adds the <paramref name="args"/> to the <paramref name="builder"/> in a human-readable (JSON) format.
    /// </summary>
    /// <typeparam name="TEventArgs">The type of the event args.</typeparam>
    /// <param name="args">The event args to add.</param>
    /// <param name="builder">The <see cref="StringBuilder"/> to add the <paramref name="args"/> to.</param>
    protected virtual void AddEventArgs<TEventArgs>(TEventArgs args, StringBuilder builder)
    {
        const string NULL_STRING = "null";

        builder.Append(typeof(TEventArgs).Name).Append(": ");
        if (args is null)
        {
            builder.Append(NULL_STRING);
        }
        else
        {
            try
            {
                builder.Append(JsonSerializer.Serialize(args));
            }
            catch (NotSupportedException ex)
            {
                builder.Append($"{nameof(NotSupportedException)} -> ")
                    .Append(ex.Message);
            }
        }
    }

    /// <summary>
    /// Generates the log entry header.
    /// </summary>
    /// <param name="entry">The <see cref="LogEntry"/> to write the log entry to.</param>
    /// <param name="builder">The <see cref="StringBuilder"/> to use for the log entry.</param>
    /// <param name="textAssemblyName">The name of the assembly that is logging (if known). If <see langword="null"/>, the assembly name will be determined using reflection.</param>
    /// <param name="method">(Output) The <see cref="MethodBase"/> of the method that is logging.</param>
    /// <returns>A <see cref="StringBuilder"/> containing the log entry header.</returns>
    [StackTraceHidden]
    protected virtual void GenerateHeader(ref LogEntry entry, StringBuilder builder, string? textAssemblyName, out MethodBase? method)
    {
        method = null;
        if (textAssemblyName is null)
        {
            StackTrace stack = new();

            method = stack.GetFirstNonHiddenCaller();
            textAssemblyName = method?.DeclaringType?.Assembly.GetName().Name ?? "<UnknownAssembly>";
        }
        entry.AssemblyName = textAssemblyName;

        string textLogLevel = LogLevelNames.NameForOrUnknown(entry.LogLevel);
        string mainThreadTag = string.Empty;
        int threadId = Environment.CurrentManagedThreadId;
        entry.ThreadId = threadId;
        if (threadId == _config.MainThreadId)
        {
            mainThreadTag = "(MAIN THREAD)";
            entry.IsMainThread = true;
        }
        entry.TimestampUtc = DateTime.UtcNow;
        builder.Append(entry.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Append(" (UTC) ")
            .Append(textAssemblyName)
            .Append(": [")
            .Append(textLogLevel)
            .Append("->Thread_0x")
            .Append(threadId.ToString("x"))
            .Append(mainThreadTag)
            .Append("] ");
    }

    /// <summary>
    /// Adds the target site to the <paramref name="builder"/>.
    /// </summary>
    /// <param name="entry">The <see cref="LogEntry"/> to add the target site to.</param>
    /// <param name="builder">The <see cref="StringBuilder"/> to add the target site to.</param>
    /// <param name="method">The <see cref="MethodBase"/> of the method that is logging.</param>
    protected virtual void AddTargetSite(ref LogEntry entry, StringBuilder builder, MethodBase? method)
    {
        string textTargetSite = method is null ? "(<UnknownType>)" : GetTargetSite(method);
        entry.TargetSite = textTargetSite;
        builder
            .Append(textTargetSite)
            .Append(" ==> ");
    }

    /// <summary>
    /// Gets the target site of the <paramref name="method"/> either from the cache or by generating it.
    /// </summary>
    /// <param name="method">The <see cref="MethodBase"/> of the method that is logging.</param>
    /// <returns>The target site of the <paramref name="method"/>.</returns>
    protected virtual string GetTargetSite(MethodBase method)
    {
        if (!_targetSiteLookup.TryGetValue(method, out string? site))
        {
            string type = method.DeclaringType?.Name ?? "<UnknownType>";
            ParameterInfo[] parameters = method.GetParameters();
            // we don't expect the target site string to et very long, so we use a capacity that should be sufficient for most cases
            StringBuilder targetBuilder = StringBuilderPool.Shared.Rent(128);
            targetBuilder.Append('(').Append(type).Append("::").Append(method.Name).Append('(');
            bool flag = false;
            foreach (ParameterInfo parameter in parameters)
            {
                if (flag)
                {
                    targetBuilder.Append(", ");
                }
                targetBuilder.Append(parameter.ParameterType.Name);
                flag = true;
            }
            targetBuilder.Append("))");
            site = targetBuilder.ToString();
            _targetSiteLookup.TryAdd(method, site);
            StringBuilderPool.Shared.Return(targetBuilder);
        }
        return site;
    }
}
