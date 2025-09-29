using System;
using System.Collections.Generic;
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
        /// <summary>Ensure the hold function can deal with exceptions.</summary>
        [Test]
        public void TestFunctionOnWheat()
        {
            Simulations simulations = Utilities.GetPlantTestingSimulation(true);

            FrostHeatDamageFunctions fhdf = new FrostHeatDamageFunctions();
            fhdf.CropType = FrostHeatDamageFunctions.CropTypes.Wheat;

            simulations.Node.FindChild<Plant>(recurse: true).AddChild(fhdf);

            Runner runner = new Runner(simulations);
            List<Exception> errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw errors[0];
        }
    }
}
