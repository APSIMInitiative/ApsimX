using APSIM.Cli.Options;
using APSIM.Interop.Documentation;
using APSIM.Shared.Documentation;
using APSIM.Shared.Utilities;
using CommandLine;
using Models.Core;
using Models.Core.ApsimFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace APSIM.Cli
{
    class Program
    {
        private static int exitCode = 0;

        static int Main(string[] args)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                new Parser(config =>
                {
                    config.AutoHelp = true;
                    config.HelpWriter = Console.Out;
                }).ParseArguments<RunOptions, DocumentOptions>(args)
                .WithParsed<RunOptions>(Run)
                .WithParsed<DocumentOptions>(Document)
                .WithNotParsed(HandleParseError);
            }
            catch (Exception err)
            {
                Console.Error.WriteLine(err.ToString());
                exitCode = 1;
            }
            return exitCode;
        }

        /// <summary>
        /// Handles parser errors to ensure that a non-zero exit code
        /// is returned when parse errors are encountered.
        /// </summary>
        /// <param name="errors">Parse errors.</param>
        private static void HandleParseError(IEnumerable<Error> errors)
        {
            if ( !(errors.IsHelp() || errors.IsVersion()) )
                exitCode = 1;
        }

        private static void Run(RunOptions options)
        {
            throw new NotImplementedException();
        }

        private static void Document(DocumentOptions options)
        {
            if (options.Files == null || !options.Files.Any())
                throw new ArgumentException($"No files were specified");
            IEnumerable<string> files = options.Files.SelectMany(f => DirectoryUtilities.FindFiles(f, options.Recursive));
            if (!files.Any())
                files = options.Files;
            foreach (string file in files)
            {
                Simulations model = FileFormat.ReadFromFile<Simulations>(file, e => throw e, false);
                if (Path.GetExtension(file) == ".json")
                    model.Links.Resolve(model, true, true, false);
                string pdfFile = Path.ChangeExtension(file, ".pdf");
                string directory = Path.GetDirectoryName(file);
                PdfWriter writer = new PdfWriter(new PdfOptions(directory, null));
                writer.Write(pdfFile, model.Document());
            }
        }
    }
}
