using System.Globalization;

namespace Wkg.Versioning;

/// <summary>
/// Represents the version information of a continuous integration deployment.
/// </summary>
/// <param name="versionPrefix">
/// The version prefix of the deployment.
/// <para>
/// This value should be injected by the continuous integration pipeline.
/// Ensure that your implementation contains the following line verbatim and that your implementation file is referenced in the gitlab-ci.yml file.
/// <code>
/// private const string CI_DEPLOYMENT__VERSION_PREFIX = "0.0.0";
/// </code>
/// </para>
/// </param>
/// <param name="versionSuffix">
/// The version suffix of the deployment.
/// <para>
/// This value should be injected by the continuous integration pipeline.
/// Ensure that your implementation contains the following line verbatim and that your implementation file is referenced in the gitlab-ci.yml file.
/// <code>
/// private const string CI_DEPLOYMENT__VERSION_SUFFIX = "CI-INJECTED";
/// </code>
/// </para>
/// </param>
/// <param name="dateTimeUtc">
/// The date and time of the deployment in UTC.
/// <para>
/// This value should be injected by the continuous integration pipeline.
/// Ensure that your implementation contains the following line verbatim and that your implementation file is referenced in the gitlab-ci.yml file.
/// <code>
/// private const string CI_DEPLOYMENT__DATETIME_UTC = "1970-01-01 00:00:00";
/// </code>
/// </para>
/// </param>
public abstract class DeploymentVersionInfo(string versionPrefix, string versionSuffix, string dateTimeUtc)
{
    /// <summary>
    /// Retrieves the version of the deployment.
    /// </summary>
    public Version Version { get; } = new Version(versionPrefix);

    /// <summary>
    /// Retrieves the date and time of the deployment in UTC.
    /// </summary>
    public DateTime BuildDateUtc { get; } = DateTime.ParseExact(dateTimeUtc, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>
    /// Indicates whether the deployment is a pre-release version.
    /// </summary>
    public bool IsPreRelease { get; } = !string.IsNullOrWhiteSpace(versionSuffix);

    /// <summary>
    /// Retrieves the pre-release tag of the deployment, or <see langword="null"/> if the deployment is not a pre-release version.
    /// </summary>
    public string? PreReleaseTag { get; } = string.IsNullOrWhiteSpace(versionSuffix) ? null : versionSuffix;

    /// <summary>
    /// Retrieves the semantic version string of the deployment.
    /// </summary>
    public string VersionString { get; } = string.IsNullOrWhiteSpace(versionSuffix)
        ? versionPrefix
        : $"{versionPrefix}-{versionSuffix}";

    /// <summary>
    /// Indicates whether the deployment has been built in debug mode.
    /// </summary>
    public bool IsDebugBuild => versionSuffix is "diag" or "debug";
}
