namespace Models.Core.Run
{
    using Models.Core.ApsimFile;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// This class generates individual .apsimx files for each simulation in a runner.
    /// </summary>
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
        /// <returns>null for success or a list of exceptions.</returns>
        public static List<Exception> Generate(Runner runner, string path, OnProgress progressCallBack)
        {
            List<Exception> errors = null;
            if (runner.TotalNumberOfSimulations > 0)
            {
                Directory.CreateDirectory(path);

                int i = 0;
                foreach (var simulation in runner.Simulations())
                {
                    try
                    {
                        Simulations sims = new Simulations()
                        {
                            Name = "Simulations",
                            Children = new List<Model>()
                            {
                                new Storage.DataStore()
                                {
                                    Name = "DataStore"
                                },
                                simulation
                            }
                        };
                        string st = FileFormat.WriteToString(sims);
                        File.WriteAllText(Path.Combine(path, simulation.Name + ".apsimx"), st);
                    }
                    catch (Exception err)
                    {
                        if (errors == null)
                            errors = new List<Exception>();
                        errors.Add(err);
                    }

                    i++;
                    progressCallBack?.Invoke(100 * i / runner.TotalNumberOfSimulations);
                }
            }
            return errors;
        }
    }
}
