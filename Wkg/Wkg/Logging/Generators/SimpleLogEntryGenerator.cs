﻿using System.Text;
using Wkg.Logging.Configuration;
using Wkg.Logging.Generators.Helpers;
using Wkg.Text;

namespace Wkg.Logging.Generators;

/// <summary>
/// A AOT compatible, simple, and lightweight <see cref="ILogEntryGenerator"/> implementation that generates log entries in the following format:
/// <code>
/// 2023-05-30 14:35:42.185 (UTC) Info on Thread_0x123 --> Output: 'This is a log message';
/// 2023-05-30 14:35:42.185 (UTC) Error: SomeException on Thread_0x123 --> info: 'while trying to do a thing' original: 'Exception message' at:
///   StackTrace line 1
/// 2023-05-30 14:35:42.185 (UTC) Event on Thread_0x123 --> (MyAssembly) (MyClass::MyButtonInstance) ==> OnClick(MyEventType: eventArgs)
/// </code>
/// </summary>
/// <remarks>
/// This class does not require reflective enumeration of target site information or stack unwinding, making it a good candidate for use in production environments.
/// </remarks>
public class SimpleLogEntryGenerator : ILogEntryGenerator<SimpleLogEntryGenerator>
{
    private const int DEFAULT_STRING_BUILDER_CAPACITY = 512;

    /// <summary>
    /// The <see cref="CompiledLoggerConfiguration"/> used to create this <see cref="SimpleLogEntryGenerator"/>
    /// </summary>
    protected readonly CompiledLoggerConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleLogEntryGenerator"/> class.
    /// </summary>
    /// <param name="config">The <see cref="CompiledLoggerConfiguration"/> used to create this <see cref="SimpleLogEntryGenerator"/></param>
    protected SimpleLogEntryGenerator(CompiledLoggerConfiguration config) => 
        _config = config;

    /// <inheritdoc/>
    public static SimpleLogEntryGenerator Create(CompiledLoggerConfiguration config) => 
        new(config);

    /// <inheritdoc/>
    public virtual void Generate(ref LogEntry entry, string title, string message)
    {
        // 2023-05-30 14:35:42.185 (UTC) Info on Thread_0x123 --> Output: 'This is a log message';
        StringBuilder builder = StringBuilderPool.Shared.Rent(DEFAULT_STRING_BUILDER_CAPACITY);

        AddPrefix(ref entry, builder);
        builder.Append(" on ");
        AddThreadInfo(ref entry, builder);
        builder.Append(" --> ")
            .Append(title)
            .Append(": \'")
            .Append(message)
            .Append('\'');
        entry.LogMessage = builder.ToString();

        StringBuilderPool.Shared.Return(builder);
    }

    /// <inheritdoc/>
    public virtual void Generate(ref LogEntry entry, Exception exception, string? additionalInfo)
    {
        // 2023-05-30 14:35:42.185 (UTC) Error: SomeException on Thread_0x123 --> info: 'while trying to do a thing' original: 'Exception message' at:
        //    StackTrace line 1
        //    StackTrace line 2
        //    StackTrace line 3

        // stack traces can be very long, so we request a larger capacity
        StringBuilder builder = StringBuilderPool.Shared.Rent(minimumCapacity: 8192);

        entry.Exception = exception;
        AddPrefix(ref entry, builder);
        builder.Append(": ")
            .Append(exception.GetType().Name)
            .Append(" on ");
        AddThreadInfo(ref entry, builder);
        builder.Append(" --> ");
        if (additionalInfo is not null)
        {
            entry.AdditionalInfo = additionalInfo;
            builder.Append("info: \'")
                .Append(additionalInfo)
                .Append("\' ");
        }
        builder.Append("original: \'")
            .Append(exception.Message)
            .Append("\' at: \n")
            .Append(exception.StackTrace);
        entry.LogMessage = builder.ToString();

        StringBuilderPool.Shared.Return(builder);
    }

    /// <inheritdoc/>
    public virtual void Generate<TEventArgs>(ref LogEntry entry, string? assemblyName, string? className, string instanceName, string eventName, TEventArgs eventArgs)
    {
        // 2023-05-30 14:35:42.185 (UTC) Event on Thread_0x123 --> (MyAssembly) (MyClass::MyButtonInstance) ==> OnClick(MyEventType: eventArgs)
        StringBuilder builder = StringBuilderPool.Shared.Rent(DEFAULT_STRING_BUILDER_CAPACITY);

        AddPrefix(ref entry, builder);
        builder.Append(" on ");
        AddThreadInfo(ref entry, builder);
        builder.Append(" --> (");
        if (assemblyName is not null && className is not null)
        {
            entry.AssemblyName = assemblyName;
            entry.ClassName = className;
            builder.Append(assemblyName)
                .Append(") (")
                .Append(className)
                .Append("::");
        }
        entry.InstanceName = instanceName;
        entry.EventName = eventName;
        builder.Append(instanceName)
            .Append(") ==> ")
            .Append(eventName)
            .Append('(');
        entry.EventArgs = eventArgs;
        AddEventArgs(eventArgs, builder);
        builder.Append(')');
        entry.LogMessage = builder.ToString();

        StringBuilderPool.Shared.Return(builder);
    }

    /// <summary>
    /// Adds the <paramref name="args"/> to the <paramref name="builder"/> calling <see cref="object.ToString"/> on the <paramref name="args"/>.
    /// </summary>
    /// <typeparam name="TEventArgs">The type of the event args.</typeparam>
    /// <param name="args">The event args to add.</param>
    /// <param name="builder">The <see cref="StringBuilder"/> to add the <paramref name="args"/> to.</param>
    protected virtual void AddEventArgs<TEventArgs>(TEventArgs args, StringBuilder builder) => 
        builder.Append(typeof(TEventArgs).Name)
            .Append(": ")
            .Append(args?.ToString());

    /// <summary>
    /// Adds the current thread's ID to the <paramref name="builder"/>.
    /// </summary>
    /// <remarks>
    /// By default, the thread ID is added in the following format:
    /// <code>
    /// Thread_0x1c8 (Main Thread)
    /// </code>
    /// </remarks>
    /// <param name="entry">The <see cref="LogEntry"/> to add the thread info to.</param>
    /// <param name="builder">The <see cref="StringBuilder"/> to add the thread info to.</param>
    protected virtual void AddThreadInfo(ref LogEntry entry, StringBuilder builder)
    {
        int threadId = Environment.CurrentManagedThreadId;
        entry.ThreadId = threadId;
        builder.Append("Thread_0x")
            .Append(threadId.ToString("x"));
        if (threadId == _config.MainThreadId)
        {
            entry.IsMainThread = true;
            builder.Append(" (Main Thread)");
        }
    }

    /// <summary>
    /// Adds the log prefix to the <paramref name="builder"/> and the <paramref name="entry"/>.
    /// </summary>
    /// <remarks>
    /// By default, the log prefix is added in the following format:
    /// <code>
    /// 2023-05-30 14:35:42.185 (UTC) Warning
    /// </code>
    /// </remarks>
    /// <param name="entry">The <see cref="LogEntry"/> to add the prefix to.</param>
    /// <param name="builder">The <see cref="StringBuilder"/> to add the prefix to.</param>
    protected virtual void AddPrefix(ref LogEntry entry, StringBuilder builder)
    {
        string logLevel = LogLevelNames.NameForOrUnknown(entry.LogLevel);
        entry.TimestampUtc = DateTime.UtcNow;
        builder
            .Append(entry.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Append(" (UTC) ")
            .Append(logLevel);
    }
}
