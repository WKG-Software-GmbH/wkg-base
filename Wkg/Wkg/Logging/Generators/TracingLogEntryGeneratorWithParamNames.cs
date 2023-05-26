using System.Reflection;
using System.Text;
using Wkg.Logging.Configuration;

namespace Wkg.Logging.Generators;

public class TracingLogEntryGeneratorWithParamNames : TracingLogEntryGenerator, ILogEntryGenerator<TracingLogEntryGeneratorWithParamNames>
{
    private TracingLogEntryGeneratorWithParamNames(CompiledLoggerConfiguration config) : base(config)
    {
    }

    public static TracingLogEntryGeneratorWithParamNames Create(CompiledLoggerConfiguration config) =>
        new(config);

    protected override string GetTargetSite(MethodBase method)
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
