using System.Collections.Immutable;
using System.Text;
using Wkg.Logging.Configuration;
using Wkg.Logging.Generators.Helpers;
using Wkg.Text;

namespace Wkg.Logging.Generators;

/// <summary>
/// An AOT compatible <see cref="ILogEntryGenerator"/> implementation with detailed log entries enumerated on compile time that generates log entries in the following format:
/// <code>
/// 2023-05-31 14:14:24.626 (UTC) [Info->Thread_0x1(MAIN THREAD)] (MyClass.cs:L69->MyMethod) ==> Output: 'Hello world! :)'
/// 2023-05-31 14:14:24.626 (UTC) [ERROR->Thread_0x1(MAIN THREAD)] (MyClass.cs:L69->MyMethod) ==> [NullReferenceException] info: 'while trying to do a thing' original: 'Exception message' at:
///   StackTrace line 1
/// 2023-05-31 14:14:24.626 (UTC) [Event->Thread_0x1(MAIN THREAD)] (MyClass.cs:L69->MyMethod) ==> MyAssembly::MyClass::MyButtonInstance::OnClick(MyEventType: eventArgs)
/// </code>
/// </summary>
/// <remarks>
/// This class does not require reflective enumeration of target site information or stack unwinding, making it a good candidate for use in production environments.
/// </remarks>
public class DetailedLogEntryGenerator : ILogEntryGenerator<DetailedLogEntryGenerator>
{
    private const int DEFAULT_STRING_BUILDER_CAPACITY = 512;
    private static readonly ImmutableArray<string> _logLevelNames;

    static DetailedLogEntryGenerator()
    {
        string[] logLevelNames = Enum.GetNames<LogLevel>();
        logLevelNames[(int)LogLevel.Error] = LogLevel.Error.ToString().ToUpperInvariant();
        logLevelNames[(int)LogLevel.Fatal] = LogLevel.Fatal.ToString().ToUpperInvariant();
        _logLevelNames = [.. logLevelNames];
    }

    /// <summary>
    /// The <see cref="CompiledLoggerConfiguration"/> used to create this <see cref="DetailedLogEntryGenerator"/>
    /// </summary>
    protected readonly CompiledLoggerConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailedLogEntryGenerator"/> class.
    /// </summary>
    /// <param name="config">The <see cref="CompiledLoggerConfiguration"/> used to create this <see cref="DetailedLogEntryGenerator"/></param>
    protected DetailedLogEntryGenerator(CompiledLoggerConfiguration config) => 
        _config = config;

    /// <inheritdoc/>
    public static DetailedLogEntryGenerator Create(CompiledLoggerConfiguration config) => 
        new(config);

    /// <inheritdoc/>
    public virtual void Generate(ref LogEntry entry, string title, string message)
    {
        // 2023-05-31 14:14:24.626 (UTC) [Info->Thread_0x1(MAIN THREAD)] (MyClass.cs:L69->MyMethod) ==> Output: 'Hello world! :)'
        StringBuilder builder = StringBuilderPool.Shared.Rent(DEFAULT_STRING_BUILDER_CAPACITY).Clear();

        AddHeader(ref entry, builder);
        builder.Append("Output: '")
            .Append(message)
            .Append('\'');
        entry.LogMessage = builder.ToString();

        StringBuilderPool.Shared.Return(builder);
    }

    /// <inheritdoc/>
    public virtual void Generate(ref LogEntry entry, Exception exception, string? additionalInfo)
    {
        // 2023-05-31 14:14:24.626 (UTC) [ERROR->Thread_0x1(MAIN THREAD)] (MyClass.cs:L69->MyMethod) ==> [NullReferenceException] info: 'while trying to do a thing' original: 'Exception message' at:
        //   StackTrace line 1

        // stack traces can be very long, so we request a larger capacity
        StringBuilder builder = StringBuilderPool.Shared.Rent(minimumCapacity: 8192).Clear();

        entry.Exception = exception;
        AddHeader(ref entry, builder);
        builder.Append('[')
            .Append(exception.GetType().Name)
            .Append("] ");
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
        string? stackTrace = exception.StackTrace;
        if (stackTrace is null)
        {
            builder.Append("<stack trace unavailable>");
        }
        else
        {
            builder.AppendLine()
                .Append(exception.StackTrace);
        }
        entry.LogMessage = builder.ToString();

        StringBuilderPool.Shared.Return(builder);
    }

    /// <inheritdoc/>
    public virtual void Generate<TEventArgs>(ref LogEntry entry, string? assemblyName, string? className, string instanceName, string eventName, TEventArgs eventArgs)
    {
        // 2023-05-31 14:14:24.626 (UTC) [Event->Thread_0x1(MAIN THREAD)] (MyClass.cs:L69->MyMethod) ==> MyAssembly::MyClass::MyButtonInstance::OnClick(MyEventType: eventArgs)
        StringBuilder builder = StringBuilderPool.Shared.Rent(DEFAULT_STRING_BUILDER_CAPACITY).Clear();

        AddHeader(ref entry, builder);

        if (assemblyName is not null && className is not null)
        {
            entry.AssemblyName = assemblyName;
            entry.ClassName = className;
            builder.Append(assemblyName)
                .Append("::")
                .Append(className)
                .Append("::");
        }
        entry.InstanceName = instanceName;
        entry.EventName = eventName;
        builder.Append(instanceName)
            .Append("::")
            .Append(eventName)
            .Append('(');
        entry.EventArgs = eventArgs;
        AddEventArgs(eventArgs, builder);
        builder.Append(')');
        entry.LogMessage = builder.ToString();

        StringBuilderPool.Shared.Return(builder);
    }

    /// <summary>
    /// Generates the header for the <paramref name="entry"/> and appends it to the <paramref name="builder"/>.
    /// </summary>
    /// <remarks>
    /// <code>
    /// 2023-05-31 14:14:24.626 (UTC) [Event->Thread_0x1(MAIN THREAD)] (MyClass.cs:L69->MyMethod) ==>
    /// </code>
    /// </remarks>
    /// <param name="entry">The <see cref="LogEntry"/> to generate the header for.</param>
    /// <param name="builder">The <see cref="StringBuilder"/> to append the header to.</param>
    protected virtual void AddHeader(ref LogEntry entry, StringBuilder builder)
    {
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
        int fileNameIndex = entry.CallerInfo.GetFileNameStartIndex();
        builder.Append(entry.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Append(" (UTC) [")
            .Append(textLogLevel)
            .Append("->Thread_0x")
            .Append(threadId.ToString("x"))
            .Append(mainThreadTag)
            .Append("] (")
            .Append(entry.CallerInfo.FilePath.AsSpan()[fileNameIndex..])
            .Append(":L")
            .Append(entry.CallerInfo.LineNumber)
            .Append("->")
            .Append(entry.CallerInfo.MemberName)
            .Append(") ==> ");
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
            .Append(args?.ToString() ?? "null");
}
