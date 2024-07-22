using System.Diagnostics.CodeAnalysis;
using Wkg.Versioning;

namespace Wkg;

/// <summary>
/// Provides version information for the Wkg assembly.
/// </summary>
[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Requires special naming for version injection.")]
public class WkgAssembly : DeploymentVersionInfo
{
    private const string __CI_DEPLOYMENT_VERSION_PREFIX = "0.0.0";
    private const string __CI_DEPLOYMENT_VERSION_SUFFIX = "CI-INJECTED";
    private const string __CI_DEPLOYMENT_DATETIME_UTC = "1970-01-01 00:00:00";

    private WkgAssembly() : base(__CI_DEPLOYMENT_VERSION_PREFIX, __CI_DEPLOYMENT_VERSION_SUFFIX, __CI_DEPLOYMENT_DATETIME_UTC) => Pass();

    /// <summary>
    /// Retrieves the version information for the Wkg assembly.
    /// </summary>
    public static WkgAssembly VersionInfo { get; } = new();
}
