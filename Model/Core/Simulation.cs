using System.Reflection;
using System;
using Model.Components;
using System.Diagnostics;
using System.Xml;

namespace Model.Core
{
    public class Simulation : Zone
    {
        // Private links
        [Link] private ISummary Summary = null;

        /// <summary>
        /// To commence the simulation, this event will be invoked.
        /// </summary>
        public event NullTypeDelegate Commenced;

        /// <summary>
        /// When the simulation is finished, this event will be invoked
        /// </summary>
        public event NullTypeDelegate Completed;

        /// <summary>
        /// Run the simulation. Returns true if no fatal errors or exceptions.
        /// </summary>
        public bool Run()
        {
            try
            {
                Initialise();
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
                Summary.WriteMessage(Msg);

                if (Completed != null)
                    Completed.Invoke();

                return false;
            }
        }

        /// <summary>
        /// Simulation is being initialised.
        /// </summary>
        public override void OnInitialised()
        {
            string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Summary.WriteProperty("Version", Version);
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
            Summary.WriteProperty("Hierarchy", Hierarchy);
        }


    }

}