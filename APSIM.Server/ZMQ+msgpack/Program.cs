using APSIM.ZMQServer;
using CommandLine;
using Models.Core;
using Models.Core.ApsimFile;
using System;
using System.Collections.Generic;
using System.Text;

namespace APSIM.Server
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
            }).ParseArguments<GlobalServerOptions>(args)
              .WithParsed<GlobalServerOptions>(Run)
              .WithNotParsed(HandleParseError);
            return exitCode;
        }

        /// <summary>
        /// Start the server with the given options.
        /// </summary>
        /// <param name="options">Options specified by the user (via CLI).</param>
        private static void Run(GlobalServerOptions options)
        {
            try
            {
                var server = new ApsimZMQServer(options);
                server.Run();
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error.ToString());
                exitCode = 1;
            }
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
    }
}
