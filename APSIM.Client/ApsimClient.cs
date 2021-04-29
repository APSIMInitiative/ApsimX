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
                IEnumerable<CompositeFactor> changes;
                while ( (changes = GetChanges())?.Any() ?? false)
                {
                    if (!pipeClient.IsConnected)
                        break;
                    for (int i = 0; i < 5; i++)
                    {
                        double ftn = (double)changes.First().Values.First();
                        ICommand command = new RunCommand(changes);
                        var timer = Stopwatch.StartNew();
                        PipeUtilities.SendObjectToPipe(pipeClient, command);
                        object response = PipeUtilities.GetObjectFromPipe(pipeClient);
                        timer.Stop();
                        if (response is Exception error)
                            throw new Exception($"Error running simulation with TT of {ftn}", error);
                        else
                        {
                            if (response is string msg)
                            {
                                if (msg == "fin")
                                    Console.WriteLine($"Ran simulation with TT of {ftn} in {timer.ElapsedMilliseconds}ms");
                                else
                                {
                                    Console.WriteLine($"Unexpected response from server: '{response}'");
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<CompositeFactor> GetChanges()
        {
            CompositeFactor factor = ChangeJuvTarget();
            if (factor == null)
                return null;
            return new[] { factor };
        }

        private CompositeFactor ChangeJuvTarget()
        {
            Console.Write("Enter Juvenile TT Target (default is 100): ");
            string input = Console.ReadLine();
            if (double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out double tt))
                return new CompositeFactor("JuvTT", "[Sorghum].Phenology.Juvenile.Target.FixedValue", tt);
            else if (string.IsNullOrWhiteSpace(input))
                return null;
            else
                throw new FormatException($"Unable to parse {input}: input is not a number");
        }

        private CompositeFactor ChangeFtn()
        {
            Console.Write("Enter FTN: ");
            string input = Console.ReadLine();
            if (double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out double ftn))
                return new CompositeFactor("ftn", "[SowingRule].Script.Ftn", ftn);
            else if (string.IsNullOrWhiteSpace(input))
                return null;
            else
                throw new FormatException($"Unable to parse {input}: input is not a number");
        }
    }
}
