using System.IO;
using System.Xml;
using Models.Core;
using System.Xml.Serialization;
using System;
using System.Reflection;
using System.Collections.Generic;
using Models.Factorial;

namespace Models.Core
{
    /// <summary>
    /// Encapsulates a collection of simulations. It is responsible for creating this collection,
    /// changing the structure of the components within the simulations, renaming components, adding
    /// new ones, deleting components. The user interface talks to an instance of this class.
    /// </summary>
    [Serializable]
    public class Simulations : Model, Utility.JobManager.IRunnable
    {
        private string _FileName;
        public Int32 ExplorerWidth { get; set; }



        /// <summary>
        /// The name of the file containing the simulations.
        /// </summary>
        [XmlIgnore]
        public string FileName
        {
            get
            {
                return _FileName;
            }
            set
            {
                _FileName = value;
            }
        }

        /// <summary>
        /// A list of all exceptions thrown during the creation and loading of the simulation.
        /// </summary>
        [XmlIgnore]
        public List<Exception> LoadErrors { get; private set; }

        /// <summary>
        /// Create a simulations object by reading the specified filename
        /// </summary>
        public static Simulations Read(string FileName)
        {
            
            // Deserialise
            Simulations simulations = Utility.Xml.Deserialise(FileName) as Simulations;

            if (simulations != null)
            {
                // Set the filename
                simulations.FileName = FileName;
                simulations.SetFileNameInAllSimulations();

                // Call the OnDeserialised method in each model.
                object[] args = new object[] { true };
                foreach (Model model in simulations.Children.AllRecursively)
                    Apsim.CallEventHandler(model, "Deserialised", args);

                // Parent all models.
                simulations.Parent = null;
                ModelFunctions.ParentAllChildren(simulations);

                // Call OnLoaded in all models.
                simulations.LoadErrors = new List<Exception>();
                foreach (Model child in simulations.Children.AllRecursively)
                {
                    try
                    {
                        child.OnLoaded();
                    }
                    catch (ApsimXException err)
                    {
                        simulations.LoadErrors.Add(err);
                    }
                    catch (Exception err)
                    {
                        simulations.LoadErrors.Add(err);
                    }
                }
            }
            else
                throw new Exception("Simulations.Read() failed. Invalid simulation file.\n");
            return simulations;
        }

        /// <summary>
        /// Create a simulations object by reading the specified filename
        /// </summary>
        public static Simulations Read(XmlNode node)
        {

            // Deserialise
            Simulations simulations = Utility.Xml.Deserialise(node) as Simulations;

            if (simulations != null)
            {
                // Set the filename
                simulations.SetFileNameInAllSimulations();

                // Call the OnSerialised method in each model.
                object[] args = new object[] { true };
                foreach (Model model in simulations.Children.AllRecursively)
                    Apsim.CallEventHandler(model, "Deserialised", args);

                // Parent all models.
                simulations.Parent = null;
                ModelFunctions.ParentAllChildren(simulations);

                // Call OnLoaded in all models.
                foreach (Model child in simulations.Children.AllRecursively)
                    child.OnLoaded();
            }
            else
                throw new Exception("Simulations.Read() failed. Invalid simulation file.\n");
            return simulations;
        }

        /// <summary>
        /// Write the specified simulation set to the specified filename
        /// </summary>
        public void Write(string FileName)
        {
            string tempFileName = Path.Combine(Path.GetTempPath(), Path.GetFileName(FileName));
            StreamWriter Out = new StreamWriter(tempFileName);
            Write(Out);
            Out.Close();

            // If we get this far without an exception then copy the tempfilename over our filename,
            // creating a backup (.bak) in the process.
            string bakFileName = FileName + ".bak";
            File.Delete(bakFileName);
            if (File.Exists(FileName))
                File.Move(FileName, bakFileName);
            File.Move(tempFileName, FileName);
            this.FileName = FileName;
            SetFileNameInAllSimulations();
        }

        /// <summary>
        /// Write the specified simulation set to the specified 'stream'
        /// </summary>
        public void Write(TextWriter stream)
        {
            object[] args = new object[] { true };
            foreach (Model model in Children.AllRecursively)
                Apsim.CallEventHandler(model, "Serialising", args);

            try
            {
                stream.Write(Utility.Xml.Serialise(this, true));
            }
            finally
            {
                foreach (Model model in Children.AllRecursively)
                    Apsim.CallEventHandler(model, "Serialised", args);

            }
        }

        /// <summary>
        /// Constructor, private to stop developers using it. Use Simulations.Read instead.
        /// </summary>
        private Simulations() { }

        /// <summary>
        /// Find all simulations under the specified parent model.
        /// </summary>
        public static Simulation[] FindAllSimulationsToRun(Model parent)
        {
            List<Simulation> simulations = new List<Simulation>();

            if (parent is Experiment)
                simulations.AddRange((parent as Experiment).Create());
            else if (parent is Simulation)
                simulations.Add(parent as Simulation);
            else
            {
                // Look for simulations.
                foreach (Model model in parent.Children.AllRecursively)
                {
                    if (model is Experiment)
                        simulations.AddRange((model as Experiment).Create());
                    else if (model is Simulation && !(model.Parent is Experiment))
                        simulations.Add(model as Simulation);
                }
            }
            // Make sure each simulation has it's filename set correctly.
            foreach (Simulation simulation in simulations)
            {
                if (simulation.FileName == null)
                    simulation.FileName = RootSimulations(parent).FileName;
            }
            return simulations.ToArray();
        }

        /// <summary>
        /// Find all simulation names that are going to be run.
        /// </summary>
        /// <returns></returns>
        public string[] FindAllSimulationNames()
        {
            List<string> simulations = new List<string>();
            // Look for simulations.
            foreach (Model Model in Children.AllRecursively)
            {
                if (Model is Simulation)
                {
                    // An experiment can have a base simulation - don't return that to caller.
                    if (!(Model.Parent is Experiment))
                        simulations.Add(Model.Name);
                }
            }

            // Look for experiments and get them to create their simulations.
            foreach (Model experiment in Children.AllRecursively)
            {
                if (experiment is Experiment)
                    simulations.AddRange((experiment as Experiment).Names());
            }

            return simulations.ToArray();

        }

        /// <summary>
        /// Look through all models. For each simulation found set the filename.
        /// </summary>
        private void SetFileNameInAllSimulations()
        {
            foreach (Model simulation in Children.AllRecursively)
                if (simulation is Simulation)
                    (simulation as Simulation).FileName = FileName;
        }

        private static Simulations RootSimulations(Model model)
        {
            Model m = model;
            while (m != null && m.Parent != null && !(m is Simulations))
                m = m.Parent as Model;

            return m as Simulations;
        }

        /// <summary>
        /// Allows the GUI to specify a simulation to run. It then calls
        /// 'Run' below to run this simulation.
        /// </summary>
        [XmlIgnore]
        public Model SimulationToRun { get; set; }

        private int NumToRun;
        private int NumCompleted;

        /// <summary>
        /// Run all simulations.
        /// </summary>
        public void Run(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            // Get a reference to the JobManager so that we can add jobs to it.
            Utility.JobManager jobManager = e.Argument as Utility.JobManager;

            // Get a reference to our child DataStore.
            DataStore store = Children.Matching(typeof(DataStore)) as DataStore;

            // Remove old simulation data.
            store.RemoveUnwantedSimulations(this);

            Simulation[] simulationsToRun;
            if (SimulationToRun == null)
            {
                // As we are going to run all simulations, we can delete all tables in the DataStore. This
                // will clean up order of columns in the tables and removed unused ones.
                store.DeleteAllTables();

                store.Disconnect();

                simulationsToRun = Simulations.FindAllSimulationsToRun(this);
            }
            else
                simulationsToRun = Simulations.FindAllSimulationsToRun(SimulationToRun);

            NumToRun = simulationsToRun.Length;
            NumCompleted = 0;

            if (NumToRun == 1)
            {
                // Skip running in another thread.
                simulationsToRun[0].OnCompleted -= OnSimulationCompleted;
                simulationsToRun[0].OnCompleted += OnSimulationCompleted;
                simulationsToRun[0].Run(null, null);
            }
            else
            {
                foreach (Simulation simulation in simulationsToRun)
                {
                    simulation.OnCompleted -= OnSimulationCompleted;
                    simulation.OnCompleted += OnSimulationCompleted;
                    jobManager.AddJob(simulation);
                }
            }
        }

        /// <summary>
        /// This gets called everytime a simulation completes. When all are done then
        /// invoke each model's OnAllCompleted method.
        /// </summary>
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            bool RunAllCompleted = false;
            lock (this)
            {
                NumCompleted++;
                RunAllCompleted = NumCompleted == NumToRun;
            }
            if (RunAllCompleted)
            {
                Console.WriteLine(FileName);
                foreach (Model model in Children.AllRecursively)
                    model.OnAllSimulationsCompleted();
            }
        }

        /// <summary>
        /// Ensure the specified filename is always a full path by converting relative
        /// paths to absolute.
        /// </summary>
        /// <param name="fileName">The filename to convert to absolute</param>
        /// <returns>The full path</returns>
        public string GetFullFileName(string fileName)
        {
            if (fileName != null && fileName != string.Empty)
            {
                return Path.Combine(Path.GetDirectoryName(this.FileName), fileName);
            }

            return null;
        }
    }
}
