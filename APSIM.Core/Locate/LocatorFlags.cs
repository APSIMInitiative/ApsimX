namespace APSIM.Core;

/// <summary>
/// Flags to control options in the Locator
/// </summary>
[Flags]
public enum LocatorFlags
{
    /// <summary>
    /// The default - treats the other options as "false"
    /// </summary>
    None = 0,

    /// <summary>
    /// If set, does a case-sensitive search; otherwise is case-insensitive
    /// </summary>
    CaseSensitive = 1,

    /// <summary>
    /// If set, fetch only property information, but not the value
    /// </summary>
    PropertiesOnly = 2,

    /// <summary>
    /// If set, disabled models are included in the search; otherwise they are excluded
    /// </summary>
    IncludeDisabled = 4,

    /// <summary>
    /// If set, any "errors" will result in an exception being thrown; otherwise null is returned
    /// </summary>
    ThrowOnError = 8,

    /// <summary>
    /// If set, fetch only model references, do not return properties or methods of the same name
    /// </summary>
    ModelsOnly = 32,
};