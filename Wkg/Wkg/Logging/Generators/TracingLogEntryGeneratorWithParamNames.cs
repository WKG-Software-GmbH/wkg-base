using System.Reflection;
using System.Text;
using Wkg.Logging.Configuration;

namespace Wkg.Logging.Generators;

/// <summary>
/// A veriant of the <see cref="TracingLogEntryGenerator"/> that includes parameter names in the target site, generating log entries in the following format:
/// <code>
/// 2023-05-31 14:14:24.626 (UTC) Wkg: [Info->Thread_0x1(MAIN THREAD)] (MyClass::MyMethod(String[] arguments, Boolean sendHelp)) ==> Output: 'Hello world! :)'
/// </code>
/// </summary>
/// <remarks>
/// This class requires reflective enumeration of target site information and stack unwinding, resulting in a performance penalty for extensive logging.
/// It is recommended to use this class only in development environments.
/// </remarks>
public class TracingLogEntryGeneratorWithParamNames : TracingLogEntryGenerator, ILogEntryGenerator<TracingLogEntryGeneratorWithParamNames>
{
    private TracingLogEntryGeneratorWithParamNames(CompiledLoggerConfiguration config) : base(config)
    {
    }

    /// <inheritdoc/>
    public static new TracingLogEntryGeneratorWithParamNames Create(CompiledLoggerConfiguration config) =>
        new(config);

    /// <inheritdoc/>
    protected override string GetTargetSite(MethodBase method)
    {
        if (!_targetSiteLookup.TryGetValue(method, out string? site))
        {
            string type = method.DeclaringType?.Name ?? "<UnknownType>";
            ParameterInfo[] parameters = method.GetParameters();
            StringBuilder builder = new();
            builder.Append('(')
                .Append(type)
                .Append("::")
                .Append(method.Name)
                .Append('(');
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
