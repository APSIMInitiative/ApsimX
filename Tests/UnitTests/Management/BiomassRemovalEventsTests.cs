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
            Assert.That(errors.Count, Is.EqualTo(0));

            DataStore dataStore = sims.FindChild<DataStore>();

            //check that the datastore and expected simulations have the same amount of entries
            Assert.That(dataStore.Reader.SimulationNames.Count, Is.EqualTo(3));
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

            Assert.That(simEvents["AboveGroundWt"], Is.EqualTo(simScript["AboveGroundWt"]));
            Assert.That(simEvents["AboveGroundN"], Is.EqualTo(simScript["AboveGroundN"]));
            Assert.That(simEvents["TotalWt"], Is.EqualTo(simScript["TotalWt"]));

            Assert.That(simScript["AboveGroundWt"], Is.Not.EqualTo(simNone["AboveGroundWt"]));
            Assert.That(simScript["AboveGroundN"], Is.Not.EqualTo(simNone["AboveGroundN"]));
            Assert.That(simScript["TotalWt"], Is.Not.EqualTo(simNone["TotalWt"]));

            Assert.That(simEvents["AboveGroundWt"], Is.Not.EqualTo(simNone["AboveGroundWt"]));
            Assert.That(simEvents["AboveGroundN"], Is.Not.EqualTo(simNone["AboveGroundN"]));
            Assert.That(simEvents["TotalWt"], Is.Not.EqualTo(simNone["TotalWt"]));
        }
    }
}