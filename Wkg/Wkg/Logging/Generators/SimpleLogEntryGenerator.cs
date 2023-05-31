using System.Text;
using Wkg.Logging.Configuration;

namespace Wkg.Logging.Generators;

public sealed class SimpleLogEntryGenerator : ILogEntryGenerator<SimpleLogEntryGenerator>
{
    private static readonly ThreadLocal<StringBuilder> _stringBuilder = new(() => new StringBuilder(512), false);
    private readonly CompiledLoggerConfiguration _config;

    private SimpleLogEntryGenerator(CompiledLoggerConfiguration config) => 
        _config = config;

    public static SimpleLogEntryGenerator Create(CompiledLoggerConfiguration config) => 
        new(config);

    /// <inheritdoc/>
    public string Generate(string title, string message, LogLevel level)
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
    public string Generate(Exception exception, string? additionalInfo, LogLevel level)
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
    public string Generate<TEventArgs>(string? assemblyName, string? className, string instanceName, string eventName, TEventArgs eventArgs)
    {
        // 2023-05-30 14:35:42.185 (UTC) Event on Thread_0x123 --> (MyAssembly) (MyClass::MyButtonInstance) ==> OnClick(eventArgs)
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
            .Append('(')
            .Append(eventArgs)
            .Append(')');
        return builder.ToString();
    }

    // Thread_0x1c8 (Main Thread)
    private void AddThreadInfo(StringBuilder builder)
    {
        int threadId = Environment.CurrentManagedThreadId;
        builder.Append("Thread_0x")
            .Append(threadId.ToString("x"));
        if (threadId == _config.MainThreadId)
        {
            builder.Append(" (Main Thread)");
        }
    }

    // 2023-05-30 14:35:42.185 (UTC) Warning
    private static void AddPrefix(StringBuilder builder, LogLevel level) => builder
        .Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"))
        .Append(" (UTC) ")
        .Append(level);
}
