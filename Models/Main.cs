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
    }
}