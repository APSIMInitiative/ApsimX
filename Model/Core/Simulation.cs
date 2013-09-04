using System.Reflection;
using System;
using Model.Components;
using System.Diagnostics;
using System.Xml;

namespace Model.Core
{
    public class Simulation : Zone, ISimulation
    {
        // Private links
        [Link] private DataStore DataStore = null;

        /// <summary>
        /// To commence the simulation, this event will be invoked.
        /// </summary>
        public event NullTypeDelegate Commenced;

        /// <summary>
        /// When the simulation is finished, this event will be invoked
        /// </summary>
        public event NullTypeDelegate Completed;

        /// <summary>
        /// When all simulations have finished, this event will be invoked
        /// </summary>
        public event NullTypeDelegate AllCompleted;

        /// <summary>
        /// Name of file containing the simulation.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Run the simulation. Returns true if no fatal errors or exceptions.
        /// </summary>
        public bool Run()
        {
            try
            {
                Initialise(this);
                if (Commenced != null)
                    Commenced.Invoke();

                if (Completed != null)
                    Completed.Invoke();
                return true;
            }
            catch (Exception err)
            {
                string Msg = err.Message;
                if (err.InnerException != null)
                    Msg += "\r\n" + err.InnerException.Message + "\r\n" + err.InnerException.StackTrace;
                else
                    Msg += "\r\n" + err.StackTrace;
                Console.WriteLine(Msg);
                DataStore.WriteMessage(err.Source, Msg);

                if (Completed != null)
                    Completed.Invoke();

                return false;
            }
        }

        /// <summary>
        /// Read XML from specified reader. Called during Deserialisation.
        /// </summary>
        public override void ReadXml(XmlReader reader)
        {
            base.ReadXml(reader);
            ResolveLinks(this);
        }

        /// <summary>
        /// All simulations have completed - invoke event.
        /// </summary>
        public void OnAllComplete()
        {
            if (AllCompleted != null)
                AllCompleted();
        }

        /// <summary>
        /// Simulation is being initialised.
        /// </summary>
        private void OnInitialised()
        {
            string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            DataStore.WriteProperty("Version", Version);
            string Hierarchy = "";
            foreach (object Child in Models)
            {
                Hierarchy += " |- " + Child.GetType().Name + "\r\n";
                if (Child is Zone)
                {
                    Zone Area = Child as Zone;
                    foreach (object AreaChild in Area.Models)
                        Hierarchy += "    |- " + AreaChild.GetType().Name + "\r\n";
                }
            }
            DataStore.WriteProperty("Hierarchy", Hierarchy);
        }


    }

}