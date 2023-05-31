using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Wkg.Logging.Configuration;
using Wkg.Logging.Intrinsics.CallStack;

namespace Wkg.Logging.Generators;

public class TracingLogEntryGenerator : ILogEntryGenerator<TracingLogEntryGenerator>
{
    protected static readonly ThreadLocal<StringBuilder> _stringBuilder = new(() => new StringBuilder(512), false);
    protected readonly CompiledLoggerConfiguration _config;
    protected readonly ConcurrentDictionary<MethodBase, string> _targetSiteLookup = new();

    protected TracingLogEntryGenerator(CompiledLoggerConfiguration config) =>
        _config = config;

    public static TracingLogEntryGenerator Create(CompiledLoggerConfiguration config) =>
        new(config);

    [StackTraceHidden]
    public virtual string Generate(string title, string message, LogLevel level)
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
    public virtual string Generate(Exception exception, string? additionalInfo, LogLevel level)
    {
        StringBuilder builder = AddTargetSite(GenerateHeader(level, null, out MethodBase? method), method)
            .Append(exception.GetType().Name)
            .Append(": ");
        if (additionalInfo is not null)
        {
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
            
        string entry = builder.ToString();
        builder.Clear();
        return entry;
    }

    [StackTraceHidden]
    public virtual string Generate<TEventArgs>(string? assemblyName, string? className, string instanceName, string eventName, TEventArgs eventArgs)
    {
        StringBuilder builder = GenerateHeader(LogLevel.Event, assemblyName, out MethodBase? method)
            .Append('(')
            .Append(className ?? method?.DeclaringType?.Name ?? "<UnknownType>")
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

    [StackTraceHidden]
    protected virtual StringBuilder GenerateHeader(LogLevel level, string? textAssemblyName, out MethodBase? method)
    {
        method = null;
        if (textAssemblyName is null)
        {
            StackTrace stack = new();

            method = stack.GetFirstNonHiddenCaller();
            textAssemblyName = method?.DeclaringType?.Assembly.GetName().Name ?? "<UnknownAssembly>";
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
        return builder.Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Append(" (UTC) ")
            .Append(textAssemblyName)
            .Append(": [")
            .Append(textLogLevel)
            .Append("->Thread_0x")
            .Append(Environment.CurrentManagedThreadId.ToString("x"))
            .Append(mainThreadTag)
            .Append("] ");
    }

    protected virtual StringBuilder AddTargetSite(StringBuilder builder, MethodBase? method)
    {
        string textTargetSite = method is null ? "(<UnknownType>)" : GetTargetSite(method);
        return builder
            .Append(textTargetSite)
            .Append(" ==> ");
    }

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
