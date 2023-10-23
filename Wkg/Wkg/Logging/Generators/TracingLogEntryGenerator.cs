﻿using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Wkg.Logging.Configuration;
using Wkg.Logging.Intrinsics.CallStack;

namespace Wkg.Logging.Generators;

/// <summary>
/// A log entry generator that generates log entries in the format of:
/// <code>
/// 2023-05-31 14:14:24.626 (UTC) Wkg: [Info->Thread_0x1(MAIN THREAD)] (MyClass::MyMethod(String[], Boolean)) ==> Output: 'Hello world! :)'
/// </code>
/// </summary>
/// <remarks>
/// This class requires reflective enumeration of target site information and stack unwinding, resulting in a performance penalty for extensive logging.
/// It is recommended to use this class only in development environments.
/// </remarks>
public class TracingLogEntryGenerator : ILogEntryGenerator<TracingLogEntryGenerator>
{
    /// <summary>
    /// A thread-local <see cref="StringBuilder"/> cache to avoid unnecessary allocations
    /// </summary>
    protected static readonly ThreadLocal<StringBuilder> _stringBuilder = new(() => new StringBuilder(512), false);

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
        StringBuilder builder = AddTargetSite(ref entry, GenerateHeader(ref entry, null, out MethodBase? method), method)
            .Append(title).Append(": \'")
            .Append(message)
            .Append('\'');
        entry.LogMessage = builder.ToString();
        builder.Clear();
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    public virtual void Generate(ref LogEntry entry, Exception exception, string? additionalInfo)
    {
        StringBuilder builder = AddTargetSite(ref entry, GenerateHeader(ref entry, null, out MethodBase? method), method)
            .Append(exception.GetType().Name)
            .Append(": ");
        entry.Exception = exception;
        if (additionalInfo is not null)
        {
            entry.AdditionalInfo = additionalInfo;
            builder.Append("info: \'")
                .Append(additionalInfo)
                .Append("\' ");
        }
        builder.Append("original: \'")
            .Append(exception.Message)
            .Append("\' at: ");
        if (exception.StackTrace is not null)
        {
            builder.Append('\n').Append(exception.StackTrace);
        }
        else
        {
            builder.Append("stacktrace unavailable");
        }
            
        entry.LogMessage = builder.ToString();
        builder.Clear();
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    public virtual void Generate<TEventArgs>(ref LogEntry entry, string? assemblyName, string? className, string instanceName, string eventName, TEventArgs eventArgs)
    {
        StringBuilder builder = GenerateHeader(ref entry, assemblyName, out MethodBase? method)
            .Append('(');
        className ??= method?.DeclaringType?.Name ?? "<UnknownType>";
        entry.ClassName = className;
        builder
            .Append(className)
            .Append("::")
            .Append(instanceName)
            .Append(") ==> ")
            .Append(eventName)
            .Append(": ");

        entry.InstanceName = instanceName;
        entry.EventName = eventName;
        entry.EventArgs = eventArgs;

        AddEventArgs(eventArgs, builder);

        entry.LogMessage = builder.ToString();
        builder.Clear();
    }

    /// <summary>
    /// Adds the <paramref name="args"/> to the <paramref name="builder"/> in a human-readable (JSON) format.
    /// </summary>
    /// <typeparam name="TEventArgs">The type of the event args.</typeparam>
    /// <param name="args">The event args to add.</param>
    /// <param name="builder">The <see cref="StringBuilder"/> to add the <paramref name="args"/> to.</param>
    protected virtual void AddEventArgs<TEventArgs>(TEventArgs args, StringBuilder builder)
    {
        const string nullString = "null";

        builder.Append(typeof(TEventArgs).Name).Append(": ");
        if (args is null)
        {
            builder.Append(nullString);
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
    /// <param name="textAssemblyName">The name of the assembly that is logging (if known). If <see langword="null"/>, the assembly name will be determined using reflection.</param>
    /// <param name="method">(Output) The <see cref="MethodBase"/> of the method that is logging.</param>
    /// <returns>A <see cref="StringBuilder"/> containing the log entry header.</returns>
    [StackTraceHidden]
    protected virtual StringBuilder GenerateHeader(ref LogEntry entry, string? textAssemblyName, out MethodBase? method)
    {
        method = null;
        if (textAssemblyName is null)
        {
            StackTrace stack = new();

            method = stack.GetFirstNonHiddenCaller();
            textAssemblyName = method?.DeclaringType?.Assembly.GetName().Name ?? "<UnknownAssembly>";
        }
        entry.AssemblyName = textAssemblyName;

        string textLogLevel = entry.LogLevel switch
        {
            LogLevel.Error or LogLevel.Fatal => entry.LogLevel.ToString().ToUpper(),
            _ => entry.LogLevel.ToString()
        };
        string mainThreadTag = string.Empty;
        int threadId = Environment.CurrentManagedThreadId;
        entry.ThreadId = threadId;
        if (threadId == _config.MainThreadId)
        {
            mainThreadTag = "(MAIN THREAD)";
            entry.IsMainThread = true;
        }

        StringBuilder builder = _stringBuilder.Value!;
        builder.Clear();
        entry.TimestampUtc = DateTime.UtcNow;
        return builder.Append(entry.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss.fff"))
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
    protected virtual StringBuilder AddTargetSite(ref LogEntry entry, StringBuilder builder, MethodBase? method)
    {
        string textTargetSite = method is null ? "(<UnknownType>)" : GetTargetSite(method);
        entry.TargetSite = textTargetSite;
        return builder
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
            StringBuilder builder = new();
            builder.Append('(').Append(type).Append("::").Append(method.Name).Append('(');
            bool flag = false;
            foreach (ParameterInfo parameter in parameters)
            {
                if (flag)
                {
                    builder.Append(", ");
                }
                builder.Append(parameter.ParameterType.Name);
                flag = true;
            }
            builder.Append("))");
            site = builder.ToString();
            _targetSiteLookup.TryAdd(method, site);
        }
        return site;
    }
}
