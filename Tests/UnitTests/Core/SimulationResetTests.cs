using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.PMF;
using Models.Soils;
using Models.Soils.Standardiser;
using Models.Storage;
using NUnit.Framework;
using UnitTests.Storage;

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
            [Link] private Clock clock = null;
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

        [Test]
        public void EnsureVariablesAreZeroed()
        {
            /*
            object mutex = new object();
            // 1. Create simulation.
            Parallel.ForEach<Simulation>(GetSimsToTest(), sim =>
            {
                Logger logger = new Logger();
                logger.Parent = sim;
                sim.Children.Add(logger);
                sim.Prepare();

                // 2. Run simulation.
                sim.Run();

                // 3. Compare simulation state now vs before running
                string pre = logger.Json;

                // Run a second time.
                logger.ExitAfterLogging = true;
                sim.Run();

                string post = logger.Json;
                lock (mutex)
                    Assert.AreEqual(pre, post, $"{Path.GetFileName(sim.FileName)} simulation failed to zero all variables");
            });
            */
            foreach (Simulation sim in GetSimsToTest())
            {
                Logger logger = new Logger();
                logger.Parent = sim;
                sim.Children.Add(logger);
                sim.Prepare();

                // 2. Run simulation.
                sim.Run();

                // 3. Compare simulation state now vs before running
                string pre = logger.Json;

                // Run a second time.
                logger.ExitAfterLogging = true;
                sim.Run();

                string post = logger.Json;

                // Easiest way to debug this test is to uncomment these two lines
                // and open the two json files in a diff tool.
                File.WriteAllText("/home/drew/code/ApsimX/pre.json", pre);
                File.WriteAllText("/home/drew/code/ApsimX/post.json", post);

                Assert.AreEqual(pre, post, $"{Path.GetFileName(sim.FileName)} simulation failed to zero all variables");
            }
        }

        private IEnumerable<Simulation> GetSimsToTest()
        {
            // Lazy loading of simulations. The .apsimx files won't be read in
            // until we're ready to run them. Therefore if we fail in one of
            // the earlier files, the latter ones won't be read.
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "Wheat.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "Maize.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "Eucalyptus.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "AgPasture.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "Barley.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "Chicory.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "FodderBeet.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "Oats.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "OilPalm.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "Plantain.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "Potato.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "RedClover.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "Rotation.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "SCRUM.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "SimpleGrazing.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "Soybean.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "Stock.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "Sugarcane.apsimx"));
            yield return CreateSimulation(Path.Combine("%root%", "Examples", "WhiteClover.apsimx"));
        }

        private Simulation CreateSimulation(string path)
        {
            path = PathUtilities.GetAbsolutePath(path, null);
            Simulations sims = FileFormat.ReadFromFile<Simulations>(path, out List<Exception> errors);
            SoilStandardiser.Standardise(sims.FindDescendant<Soil>());
            DataStore storage = sims.FindDescendant<DataStore>();
            storage.Close();
            storage.FileName = ":memory:";
            storage.Open();
            Clock clock = sims.FindDescendant<Clock>();
            clock.EndDate = clock.StartDate.AddYears(1);
            return sims.FindDescendant<Simulation>();
        }
    }
}
