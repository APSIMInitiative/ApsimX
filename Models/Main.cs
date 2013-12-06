using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Models.Core;

namespace Models
{
    public class Program
    {
        /// <summary>
        /// Main program entry point.
        /// </summary>
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: ApsimX .ApsimFileName");
                return 1;
            }

            try
            {
                Simulations simulations = Simulations.Read(args[0]);
                if (simulations == null)
                    throw new Exception("No simulations found in file: " + args[0]);

                DataStore store = simulations.Get("DataStore") as DataStore;
                if (store == null)
                    throw new Exception("Cannot find DataStore in file: " + args[0]);

                store.MessageWritten += OnMessageWritten;
                simulations.Initialise();
                store.MessageWritten -= OnMessageWritten;
                if (simulations.Run())
                    return 0;
                else
                    return 1;

            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                return 1;
            }
        }

        /// <summary>
        /// Simulation has written something - echo to stdout.
        /// </summary>
        static void OnMessageWritten(object sender, DataStore.MessageArg e)
        {
            string messageType = "";
            if (e.ErrorLevel == DataStore.ErrorLevel.Error)
                messageType = "ERROR: ";
            else if (e.ErrorLevel == DataStore.ErrorLevel.Warning)
                messageType = "WARNING:";

            Console.WriteLine("{0} {1} {2} {3}", new object[] {messageType,
                                                               e.SimulationDateTime,
                                                               e.SimulationName,
                                                               e.Message});
        }
    }
}