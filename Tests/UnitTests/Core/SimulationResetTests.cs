using System;
using System.IO;
using APSIM.Core;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Soils;
using Models.Storage;
using NUnit.Framework;

namespace UnitTests.Core
{
    /// <summary>
    /// Tests to ensure that a simulation's state after a simulation
    /// is identical to its state before a simulation is run.
    /// </summary>
    [TestFixture]
    public class SimulationResetTests
    {
        private class Logger : Model
        {
            [Link] private IClock clock = null;
            [Link(Type = LinkType.Ancestor)] private Simulation sim = null;
            public string Json { get; set; }
            public bool ExitAfterLogging { get; set; }

            [EventSubscribe("EndOfDay")]
            private void Log(object sender, EventArgs args)
            {
                if (clock.Today == clock.StartDate)
                {
                    // Always set ExitAfterLogging and json to null before loggin, to ensure
                    // they don't get in the way.
                    bool exitEarly = ExitAfterLogging;
                    ExitAfterLogging = false;
                    Json = null;
                    Json = ReflectionUtilities.JsonSerialise(sim, true);
                    ExitAfterLogging = exitEarly;
                    if (ExitAfterLogging)
                        clock.EndDate = clock.Today;
                }
            }
        }

        [Parallelizable]
        [TestCase("AgPasture.apsimx")]
        [TestCase("Barley.apsimx")]
        [TestCase("Chicory.apsimx")]
        [TestCase("Eucalyptus.apsimx")]
        [TestCase("FodderBeet.apsimx")]
        [TestCase("Maize.apsimx")]
        [TestCase("Oats.apsimx")]
        [TestCase("OilPalm.apsimx")]
        [TestCase("PlantainForage.apsimx")]
        [TestCase("Potato.apsimx")]
        [TestCase("RedClover.apsimx")]
        [TestCase("Rotation.apsimx")]
        [TestCase("SCRUM.apsimx")]
        [TestCase("SimpleGrazing.apsimx")]
        [TestCase("Soybean.apsimx")]
        [TestCase("Stock.apsimx")]
        [TestCase("Sugarcane.apsimx")]
        [TestCase("Wheat.apsimx")]
        [TestCase("WhiteClover.apsimx")]
        public void TestSimulation(string fileName)
        {
            Simulation sim = CreateSimulation(Path.Combine("%root%", "Examples", fileName));
            Logger logger = new Logger();
            sim.Node.AddChild(logger);
            sim.Prepare();

            // Run full simulation.
            sim.Run();

            // Get JSON of total state at end of run (including privates)
            string pre = logger.Json;

            // Run a second time, but only run the first day, then skip to end
            logger.ExitAfterLogging = true;
            sim.Run();

            //Get state of sim as JSON again, this should match
            string post = logger.Json;

            // Easiest way to debug this test is to uncomment these four lines
            // and open the two json files in a diff tool.
            /*
            string path = Path.GetTempPath();
            string name = Guid.NewGuid().ToString();
            File.WriteAllText(Path.Combine(path, $"{name}-pre.json"), pre);
            File.WriteAllText(Path.Combine(path, $"{name}-post.json"), post);
            */

            Assert.That(post, Is.EqualTo(pre), $"{Path.GetFileName(sim.FileName)} simulation failed to zero all variables");
        }

        private static Simulation CreateSimulation(string path)
        {
            path = PathUtilities.GetAbsolutePath(path, null);
            Simulations sims = FileFormat.ReadFromFile<Simulations>(path).Model as Simulations;
            foreach (Soil soil in sims.Node.FindChildren<Soil>(recurse: true))
                soil.Sanitise();
            DataStore storage = sims.Node.FindChild<DataStore>(recurse: true);
            storage.UseInMemoryDB = true;
            IClock clock = sims.Node.FindChild<Clock>(recurse: true);
            clock.EndDate = clock.StartDate.AddYears(1);
            return sims.Node.FindChild<Simulation>(recurse: true);
        }
    }
}
