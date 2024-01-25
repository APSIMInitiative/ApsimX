using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace APSIM.Cli.Options
{
    /// <summary>
    /// APSIM command-line options for importing an old apsim file.
    /// </summary>
    [Verb("import", HelpText = "Import an old .apsim file")]
    public class ImportOptions
    {
        /// <summary>Files to be imported.</summary>
        [Value(0, HelpText = ".apsim file(s) to be imported.", MetaName = "ApsimXFileSpec", Required = true)]
        public IEnumerable<string> Files { get; set; }

        /// <summary>
        /// Recursively search through subdirectories for files matching the file specification.
        /// </summary>
        [Option('r', "recursive", HelpText = "Recursively search through subdirectories for files matching the file specification.")]
        public bool Recursive { get; set; }

        /// <summary>
        /// Concrete examples shown in help text.
        /// </summary>
        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Normal usage",
                                         new ImportOptions()
                                         {
                                             Files = new[] { "file.apsim", "file2.apsim" }
                                         });
                yield return new Example("Recursively search subdirectories",
                                         new ImportOptions()
                                         {
                                             Files = new[] { "*.apsim" },
                                             Recursive = true
                                         });
            }
        }
    }
}
