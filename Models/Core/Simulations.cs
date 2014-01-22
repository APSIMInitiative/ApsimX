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
    public class Simulations : Zone
    {
        private string _FileName;

        /// <summary>
        /// Invoked when all simulations are about to commence.
        /// </summary>
        public event EventHandler AllCommencing;

        /// <summary>
        /// When all simulations have finished, this event will be invoked
        /// </summary>
        public event EventHandler AllCompleted;

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
        /// Create a simulations object by reading the specified filename
        /// </summary>
        public static Simulations Read(string FileName)
        {
            
            // Deserialise
            Simulations simulations = Utility.Xml.Deserialise(FileName) as Simulations;

            // Set the filename
            simulations.FileName = FileName;

            // Resolve links.
            Utility.ModelFunctions.ResolveLinks(simulations);

            // Connect all events.
            Utility.ModelFunctions.ConnectEventsInAllModels(simulations);

            // Call OnLoaded in all models.
            Utility.ModelFunctions.CallOnLoaded(simulations);

            return simulations;
        }

        /// <summary>
        /// Write the specified simulation set to the specified filename
        /// </summary>
        public void Write(string FileName)
        {
            string tempFileName = Path.Combine(Path.GetTempPath(), Path.GetFileName(FileName));
            StreamWriter Out = new StreamWriter(tempFileName);
            Out.Write(Utility.Xml.Serialise(this, true));
            Out.Close();

            // If we get this far without an exception then copy the tempfilename over our filename,
            // creating a backup (.bak) in the process.
            string bakFileName = FileName + ".bak";
            File.Delete(bakFileName);
            if (File.Exists(FileName)) 
                File.Move(FileName, bakFileName);
            File.Move(tempFileName, FileName);
            this.FileName = FileName;
        }

        /// <summary>
        /// Run all simulations. Return true if all ran ok.
        /// </summary>
        public bool Run()
        {
            return Run(FindAllSimulationsToRun());
        }

        /// <summary>
        /// Run the specified simulation. Return true if it ran ok.
        /// </summary>
        public bool Run(Simulation simulation)
        {
            return Run(new Simulation[1] { simulation });
        }

        /// <summary>
        /// Run the specified simulations
        /// </summary>
        private bool Run(Simulation[] simulations)
        {
            // Invoke the AllCommencing event.
            if (AllCommencing != null)
                AllCommencing(this, new EventArgs());

            // Run all simulations
            bool allok = true;
            foreach (Simulation simulation in simulations)
                allok = simulation.Run() && allok;

            // Invoke the AllCompleted event.
            if (AllCompleted != null)
                AllCompleted(this, new EventArgs());

            return allok;
        }

        /// <summary>
        /// Constructor, private to stop developers using it. Use Simulations.Read instead.
        /// </summary>
        private Simulations() { }

        /// <summary>
        /// Find all simulations.
        /// </summary>
        public Simulation[] FindAllSimulationsToRun()
        {
            List<Simulation> simulations = new List<Simulation>();
            // Look for simulations.
            foreach (Model Model in FindAll(typeof(Simulation)))
            {
                // An experiment can have a base simulation - don't return that to caller.
                if (!(Model.Parent is Experiment))
                    simulations.Add(Model as Simulation);
            }

            // Look for experiments and get them to create their simulations.
            foreach (Experiment experiment in FindAll(typeof(Experiment)))
                simulations.AddRange(experiment.Create());

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
            foreach (Model Model in FindAll(typeof(Simulation)))
            {
                // An experiment can have a base simulation - don't return that to caller.
                if (!(Model.Parent is Experiment))
                    simulations.Add(Model.Name);
            }

            // Look for experiments and get them to create their simulations.
            foreach (Experiment experiment in FindAll(typeof(Experiment)))
                simulations.AddRange(experiment.Names());

            return simulations.ToArray();

        }


    }
}
