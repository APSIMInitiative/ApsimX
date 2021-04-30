using System;
using System.IO.Pipes;
using System.Security.Principal;
using APSIM.Shared.Utilities;
using APSIM.Server.Commands;
using System.Collections.Generic;
using Models.Factorial;
using System.Linq;
using System.Globalization;
using System.Diagnostics;
using Models.Core.Run;

namespace APSIM.Client
{
    public class ApsimClient
    {
        public void Run()
        {
            using (var pipeClient = new NamedPipeClientStream(".", "testpipe", PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation))
            {
                Console.Write("Connecting to server...");
                pipeClient.Connect();
                Console.WriteLine("done.");
                IEnumerable<IReplacement> changes;
                while ( (changes = GetChanges())?.Any() ?? false)
                {
                    if (!pipeClient.IsConnected)
                        break;
                    for (int i = 0; i < 5; i++)
                    {
                        ICommand command = new RunCommand(changes);
                        var timer = Stopwatch.StartNew();
                        PipeUtilities.SendObjectToPipe(pipeClient, command);
                        object response = PipeUtilities.GetObjectFromPipe(pipeClient);
                        timer.Stop();
                        if (response is Exception error)
                            throw new Exception($"Error running simulation", error);
                        else
                        {
                            if (response is string msg)
                                Console.WriteLine($"Ran simulation in {timer.ElapsedMilliseconds}ms. Server response: {msg}");
                            else
                            {
                                Console.WriteLine($"Unexpected response from server:");
                                Console.WriteLine(response);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<IReplacement> GetChanges()
        {
            IReplacement factor = ChangeJuvTarget();
            if (factor == null)
                return null;
            return new[] { factor };
        }

        private IReplacement ChangeJuvTarget()
        {
            Console.Write("Enter Juvenile TT Target (default is 100): ");
            string input = Console.ReadLine();
            if (double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out double tt))
                return new PropertyReplacement("[Sorghum].Phenology.Juvenile.Target.FixedValue", tt);
            else if (string.IsNullOrWhiteSpace(input))
                return null;
            else
                throw new FormatException($"Unable to parse {input}: input is not a number");
        }

        private IReplacement ChangeFtn()
        {
            Console.Write("Enter FTN: ");
            string input = Console.ReadLine();
            if (double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out double ftn))
                return new PropertyReplacement("[SowingRule].Script.Ftn", ftn);
            else if (string.IsNullOrWhiteSpace(input))
                return null;
            else
                throw new FormatException($"Unable to parse {input}: input is not a number");
        }
    }
}
