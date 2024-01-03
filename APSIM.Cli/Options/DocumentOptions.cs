using System;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace APSIM.Cli.Options
{
    /// <summary>
    /// Command-line options for Models.exe.
    /// </summary>
    [Verb("document", HelpText = "Document one or more files")]
    public class DocumentOptions
    {
        /// <summary>Files to be run.</summary>
        [Value(0, HelpText = ".apsimx file(s) to be run.", MetaName = "ApsimXFileSpec", Required = true)]
        public IEnumerable<string> Files { get; set; }

        /// <summary>
        /// Recursively search through subdirectories for files matching the file specification.
        /// </summary>
        [Option("recursive", HelpText = "Recursively search through subdirectories for files matching the file specification.")]
        public bool Recursive { get; set; }

        /// <summary>
        /// Generate Params/Inputs/Outputs Documentation.
        /// </summary>
        [Option("params", HelpText = "Generate Params/Inputs/Outputs Documentation")]
        public bool ParamsDocs { get; set; }

        /// <summary>
        /// Generate documentation for a model at the given path inside the file.
        /// </summary>
        /// <value></value>
        [Option('p', "path", HelpText = "Generate documentation for a model at the given path inside the file")]
        public string Path { get; set; }

        /// <summary>
        /// Concrete examples shown in help text.
        /// </summary>
        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Normal usage",
                                         new DocumentOptions()
                                         {
                                             Files = new[] { "file.apsimx", "file2.apsimx" }
                                         });
            }
        }
    }
}
