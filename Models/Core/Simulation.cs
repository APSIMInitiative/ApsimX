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

        public event EventHandler OnCompleted;

        /// <summary>
        /// Cache to speed up scope lookups.
        /// </summary>
        [NonSerialized]
        private Dictionary<string, Model[]> _ScopeCache = null;

        /// <summary>
        /// Cache to speed up variable lookups.
        /// </summary>
        [NonSerialized]
        private Dictionary<string, IVariable> _VariableCache = null;

        /// <summary>
        /// Get a reference to the clock model.
        /// </summary>
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
        /// Return a reference to the scope cache.
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, Model[]> ScopeCache 
        { 
            get 
            { 
                if (_ScopeCache == null)
                    _ScopeCache = new Dictionary<string, Model[]>();
                return _ScopeCache; 
            } 
        }

        /// <summary>
        /// Return a reference to the variable cache.
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, IVariable> VariableCache 
        { 
            get 
            { 
                if (_VariableCache == null)
                    _VariableCache = new Dictionary<string, IVariable>();
                return _VariableCache; 
            } 
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
                AllModels.ForEach(Disconnect);
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
            AllModels.ForEach(Connect);

            timer = new Stopwatch();
            _IsRunning = true;
            VariableCache.Clear();
            ScopeCache.Clear();
            Console.WriteLine("Running: " + Path.GetFileNameWithoutExtension(FileName) + " - " + Name);
            timer.Start();
            AllModels.ForEach(CallOnCommencing);
        }

        /// <summary>
        /// Perform the run. Will throw if error occurs.
        /// </summary>
        public void DoRun(object sender)
        {
            if (Commenced != null)
                Commenced.Invoke(sender, new EventArgs());
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

            if (OnCompleted != null)
                OnCompleted.Invoke(this, null);

            timer.Stop();
            Console.WriteLine("Completed: " + Path.GetFileNameWithoutExtension(FileName) + " - " + Name + " [" + timer.Elapsed.TotalSeconds.ToString("#.00") + " sec]");
            _IsRunning = false;

            AllModels.ForEach(Disconnect);

        }



    }


}