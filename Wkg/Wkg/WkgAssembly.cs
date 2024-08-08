using Wkg.Versioning;

namespace Wkg;

/// <summary>
/// Provides version information for the Wkg assembly.
/// </summary>
public class WkgAssembly : DeploymentVersionInfo
{
    private const string CI_DEPLOYMENT__VERSION_PREFIX = "0.0.0";
    private const string CI_DEPLOYMENT__VERSION_SUFFIX = "CI-INJECTED";
    private const string CI_DEPLOYMENT__DATETIME_UTC = "1970-01-01 00:00:00";

    private WkgAssembly() : base(CI_DEPLOYMENT__VERSION_PREFIX, CI_DEPLOYMENT__VERSION_SUFFIX, CI_DEPLOYMENT__DATETIME_UTC) => Pass();

    /// <summary>
    /// Retrieves the version information for the Wkg assembly.
    /// </summary>
    public static WkgAssembly VersionInfo { get; } = new();
}
