using APSIM.Shared.Utilities;
using Models.Climate;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Factorial;
using System.Reflection;

namespace Models
{
    /// <summary>
    /// This simple class scraps metadata from a directory structure of .apsimx files. It extracts experiment name, experment design, latitude, longitude
    /// from all simulations found. It writes this information as CSV to standard out.
    /// </summary>
    class Scrapper
    {
        /// <summary>
        /// Cache to store lat/long pairs for weather files.
        /// </summary>
        private static Dictionary<string, (double, double)> latLongCache = new Dictionary<string, (double, double)>();

        /// <summary>
        /// Main program entry point.
        /// </summary>
        /// <param name="args"> Command line arguments</param>
        /// <returns> Program exit code (0 for success)</returns>
        public static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                    throw new Exception("Usage: ApsimXScrapper path > results.csv");

                string rootPath = args[0];

                Console.WriteLine($"FileName,ExperimentName,SimulationName,ExperimentDesign,Latitude,Longitude");

                foreach (string path in Directory.GetFiles(rootPath, "*.apsimx", SearchOption.AllDirectories)
                                                 .Where(p => p.Contains(@"\Examples\") || p.Contains(@"\Prototypes\") || p.Contains(@"\Tests\"))
                                                 .Where(p => !p.Contains(@"\UnitTests\")))
                {
                    ScrapeFile(path, rootPath);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Scrape a .apsimx file, looking for experiments and simulations.
        /// </summary>
        /// <param name="fileName">The name of the .apsimx file</param>
        /// <param name="rootPath">The path to the apsimx directory.</param>
        private static void ScrapeFile(string fileName, string rootPath)
        {
            Simulations? simulations = ModelTreeFactory.CreateFromFile(fileName, (e) => throw e, false).Root.Instance as Simulations;
            if (simulations != null)
            {
                foreach (var experiment in simulations.FindAllDescendants<Experiment>())
                {
                    foreach (var simulationDescription in experiment.GetSimulationDescriptions())
                    {
                        var simulation = simulationDescription.ToSimulation();
                        ScrapeSimulation(simulation, experiment.Name, experiment.GetDesign(), rootPath);
                    }
                }

                foreach (var simulation in  simulations.FindAllDescendants<Simulation>()
                                                       .Where(s => s.Parent is not Experiment))
                {
                    ScrapeSimulation(simulation, string.Empty, string.Empty, rootPath);
                }
            }
        }

        /// <summary>
        /// Scrape a simulation and extract lat/long and write a line to standard out.
        /// </summary>
        /// <param name="simulation">The simulation to examine.</param>
        /// <param name="experimentName">The name of the associated experiment or blank if none.</param>
        /// <param name="experimentDesign">The design of the associated experiment or blank if none.</param>
        /// <param name="rootPath">The path to the apsimx directory.</param>
        private static void ScrapeSimulation(Simulation simulation, string experimentName, string experimentDesign, string rootPath)
        {
            (double, double) latitudeLongitude = GetLatLongFromSimulation(simulation, rootPath);
            string shortFileName = simulation.FileName.Replace(rootPath, string.Empty);
            Console.WriteLine($"{shortFileName}, {experimentName}, {simulation.Name}, {experimentDesign}, {latitudeLongitude.Item1}, {latitudeLongitude.Item2}");
        }

        /// <summary>
        /// Get a lat/long pair for a given simulation.
        /// </summary>
        /// <param name="simulation">The simulation.</param>
        /// <param name="rootPath">The path to the apsimx directory.</param>
        private static (double, double) GetLatLongFromSimulation(Simulation simulation, string rootPath)
        {
            (double, double) latitudeLongitude;
            Weather weather = simulation.FindChild<Weather>();
            if (weather == null)
                latitudeLongitude = (double.NaN, double.NaN);
            else
            {
                weather.FileName = weather.FileName.Replace("%root%", rootPath);
                string cacheKey = Path.GetFileName(weather.FileName);
                if (!latLongCache.TryGetValue(cacheKey, out latitudeLongitude))
                {
                    try
                    {
                        Clock clock = simulation.FindChild<Clock>();
                        CallMethod(clock, "OnSimulationCommencing", new object[] { null, EventArgs.Empty });
                        InjectLink(weather, "clock", clock);
                        CallMethod(weather, "OnSimulationCommencing", new object[] { null, EventArgs.Empty });
                        CallMethod(weather, "OnDoWeather", new object[] { null, EventArgs.Empty });
                        latitudeLongitude = (weather.Latitude, weather.Longitude);
                        latLongCache.Add(cacheKey, latitudeLongitude);
                    }
                    catch
                    {
                    }
                }
            }
            return latitudeLongitude;
        }

        /// <summary>Call an event in a model</summary>
        public static void CallMethod(object instance, string methodName, object[] arguments)
        {
            MethodInfo methodToInvoke = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
            methodToInvoke?.Invoke(instance, arguments);
        }

        /// <summary>Inject a link into a model</summary>
        public static void InjectLink(object model, string linkFieldName, object linkFieldValue)
        {
            ReflectionUtilities.SetValueOfFieldOrProperty(linkFieldName, model, linkFieldValue);
        }
    }
}