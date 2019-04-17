using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.ApsimFile;
using NUnit.Framework;

namespace UnitTests.Core
{
    public class PostSimulationToolTests
    {
        /// <summary>
        /// Ensures that an exception thrown in one tool does not prevent
        /// another tool from running. Reproduces github bug #3751.
        /// https://github.com/APSIMInitiative/ApsimX/issues/3751
        /// </summary>
        [Test]
        public void EnsureAllToolsRun()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.PostSimulationTool.apsimx");
            List<Exception> errors;
            Simulations sims = FileFormat.ReadFromString<Simulations>(json, out errors);
            Assert.AreEqual(0, errors.Count);

            IModel script = Apsim.Child(Apsim.Find(sims, "Tool2"), "Script");
            Assert.NotNull(script);

            Simulation sim = Apsim.Find(sims, typeof(Simulation)) as Simulation;
            Assert.NotNull(sim);

            bool hasBeenRun = (bool)ReflectionUtilities.GetValueOfFieldOrProperty("HasBeenRun", script);
            Assert.False(hasBeenRun);

            sims.Run(sim, doClone: false);

            hasBeenRun = (bool)ReflectionUtilities.GetValueOfFieldOrProperty("HasBeenRun", script);
            Assert.True(hasBeenRun, "Failure in a post simulation tool prevented another post simulation tool from running.");
        }
    }
}
