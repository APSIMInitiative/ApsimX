using CommandLine;

namespace APSIM.Workflow.Verbs;

/// <summary>Command line options for the split verb.</summary>
[Verb("split", HelpText = "Split an APSIMX file.")]
public class SplitOptions
{
    /// <summary>Gets or sets the APSIMX file to split.</summary>
    [Option('d', "payload-directory", Required = true, HelpText = "Path of the APSIMX file to split.")]
    public string DirectoryPath { get; set; } = "";

    /// <summary>Gets or sets the splitting rules file.</summary>
    [Option('s', "splitfiles", Required = true, HelpText = "Splitting rules file for the APSIMX file.")]
    public string SplitFile { get; set; } = "";
}
