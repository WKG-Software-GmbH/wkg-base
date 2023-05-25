using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Wkg.Logging;
using Wkg.Logging.Configuration;

namespace Wkg.Logging.Entries;

public class DefaultLogEntryGenerator : ILogEntryGenerator<DefaultLogEntryGenerator>
{
    private readonly CompiledLoggerConfiguration _config;
    private readonly ConcurrentDictionary<MethodBase, string> _targetSiteLookup = new();
    private readonly ThreadLocal<StringBuilder> _stringBuilder = new(() => new StringBuilder(256), false);

    private DefaultLogEntryGenerator(CompiledLoggerConfiguration config) =>
        _config = config;

    public static DefaultLogEntryGenerator Create(CompiledLoggerConfiguration config) =>
        new(config);

    [StackTraceHidden]
    public string Generate(string title, string message, LogLevel level)
    {
        StringBuilder builder = AddTargetSite(GenerateHeader(level, null, out MethodBase? method), method)
            .Append(title).Append(": \'")
            .Append(message)
            .Append('\'');
        string entry = builder.ToString();
        builder.Clear();
        return entry;
    }

    [StackTraceHidden]
    public string Generate(Exception exception, LogLevel level)
    {
        string stackTrace;
        if (exception.StackTrace == null)
        {
            stackTrace = string.Empty;
        }
        else
        {
            stackTrace = $"\n{exception.StackTrace}";
        }
        StringBuilder builder = AddTargetSite(GenerateHeader(level, null, out MethodBase? method), method)
            .Append(exception.GetType().Name).Append(": \'")
            .Append(exception.Message)
            .Append("\' at: ")
            .Append(stackTrace);
        string entry = builder.ToString();
        builder.Clear();
        return entry;
    }

    [StackTraceHidden]
    public string Generate<TEventArgs>(string? assemblyName, string? className, string instanceName, string eventName, TEventArgs eventArgs)
    {
        StringBuilder builder = GenerateHeader(LogLevel.Event, assemblyName, out MethodBase? method)
            .Append('(')
            .Append(className ?? method?.DeclaringType?.Name ?? "UNKNOWN")
            .Append("::")
            .Append(instanceName)
            .Append(") ==> ")
            .Append(eventName)
            .Append(": ");
        AddEventArgs(eventArgs, builder);

        string entry = builder.ToString();
        builder.Clear();
        return entry;
    }

    private static void AddEventArgs<TEventArgs>(TEventArgs args, StringBuilder builder)
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
                builder.Append($"{nameof(NotSupportedException)} -> ").Append(ex.Message);
            }
        }
    }

    [StackTraceHidden]
    private StringBuilder GenerateHeader(LogLevel level, string? textAssemblyName, out MethodBase? method)
    {
        method = null;
        if (textAssemblyName is null)
        {
            StackFrame frame = new();
            method = frame.GetMethod();
            textAssemblyName = method?.DeclaringType?.Assembly.GetName().Name ?? "UNKNOWN";
        }

        string textLogLevel = level switch
        {
            LogLevel.Error or LogLevel.Fatal => level.ToString().ToUpper(),
            _ => level.ToString()
        };
        string mainThreadTag = Environment.CurrentManagedThreadId == _config.MainThreadId
            ? "(MAIN THREAD)"
            : string.Empty;

        StringBuilder builder = _stringBuilder.Value!;
        builder.Clear();
        return builder.Append($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ")
            .Append(textAssemblyName)
            .Append(": [")
            .Append(textLogLevel)
            .Append("->Thread#0x")
            .Append(Environment.CurrentManagedThreadId.ToString("x"))
            .Append(mainThreadTag)
            .Append("] ");
    }

    private StringBuilder AddTargetSite(StringBuilder builder, MethodBase? method)
    {
        string textTargetSite = method is null ? "(UNKNOWN CALLER)" : GetTargetSite(method);
        return builder
            .Append(textTargetSite)
            .Append(" ==> ");
    }

    private string GetTargetSite(MethodBase method)
    {
        if (!_targetSiteLookup.TryGetValue(method, out string? site))
        {
            string type = method.DeclaringType?.Name ?? "<<UNKNOWN TYPE>>";
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

                if (parameter.Name is not null)
                {
                    builder.Append(' ').Append(parameter.Name);
                }
                flag = true;
            }
            builder.Append("))");
            site = builder.ToString();
            _targetSiteLookup.TryAdd(method, site);
        }
        return site;
    }
}
