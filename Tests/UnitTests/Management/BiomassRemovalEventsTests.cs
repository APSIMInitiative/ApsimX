using APSIM.Shared.Utilities;
using Models;
using Models.Climate;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Run;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace UnitTests.Core
{
    /// <summary>This is a test class for the BiomassRemovalEvents class</summary>
    [TestFixture]
    public class BiomassRemovalEventsTests
    {
        /// <summary>
        /// Runs a simulation with a script that cuts biomass, the new BiomassRemovalEvents to cut biomass and not cutting biomass.
        /// The script and event should have the same values, and they should differ from the sim that doesn't remove biomass.
        /// </summary>
        [Test]
        public void RunSimulationWithBiomassRemoval()
        {
            //read in our base test that we'll use for this
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Management.BiomassRemovalEventsTests.apsimx");
            string weatherData = ReflectionUtilities.GetResourceAsString("UnitTests.Management.CustomMetData.met");
            string metFile = Path.GetTempFileName();
            File.WriteAllText(metFile, weatherData);

            Simulations sims = FileFormat.ReadFromString<Simulations>(json, e => throw e, false).NewModel as Simulations;
            Models.Climate.Weather weather = sims.FindDescendant<Models.Climate.Weather>();
            weather.FullFileName = metFile;

            Runner runner = new Runner(sims);
            List<Exception> errors = runner.Run();

            // Check that no errors were thrown
            Assert.AreEqual(0, errors.Count);

            DataStore dataStore = sims.FindChild<DataStore>();

            //check that the datastore and expected simulations have the same amount of entries
            Assert.AreEqual(dataStore.Reader.SimulationNames.Count, 3);
            System.Data.DataTable dt = dataStore.Reader.GetData("Report");

            System.Data.DataRow simScript = dt.Rows[0];
            System.Data.DataRow simEvents = dt.Rows[1];
            System.Data.DataRow simNone = dt.Rows[2];

            Assert.AreEqual(simScript[7], simEvents[7]);
            Assert.AreEqual(simScript[8], simEvents[8]);
            Assert.AreEqual(simScript[9], simEvents[9]);

            Assert.AreNotEqual(simNone[7], simScript[7]);
            Assert.AreNotEqual(simNone[8], simScript[8]);
            Assert.AreNotEqual(simNone[9], simScript[9]);

            Assert.AreNotEqual(simNone[7], simEvents[7]);
            Assert.AreNotEqual(simNone[8], simEvents[8]);
            Assert.AreNotEqual(simNone[9], simEvents[9]);
        }
    }
}