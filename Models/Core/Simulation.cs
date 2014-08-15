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
    public class Simulation : Model, Utility.JobManager.IRunnable
    {
        private bool _IsRunning = false;

        [NonSerialized]
        private Stopwatch timer;

        public event EventHandler OnCompleted;

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

                string Msg = err.Message;
                if (err.InnerException != null)
                    Msg += "\r\n" + err.InnerException.Message + "\r\n" + err.InnerException.StackTrace;
                else
                    Msg += "\r\n" + err.StackTrace;

                store.WriteMessage(Name, Clock.Today, err.ModelFullPath, err.Message, DataStore.ErrorLevel.Error);
                store.Disconnect();
                CleanupRun();
                throw new Exception(Msg);
            }
            catch (Exception err)
            {
                DataStore store = new DataStore(this);

                string Msg = err.Message;
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

            Events.Connect();
            ResolveLinks();
            foreach (Model child in Children.AllRecursively)
            {
                child.Events.Connect();
                child.ResolveLinks();
            }

            _IsRunning = true;
            
            Locater.Clear();
            Console.WriteLine("Running: " + Path.GetFileNameWithoutExtension(FileName) + " - " + Name);
            foreach (Model child in Children.AllRecursively)
                child.OnSimulationCommencing();
        }

        /// <summary>
        /// Perform the run. Will throw if error occurs.
        /// </summary>
        public void DoRun(object sender)
        {
            if (DoCommence != null)
                DoCommence.Invoke(sender, new EventArgs());
            else
                throw new ApsimXException(FullPath, "Cannot invoke Commenced");
        }

        /// <summary>
        /// Cleanup after the run.
        /// </summary>
        public void CleanupRun()
        {
            _IsRunning = false;

            OnSimulationCompleted();
            foreach (Model child in Children.AllRecursively)
                child.OnSimulationCompleted();

            if (OnCompleted != null)
                OnCompleted.Invoke(this, null);

            Events.Disconnect();
            UnResolveLinks();
            foreach (Model child in Children.AllRecursively)
            {
                child.Events.Disconnect();
                child.UnResolveLinks();
            }

            timer.Stop();
            Console.WriteLine("Completed: " + Path.GetFileNameWithoutExtension(FileName) + " - " + Name + " [" + timer.Elapsed.TotalSeconds.ToString("#.00") + " sec]");
        }

    }


}