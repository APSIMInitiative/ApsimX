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

namespace Model.Core
{
    /// <summary>
    /// Encapsulates a collection of simulations. It is responsible for creating this collection,
    /// changing the structure of the components within the simulations, renaming components, adding
    /// new ones, deleting components. The user interface talks to an instance of this class.
    /// </summary>
    public class Simulations
    {
        private string _FileName;
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
                foreach (Simulation Simulation in Sims)
                    Simulation.FileName = _FileName;
            }
        }

        /// <summary>
        /// Collection of simulations.
        /// </summary>
        /// 
        [XmlElement("Simulation")]
        public List<Simulation> Sims { get; set; }

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
        /// Run all simulations. Return true if all ran ok.
        /// </summary>
        public bool Run()
        {
            bool ok = true;
            foreach (Simulation Simulation in Sims)
                ok = Simulation.Run() && ok;

            foreach (Simulation Simulation in Sims)
                Simulation.OnAllComplete();
            return ok;
        }

        /// <summary>
        /// Run the specified simulation. Return true if it ran ok.
        /// </summary>
        public bool Run(ISimulation Sim)
        {
            Simulation Simulation = Sim as Simulation;
            bool ok = Simulation.Run();

            Simulation.OnAllComplete();
            return ok;
        }

        /// <summary>
        /// Return a model given the full path. Format of full path:
        /// Simulations.Test.Field.Report        /// 
        /// </summary>
        /// <returns>Returns the found object or null if not found.</returns>
        public object Get(string FullPath)
        {
            if (FullPath == "Simulations")
                return this;
            int PosFirstPeriod = FullPath.IndexOf('.');
            if (PosFirstPeriod != -1)
            {
                string FirstWord = FullPath.Substring(0, PosFirstPeriod);
                string SecondWord;
                int PosSecondPeriod = FullPath.IndexOf('.', PosFirstPeriod+1);
                if (PosSecondPeriod != -1)
                    SecondWord = FullPath.Substring(PosFirstPeriod+1, PosSecondPeriod-PosFirstPeriod-1);
                else
                    SecondWord = FullPath.Substring(PosFirstPeriod+1);
                
                if (FirstWord == "Simulations")
                {
                    foreach (Simulation Simulation in Sims)
                        if (Simulation.Name == SecondWord)
                        {
                            if (PosSecondPeriod == -1)
                                return Simulation;
                            else
                                return Simulation.Get(FullPath.Substring(PosSecondPeriod + 1));
                        }
                            
                }
            }
            return null;
        }

        /// <summary>
        /// Constructor, private to stop developers using it. Use 'Utility.Xml.Deserialise' instead.
        /// </summary>
        private Simulations() { }

    }
}
