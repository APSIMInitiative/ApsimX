using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using APSIM.Core;
using APSIM.Shared.Extensions.Collections;
using Models.Core.ApsimFile;
using Models.Storage;

namespace Models.Core.Run
{

    /// <summary>
    /// This class generates individual .apsimx files for each simulation in a runner.
    /// </summary>
    /// <remarks>
    /// Should this class implement IJobManager so that its interaction with the
    /// job runner is more explicit? It seems very odd to have to pass a Runner
    /// instance into the Generate method...
    /// </remarks>
    public class GenerateApsimXFiles
    {
        /// <summary>A delegate that gets called to indicate progress during an operation.</summary>
        /// <param name="progress">Progress (0 - 1).</param>
        public delegate void OnProgress(double progress);

        /// <summary>
        /// Split an .apsimx file into smaller files, with each generated file
        /// containing the specified number of simulations.
        /// </summary>
        /// <param name="file">A runner containing a set of simulations.</param>
        /// <param name="simsPerFile">Number of simulations in each generated file.</param>
        /// <param name="path">Path to which the files will be saved.</param>
        /// <param name="progressCallBack">Invoked when the method needs to indicate progress.</param>
        /// <param name="collectExternalFiles">Collect all external files and store on path?</param>
        /// <returns>null for success or a list of exceptions.</returns>
        public static IEnumerable<string> SplitFile(string file, uint simsPerFile, string path, OnProgress progressCallBack, bool collectExternalFiles = false)
        {
            IModel model = NodeTree.CreateFromFile<Simulations>(file, e => throw e, false).Root.Model as IModel;
            Runner runner = new Runner(file);
            return Generate(runner, simsPerFile, path, progressCallBack, collectExternalFiles);
        }

        /// <summary>
        /// Generates .apsimx files for each simulation in a runner.
        /// Returns the names of the generated files.
        /// </summary>
        /// <param name="runner">A runner containing a set of simulations.</param>
        /// <param name="simsPerFile">Number of simulations in each generated file.</param>
        /// <param name="path">Path which the files will be saved to.</param>
        /// <param name="progressCallBack">Invoked when the method needs to indicate progress.</param>
        /// <param name="collectExternalFiles">Collect all external files and store on path?</param>
        /// <returns>Names of the generated files.</returns>
        public static IEnumerable<string> Generate(Runner runner, uint simsPerFile, string path, OnProgress progressCallBack, bool collectExternalFiles = false)
        {
            if (simsPerFile > int.MaxValue)
                // This would be over 2 billion sims. There would be other, more fundamental
                // problems well before this point is reached. Like max capacity of a list.
                throw new InvalidOperationException("too many simulations");

            Directory.CreateDirectory(path);

            int i = 0;
            Queue<Simulation> simulations = new Queue<Simulation>(runner.Simulations());
            List<Simulation> simsInCurrentFile = new List<Simulation>();
            int numSims = simulations.Count;
            int numFiles = simulations.Count / (int)simsPerFile + 1;
            List<string> generatedFiles = new List<string>(numFiles);
            while (simulations.Any())
            {
                Simulations sims = new Simulations()
                {
                    Name = "Simulations",
                    Children = new List<IModel>()
                    {
                        new DataStore()
                    }
                };
                foreach (Simulation sim in simulations.DequeueChunk(simsPerFile))
                {
                    FixSimulation(sim, path, collectExternalFiles);
                    sims.Children.Add(sim);
                }
                //var tree = NodeTree.Create(sims);
                //string st = tree.Root.ToJSONString();
                string st = FileFormat.WriteToString(sims);

                string fileName = Path.Combine(path, $"generated-{i}.apsimx");
                generatedFiles.Add(fileName);
                File.WriteAllText(fileName, st);

                progressCallBack?.Invoke(1.0 * (i + 1) / numSims);
                i++;
            }
            return generatedFiles;
        }

        private static void FixSimulation(Simulation simulation, string outputPath, bool collectExternalFiles)
        {
            // If any of the property replacements (ie from a factor) modify manager
            // script parameters, we need to tell the manager to update its parameter
            // list before serializing the models. This normally doesn't matter because
            // the changes are applied to the script object itself, rather than the
            // dictionary; however in this instance, we care about what's going to be
            // serialized, which is the contents of the dict.
            foreach (Manager manager in simulation.FindAllDescendants<Manager>())
                manager.GetParametersFromScriptModel();

            if (collectExternalFiles)
            {
                // Find all models that reference external files. For each model, copy all the referenced
                // files onto our path and then tell the model to remove the paths. The result will be
                // a self contained path that has all files needed to run all simulations. Useful
                // for running on clusters.
                foreach (IReferenceExternalFiles child in simulation.FindAllDescendants<IReferenceExternalFiles>())
                {
                    foreach (var fileName in child.GetReferencedFileNames())
                    {
                        string destFileName = Path.Combine(outputPath, Path.GetFileName(fileName));
                        if (!File.Exists(destFileName))
                            File.Copy(fileName, destFileName);
                    }
                    child.RemovePathsFromReferencedFileNames();
                }
            }
        }
    }
}
