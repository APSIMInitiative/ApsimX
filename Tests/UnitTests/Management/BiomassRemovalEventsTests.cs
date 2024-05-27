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

            foreach (var ex in errors)
                Console.WriteLine(ex.ToString());

            // Check that no errors were thrown
            Assert.AreEqual(0, errors.Count);

            DataStore dataStore = sims.FindChild<DataStore>();

            //check that the datastore and expected simulations have the same amount of entries
            Assert.AreEqual(dataStore.Reader.SimulationNames.Count, 3);
            System.Data.DataTable dt = dataStore.Reader.GetData("Report");


            System.Data.DataRow simScript = null;
            System.Data.DataRow simEvents = null;
            System.Data.DataRow simNone = null;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if ((dt.Rows[i]["SimulationName"] as string).CompareTo("ExperimentScriptfalse") == 0)
                    simScript = dt.Rows[i];
                else if ((dt.Rows[i]["SimulationName"] as string).CompareTo("ExperimentEventsfalse") == 0)
                    simEvents = dt.Rows[i];
                else if ((dt.Rows[i]["SimulationName"] as string).CompareTo("ExperimentNone") == 0)
                    simNone = dt.Rows[i];
                else
                    Assert.Fail();
            }

            Assert.AreEqual(simScript["AboveGroundWt"], simEvents["AboveGroundWt"]);
            Assert.AreEqual(simScript["AboveGroundN"], simEvents["AboveGroundN"]);
            Assert.AreEqual(simScript["TotalWt"], simEvents["TotalWt"]);

            Assert.AreNotEqual(simNone["AboveGroundWt"], simScript["AboveGroundWt"]);
            Assert.AreNotEqual(simNone["AboveGroundN"], simScript["AboveGroundN"]);
            Assert.AreNotEqual(simNone["TotalWt"], simScript["TotalWt"]);

            Assert.AreNotEqual(simNone["AboveGroundWt"], simEvents["AboveGroundWt"]);
            Assert.AreNotEqual(simNone["AboveGroundN"], simEvents["AboveGroundN"]);
            Assert.AreNotEqual(simNone["TotalWt"], simEvents["TotalWt"]);
        }
    }
}