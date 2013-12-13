using System.Reflection;
using System;
using Models;
using System.Diagnostics;
using System.Xml;
using System.Collections.Generic;

namespace Models.Core
{
    [Serializable]
    public class Simulation : Zone
    {


        // Private links
        [Link] private ISummary Summary = null;

        /// <summary>
        /// This event will be invoked when the simulation is initialised.
        /// </summary>
        public event EventHandler Initialised;

        /// <summary>
        /// To commence the simulation, this event will be invoked.
        /// </summary>
        public event EventHandler Commenced;

        /// <summary>
        /// This event will be invoked when the simulation has completed.
        /// </summary>
        public event EventHandler Completed;

        /// <summary>
        /// Run the simulation. Returns true if no fatal errors or exceptions.
        /// </summary>
        public bool Run()
        {
            bool ok;
            try
            {
                if (Initialised != null)
                    Initialised(this, new EventArgs());

                if (Commenced != null)
                    Commenced.Invoke(this, new EventArgs());

                ok = true;
            }
            catch (Exception err)
            {
                string Msg = err.Message;
                if (err.InnerException != null)
                    Msg += "\r\n" + err.InnerException.Message + "\r\n" + err.InnerException.StackTrace;
                else
                    Msg += "\r\n" + err.StackTrace;
                Summary.WriteError(Msg);

                ok = false;
            }

            if (Completed != null)
                Completed.Invoke(this, new EventArgs());

            return ok;
        }

        public void Initialise()
        {
            if (Initialised != null)
                Initialised(this, new EventArgs());
        }

        public void Close()
        {
            if (Completed != null)
                Completed.Invoke(this, new EventArgs());
        }


    }

}