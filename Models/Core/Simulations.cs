using System.IO;
using System.Xml;
using Models.Core;
using System.Xml.Serialization;
using System;
using System.Reflection;
using System.Collections.Generic;

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
            Simulations simulations = Utility.Xml.Deserialise(FileName) as Simulations;
            simulations.FileName = FileName;

            return simulations;
        }

        /// <summary>
        /// Create a simulations object by reading from the specified xml node
        /// </summary>
        public static Simulations Read(XmlNode xmlNode)
        {
            Simulations simulations = Utility.Xml.Deserialise(xmlNode) as Simulations;
            return simulations;
        }

        /// <summary>
        /// Resolve all links in all models.
        /// </summary>
        public void Initialise()
        {
            ResolveAllLinks();
            ConnectAllEvents(); // required for Initialised() calls
            InitialiseAllSimulations();
        }

        public void InitialiseAllSimulations()
        {
            foreach (object aModel in Models)
            {
                if (aModel is Simulation)
                {
                    (aModel as Simulation).Initialise();
                }
            }
        }

        public void ResolveAllLinks()
        {
            Utility.ModelFunctions.ResolveLinks(this);
        }

        public void ConnectAllEvents()
        {
            foreach (object aModel in Models)
            {
                if (aModel is Simulation)
                {
                    (aModel as Simulation).ConnectAllEvents();
                }
            }
        }

        public void DisconnectAllEvents()
        {
            foreach (object aModel in Models)
            {
                if (aModel is Simulation)
                {
                    (aModel as Simulation).DisconnectAllEvents();
                }
            }
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
            // Connect all events for the simulations we're about to run.
//            DisconnectAllEvents();  // cleanup any remaining
//            ConnectAllEvents();

            // Invoke the AllCommencing event.
            if (AllCommencing != null)
                AllCommencing(this, new EventArgs());

            // Run all simulations
            bool ok = true;
            foreach (Simulation simulation in FindAllSimulations())
                ok = simulation.Run() && ok;

            // Invoke the AllCompleted event.
            if (AllCompleted != null)
                AllCompleted(this, new EventArgs());

            return ok;
        }

        /// <summary>
        /// Run the specified simulation. Return true if it ran ok.
        /// </summary>
        public bool Run(Simulation Sim)
        {
            if (AllCommencing != null)
                AllCommencing(this, new EventArgs());

            Simulation simulation = Sim as Simulation;
//            simulation.DisconnectAllEvents();  // cleanup any remaining
//            simulation.ConnectAllEvents();
            bool ok = simulation.Run();

            if (AllCompleted != null)
                AllCompleted(this, new EventArgs());

            return ok;
        }

        /// <summary>
        /// Constructor, private to stop developers using it. Use Simulations.Read instead.
        /// </summary>
        private Simulations() { }

        /// <summary>
        /// Close the simulation
        /// </summary>
        public void Close()
        {
            foreach (object Model in Models)
                if (Model is Simulation)
                    (Model as Simulation).Close();

            if (AllCompleted != null)
                AllCompleted(this, null);
        }

        /// <summary>
        /// Find all simulations.
        /// </summary>
        public Simulation[] FindAllSimulations()
        {
            List<Simulation> simulations = new List<Simulation>();
            foreach (object Model in FindAll())
            {
                if (Model is Simulation)
                    simulations.Add(Model as Simulation);
                else if (Model is Creator)
                {
                    foreach (Model model in (Model as Creator).Create())
                        if (model is Simulation)
                            simulations.Add(model as Simulation);
                }
            }
            return simulations.ToArray();
        }

    }
}
