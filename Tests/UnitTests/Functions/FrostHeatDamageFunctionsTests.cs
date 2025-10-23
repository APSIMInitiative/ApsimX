using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using APSIM.Core;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Core.Run;
using Models.Functions;
using Models.PMF;
using NUnit.Framework;

namespace UnitTests.Functions
{
    [TestFixture]
    class FrostHeatDamageFunctionsTests
    {
        /// <summary>Ensure the FrostHeatDamageFunctions runs without exceptions for supported crop types.</summary>
        [Test]
        public void TestFrostHeatFunctionRuns()
        {
            string[] crops = { "Wheat", "Canola", "Barley" };
            FrostHeatDamageFunctions.CropTypes[] types = { FrostHeatDamageFunctions.CropTypes.Wheat, FrostHeatDamageFunctions.CropTypes.Canola, FrostHeatDamageFunctions.CropTypes.Wheat };

            for(int i = 0; i < crops.Length; i++)
            {
                string path = Path.Combine("%root%", "Examples", $"{crops[i]}.apsimx");
                path = PathUtilities.GetAbsolutePath(path, null);
                Simulations simulations = FileFormat.ReadFromFile<Simulations>(path).Model as Simulations;

                FrostHeatDamageFunctions fhdf = new FrostHeatDamageFunctions();
                fhdf.CropType = types[i];

                simulations.Node.FindChild<Plant>(recurse: true).AddChild(fhdf);

                List<string> reportVariables = simulations.Node.FindChild<Models.Report>("Report", recurse: true).VariableNames.ToList();
                reportVariables.Add("[FrostHeatDamageFunctions].FrostHeatYield");
                reportVariables.Add("[FrostHeatDamageFunctions].FrostEventNumber");
                reportVariables.Add("[FrostHeatDamageFunctions].HeatEventNumber");
                simulations.Node.FindChild<Models.Report>("Report", recurse: true).VariableNames = reportVariables.ToArray();

                simulations.Node = Node.Create(simulations);

                Runner runner = new Runner(simulations);
                List<Exception> errors = runner.Run();
                if (errors != null && errors.Count > 0)
                    throw errors[0];
            }
        }
    }
}
