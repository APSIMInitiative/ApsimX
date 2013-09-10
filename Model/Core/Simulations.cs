using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Collections;
using Model.Core;
using System.Xml.Serialization;
using Model.Components;
using System.Diagnostics;

namespace Model.Core
{
    /// <summary>
    /// Encapsulates a collection of simulations. It is responsible for creating this collection,
    /// changing the structure of the components within the simulations, renaming components, adding
    /// new ones, deleting components. The user interface talks to an instance of this class.
    /// </summary>
    public class Simulations : Zone
    {
        private string _FileName;

        /// <summary>
        /// When all simulations have finished, this event will be invoked
        /// </summary>
        public event NullTypeDelegate AllCompleted;

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
        /// Write the specified simulation set to the specified filename
        /// </summary>
        public void Write(string FileName)
        {
            StreamWriter Out = new StreamWriter(FileName);
            Out.Write(Utility.Xml.Serialise(this, true));
            Out.Close();
            this.FileName = FileName;
        }

        /// <summary>
        /// Read XML from specified reader. Called during Deserialisation.
        /// </summary>
        public override void ReadXml(XmlReader reader)
        {
            base.ReadXml(reader);
            Name = "Simulations";
            ResolveLinks(this);
        }

        /// <summary>
        /// Run all simulations. Return true if all ran ok.
        /// </summary>
        public bool Run()
        {
            InitialiseNonSimulations();

            bool ok = true;
            foreach (object Model in Models)
                if (Model is Simulation)
                    ok = (Model as Simulation).Run() && ok;

            if (AllCompleted != null)
                AllCompleted();
            return ok;
        }

        /// <summary>
        /// Run the specified simulation. Return true if it ran ok.
        /// </summary>
        public bool Run(ISimulation Sim)
        {
            InitialiseNonSimulations();

            Simulation Simulation = Sim as Simulation;
            bool ok = Simulation.Run();

            if (AllCompleted != null)
                AllCompleted();
            return ok;
        }

        /// <summary>
        /// Initialise all non simulation models.
        /// </summary>
        private void InitialiseNonSimulations()
        {
            foreach (object Model in Models)
            {
                if (!(Model is Simulation))
                    Initialise(Model);
            }
        }

        /// <summary>
        /// Return a model given the full path. Format of full path:
        /// Simulations.Test.Field.Report        /// 
        /// </summary>
        /// <returns>Returns the found object or null if not found.</returns>
        public override object Get(string FullPath)
        {
            if (FullPath == "Simulations")
                return this;

            int PosFirstPeriod = FullPath.IndexOf('.');
            if (PosFirstPeriod != -1)
            {
                string Remainder = FullPath.Substring(PosFirstPeriod + 1);
                return base.Get(Remainder);
            }
            return null;
        }

        /// <summary>
        /// Constructor, private to stop developers using it. Use 'Utility.Xml.Deserialise' instead.
        /// </summary>
        private Simulations() { }

    }
}
