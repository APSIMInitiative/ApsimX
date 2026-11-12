using CommandLine;

namespace APSIM.Workflow.Verbs;

/// <summary>Command line options for the locations verb.</summary>
[Verb("locations", HelpText = "Print the absolute paths of valid validation directories.")]
public class LocationsOptions
{
    /// <summary>Gets or sets a value indicating whether verbose output is enabled.</summary>
    [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
    public bool Verbose { get; set; }
}
