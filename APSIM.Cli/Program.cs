using APSIM.Cli.Options;
using APSIM.Interop.Documentation;
using APSIM.Services.Documentation;
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
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            new Parser(config =>
            {
                config.AutoHelp = true;
                config.HelpWriter = Console.Out;
            }).ParseArguments<RunOptions, DocumentOptions>(args)
              .WithParsed<RunOptions>(Run)
              .WithParsed<DocumentOptions>(Document)
              .WithNotParsed(HandleParseError);
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

        }

        private static void Document(DocumentOptions options)
        {
            IEnumerable<string> files = options.Files.SelectMany(f => DirectoryUtilities.FindFiles(f, options.Recursive));
            if (files == null || !files.Any())
                throw new ArgumentException($"No files were specified");
            foreach (string file in files)
            {
                IModel model = FileFormat.ReadFromFile<Simulations>(file, out List<Exception> errors);
                string pdfFile = Path.ChangeExtension(file, ".pdf");
                PdfWriter.Write(pdfFile, GetTags(model));
            }
        }

        private static IEnumerable<ITag> GetTags(IModel model)
        {
            if (model.IncludeInDocumentation)
                foreach (ITag tag in model.Document())
                    yield return tag;
            foreach (IModel child in model.Children)
                foreach (ITag tag in GetTags(child))
                    yield return tag;
        }
    }
}
