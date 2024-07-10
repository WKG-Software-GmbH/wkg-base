using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Wkg.Logging.Configuration;
using Wkg.Text;

namespace Wkg.Logging.Generators;

/// <summary>
/// A variant of the <see cref="TracingLogEntryGenerator"/> that includes parameter names in the target site, generating log entries in the following format:
/// <code>
/// 2023-05-31 14:14:24.626 (UTC) Wkg: [Info->Thread_0x1(MAIN THREAD)] (MyClass::MyMethod(String[] arguments, Boolean sendHelp)) ==> Output: 'Hello world! :)'
/// </code>
/// </summary>
/// <remarks>
/// This class requires reflective enumeration of target site information and stack unwinding, resulting in a performance penalty for extensive logging.
/// It is recommended to use this class only in development environments.
/// </remarks>
[RequiresUnreferencedCode("Requires reflective access to the event args type for JSON serialization.")]
public class TracingLogEntryGeneratorWithParamNames : TracingLogEntryGenerator, ILogEntryGenerator<TracingLogEntryGeneratorWithParamNames>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TracingLogEntryGeneratorWithParamNames"/> class.
    /// </summary>
    /// <param name="config">The <see cref="CompiledLoggerConfiguration"/> used to create this <see cref="TracingLogEntryGeneratorWithParamNames"/></param>
    protected TracingLogEntryGeneratorWithParamNames(CompiledLoggerConfiguration config) : base(config)
    {
    }

    /// <inheritdoc/>
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
    public static new TracingLogEntryGeneratorWithParamNames Create(CompiledLoggerConfiguration config) =>
        new(config);
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.

    /// <inheritdoc/>
    protected override string GetTargetSite(MethodBase method)
    {
        if (!_targetSiteLookup.TryGetValue(method, out string? site))
        {
            StringBuilder targetBuilder = StringBuilderPool.Shared.Rent(256);

            string type = method.DeclaringType?.Name ?? "<UnknownType>";
            ParameterInfo[] parameters = method.GetParameters();
            targetBuilder.Append('(')
                .Append(type)
                .Append("::")
                .Append(method.Name)
                .Append('(');
            bool flag = false;
            foreach (ParameterInfo parameter in parameters)
            {
                if (flag)
                {
                    targetBuilder.Append(", ");
                }

                targetBuilder.Append(parameter.ParameterType.Name);

                if (parameter.Name is not null)
                {
                    targetBuilder.Append(' ').Append(parameter.Name);
                }
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
