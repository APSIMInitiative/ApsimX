namespace Models.Core.Run
{
    using Models.Core.ApsimFile;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

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
        /// <param name="percent">Percentage compete.</param>
        public delegate void OnProgress(int percent);

        /// <summary>
        /// Generates .apsimx files for each simulation in a runner.
        /// Returns any exceptions thrown.
        /// </summary>
        /// <param name="runner">A runner containing a set of simulations.</param>
        /// <param name="path">Path which the files will be saved to.</param>
        /// <param name="progressCallBack">Invoked when the method needs to indicate progress.</param>
        /// <param name="collectExternalFiles">Collect all external files and store on path?</param>
        /// <returns>null for success or a list of exceptions.</returns>
        public static List<Exception> Generate(Runner runner, string path, OnProgress progressCallBack, bool collectExternalFiles = false)
        {
            List<Exception> errors = null;
            Directory.CreateDirectory(path);

            int i = 0;
            List<Simulation> simulations = runner.Simulations().ToList();
            foreach (var simulation in simulations)
            {
                try
                {
                    Simulations sims = new Simulations()
                    {
                        Name = "Simulations",
                        Children = new List<IModel>()
                        {
                            new Storage.DataStore()
                            {
                                Name = "DataStore"
                            },
                            simulation
                        }
                    };

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
                                string destFileName = Path.Combine(path, Path.GetFileName(fileName));
                                if (!File.Exists(destFileName))
                                    File.Copy(fileName, destFileName);
                            }
                            child.RemovePathsFromReferencedFileNames();
                        }
                    }

                    string st = FileFormat.WriteToString(sims);
                    File.WriteAllText(Path.Combine(path, simulation.Name + ".apsimx"), st);
                }
                catch (Exception err)
                {
                    if (errors == null)
                        errors = new List<Exception>();
                    errors.Add(err);
                }

                progressCallBack?.Invoke(Convert.ToInt32(100 * (i + 1) / simulations.Count));
                i++;
            }
            return errors;
        }
    }
}
