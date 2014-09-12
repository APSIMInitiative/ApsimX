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
    public class Simulation : Zone, Utility.JobManager.IRunnable
    {
        private bool _IsRunning = false;

        [NonSerialized]
        private Stopwatch timer;

        public event EventHandler Commencing;

        public event EventHandler Completed;

        /// <summary>
        /// A locater object for finding models and variables.
        /// </summary>
        [NonSerialized]
        private Locater locater;

        /// <summary>
        /// Cache to speed up scope lookups.
        /// </summary>
        public Locater Locater
        {
            get
            {
                if (locater == null)
                {
                    locater = new Locater();
                }

                return locater;
            }
        }

        /// <summary>
        /// Get a reference to the clock model.
        /// </summary>
        [Link] private Clock Clock = null;

        /// <summary>
        /// To commence the simulation, this event will be invoked.
        /// </summary>
        public event EventHandler DoCommence;

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
            locater = new Locater();
        }

        /// <summary>
        /// Run the simulation. Will throw on error.
        /// </summary>
        public void Run(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                StartRun();
                DoRun(sender);
                CleanupRun();
            }
            catch (ApsimXException err)
            {
                DataStore store = new DataStore(this);

                string Msg = "ERROR in file: " + FileName + "\r\n" +
                             "Simulation name: " + Name + "\r\n";
                Msg += err.Message;
                if (err.InnerException != null)
                    Msg += "\r\n" + err.InnerException.Message + "\r\n" + err.InnerException.StackTrace;
                else
                    Msg += "\r\n" + err.StackTrace;

                string modelFullPath = string.Empty;
                if (err.model != null)
                {
                    modelFullPath = Apsim.FullPath(err.model);
                }
                store.WriteMessage(Name, Clock.Today, modelFullPath, err.Message, DataStore.ErrorLevel.Error);
                store.Disconnect();
                CleanupRun();
                throw new Exception(Msg);
            }
            catch (Exception err)
            {
                DataStore store = new DataStore(this);

                string Msg = "ERROR in file: " + FileName + "\r\n" +
                             "Simulation name: " + Name + "\r\n"; 
                Msg += err.Message;
                if (err.InnerException != null)
                    Msg += "\r\n" + err.InnerException.Message + "\r\n" + err.InnerException.StackTrace;
                else
                    Msg += "\r\n" + err.StackTrace;

                store.WriteMessage(Name, Clock.Today, "Unknown", err.Message, DataStore.ErrorLevel.Error);
                store.Disconnect();

                CleanupRun();
                throw new Exception(Msg);
            }
            if (e != null)
                e.Result = this;
        }

        /// <summary>
        /// Startup the run.
        /// </summary>
        public void StartRun()
        {
            timer = new Stopwatch();
            timer.Start();

            Apsim.ConnectEvents(this);
            Apsim.ResolveLinks(this);
            foreach (Model child in Apsim.ChildrenRecursively(this))
            {
                Apsim.ConnectEvents(child);
                Apsim.ResolveLinks(child);
            }

            _IsRunning = true;
            
            Locater.Clear();
            Console.WriteLine("Running: " + Path.GetFileNameWithoutExtension(FileName) + " - " + Name);
            if (Commencing != null)
                Commencing.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Perform the run. Will throw if error occurs.
        /// </summary>
        public void DoRun(object sender)
        {
            if (DoCommence != null)
                DoCommence.Invoke(sender, new EventArgs());
            else
                throw new ApsimXException(this, "Cannot invoke Commenced");
        }

        /// <summary>
        /// Cleanup after the run.
        /// </summary>
        public void CleanupRun()
        {
            _IsRunning = false;

            if (Completed != null)
                Completed.Invoke(this, null);

            Apsim.DisconnectEvents(this);
            Apsim.UnresolveLinks(this);
            foreach (Model child in Apsim.ChildrenRecursively(this))
            {
                Apsim.DisconnectEvents(child);
                Apsim.UnresolveLinks(child);
            }

            timer.Stop();
            Console.WriteLine("Completed: " + Path.GetFileNameWithoutExtension(FileName) + " - " + Name + " [" + timer.Elapsed.TotalSeconds.ToString("#.00") + " sec]");
        }

    }


}