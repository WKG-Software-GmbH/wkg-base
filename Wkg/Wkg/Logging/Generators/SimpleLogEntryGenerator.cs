using System.Text;
using Wkg.Logging.Configuration;

namespace Wkg.Logging.Generators;

/// <summary>
/// A simple and lightweight <see cref="ILogEntryGenerator"/> implementation that generates log entries in the following format:
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
    /// <summary>
    /// A thread-local <see cref="StringBuilder"/> cache to avoid unnecessary allocations
    /// </summary>
    protected static readonly ThreadLocal<StringBuilder> _stringBuilder = new(() => new StringBuilder(512), false);

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
    public virtual string Generate(string title, string message, LogLevel level)
    {
        // 2023-05-30 14:35:42.185 (UTC) Info on Thread_0x123 --> Output: 'This is a log message';
        StringBuilder builder = _stringBuilder.Value!;
        builder.Clear();
        AddPrefix(builder, level);
        builder.Append(" on ");
        AddThreadInfo(builder);
        builder.Append(" --> ")
            .Append(title)
            .Append(": \'")
            .Append(message)
            .Append('\'');
        return builder.ToString();
    }

    /// <inheritdoc/>
    public virtual string Generate(Exception exception, string? additionalInfo, LogLevel level)
    {
        // 2023-05-30 14:35:42.185 (UTC) Error: SomeException on Thread_0x123 --> info: 'while trying to do a thing' original: 'Exception message' at:
        //    StackTrace line 1
        //    StackTrace line 2
        //    StackTrace line 3
        StringBuilder builder = _stringBuilder.Value!;
        builder.Clear();
        AddPrefix(builder, level);
        builder.Append(": ")
            .Append(exception.GetType().Name)
            .Append(" on ");
        AddThreadInfo(builder);
        builder.Append(" --> ");
        if (additionalInfo is not null)
        {
            builder.Append("info: \'")
                .Append(additionalInfo)
                .Append("\' ");
        }
        builder.Append("original: \'")
            .Append(exception.Message)
            .Append("\' at: \n")
            .Append(exception.StackTrace);
        return builder.ToString();
    }

    /// <inheritdoc/>
    public virtual string Generate<TEventArgs>(string? assemblyName, string? className, string instanceName, string eventName, TEventArgs eventArgs)
    {
        // 2023-05-30 14:35:42.185 (UTC) Event on Thread_0x123 --> (MyAssembly) (MyClass::MyButtonInstance) ==> OnClick(MyEventType: eventArgs)
        StringBuilder builder = _stringBuilder.Value!;
        builder.Clear();
        AddPrefix(builder, LogLevel.Event);
        builder.Append(" on ");
        AddThreadInfo(builder);
        builder.Append(" --> (");
        if (assemblyName is not null && className is not null)
        {
            builder.Append(assemblyName)
                .Append(") (")
                .Append(className)
                .Append("::");
        }
        builder.Append(instanceName)
            .Append(") ==> ")
            .Append(eventName)
            .Append('(');
        AddEventArgs(eventArgs, builder);
        builder.Append(')');
        return builder.ToString();
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
    /// <param name="builder">The <see cref="StringBuilder"/> to add the thread info to.</param>
    protected virtual void AddThreadInfo(StringBuilder builder)
    {
        int threadId = Environment.CurrentManagedThreadId;
        builder.Append("Thread_0x")
            .Append(threadId.ToString("x"));
        if (threadId == _config.MainThreadId)
        {
            builder.Append(" (Main Thread)");
        }
    }

    /// <summary>
    /// Adds the log prefix to the <paramref name="builder"/> for the specified <paramref name="level"/>.
    /// </summary>
    /// <remarks>
    /// By default, the log prefix is added in the following format:
    /// <code>
    /// 2023-05-30 14:35:42.185 (UTC) Warning
    /// </code>
    /// </remarks>
    /// <param name="builder">The <see cref="StringBuilder"/> to add the prefix to.</param>
    /// <param name="level">The <see cref="LogLevel"/> to add the prefix for.</param>
    protected virtual void AddPrefix(StringBuilder builder, LogLevel level) => builder
        .Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"))
        .Append(" (UTC) ")
        .Append(level);
}
