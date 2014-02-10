using System.Reflection;
using System;
using Models;
using System.Diagnostics;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;

namespace Models.Core
{
    [Serializable]
    public class Simulation : Zone
    {
        private bool _IsRunning = false;

        // Private links
        [Link] private Clock Clock = null;

        /// <summary>
        /// To commence the simulation, this event will be invoked.
        /// </summary>
        public event EventHandler Commenced;

        /// <summary>
        /// Return the filename that this simulation sits in.
        /// </summary>
        [XmlIgnore]
        public string FileName { get; set; }

        /// <summary>
        /// Return true if the simulation is running.
        /// </summary>
        public bool IsRunning { get { return _IsRunning; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public Simulation()
        {
        }

        /// <summary>
        /// Run the simulation. Returns true if no fatal errors or exceptions.
        /// </summary>
        public bool Run()
        {
            bool ok = false;
            
            try
            {
                StartRun();
                DoRun();
                CleanupRun();
                ok = true;
            }
            catch (ApsimXException err)
            {
                DataStore store = new DataStore();
                store.Connect(Path.ChangeExtension(FileName, ".db"));

                string Msg = err.Message;
                if (err.InnerException != null)
                    Msg += "\r\n" + err.InnerException.Message + "\r\n" + err.InnerException.StackTrace;
                else
                    Msg += "\r\n" + err.StackTrace;

                store.WriteMessage(Name, Clock.Today, err.ModelFullPath, err.Message, DataStore.ErrorLevel.Error);

                CleanupRun();
                ok = false;
            }

            return ok;
        }

        /// <summary>
        /// Startup the run.
        /// </summary>
        public void StartRun()
        {
            _IsRunning = true;
            Model.Variables.ClearCache();
            Model.Scope.ClearCache();
            AllModels.ForEach(CallOnCommencing);
        }

        /// <summary>
        /// Perform the run. Will throw if error occurs.
        /// </summary>
        public void DoRun()
        {
            if (Commenced != null)
                Commenced.Invoke(this, new EventArgs());
            else
                throw new ApsimXException(FullPath, "Cannot invoke Commenced");
        }

        /// <summary>
        /// Cleanup after the run.
        /// </summary>
        public void CleanupRun()
        {
            CallOnCompleted(this);
            AllModels.ForEach(CallOnCompleted);
            _IsRunning = false;
        }



    }


}