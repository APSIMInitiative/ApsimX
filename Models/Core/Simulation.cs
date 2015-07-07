using System.Reflection;
using System;
using Models;
using System.Diagnostics;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using APSIM.Shared.Utilities;

namespace Models.Core 
{
    /// <summary>
    /// A simulation model
    /// </summary>
    [Serializable]
    public class Simulation : Zone, JobManager.IRunnable
    {
        /// <summary>The _ is running</summary>
        private bool _IsRunning = false;

        /// <summary>Gets a value indicating whether this job is completed. Set by JobManager.</summary>
        [XmlIgnore]
        public bool IsCompleted { get; set; }

        /// <summary>Gets the error message. Can be null if no error. Set by JobManager.</summary>
        [XmlIgnore]
        public string ErrorMessage { get; set; }

        /// <summary>Gets a value indicating whether this instance is computationally time consuming.</summary>
        public bool IsComputationallyTimeConsuming { get { return true; } }

        /// <summary>The timer</summary>
        [NonSerialized]
        private Stopwatch timer;

        /// <summary>Occurs when [commencing].</summary>
        public event EventHandler Commencing;

        /// <summary>Occurs when [completed].</summary>
        public event EventHandler Completed;

        /// <summary>A locater object for finding models and variables.</summary>
        [NonSerialized]
        private Locater locater;

        /// <summary>Cache to speed up scope lookups.</summary>
        /// <value>The locater.</value>
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

        /// <summary>Get a reference to the clock model.</summary>
        [Link] private Clock Clock = null;

        /// <summary>To commence the simulation, this event will be invoked.</summary>
        public event EventHandler DoCommence;

        /// <summary>Return the filename that this simulation sits in.</summary>
        /// <value>The name of the file.</value>
        [XmlIgnore]
        public string FileName { get; set; }

        /// <summary>Return true if the simulation is running.</summary>
        /// <value>
        /// <c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunning { get { return _IsRunning; } }


        /// <summary>Constructor</summary>
        public Simulation()
        {
            locater = new Locater();
        }

        /// <summary>Run the simulation. Will throw on error.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="System.Exception">
        /// </exception>
        public void Run(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                StartRun();
                DoRun(sender);
                CleanupRun(null);
            }
            catch (ApsimXException err)
            {
                DateTime errorDate = Clock.Today;
                
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

                ErrorMessage = Msg;
                ISummary summary = Apsim.Find(this, typeof(Summary)) as ISummary;
                summary.WriteMessage(this, Msg);
                CleanupRun(Msg);

                throw new Exception(Msg);
            }
            catch (Exception err)
            {
                DateTime errorDate = Clock.Today;
                string Msg = "ERROR in file: " + FileName + "\r\n" +
                             "Simulation name: " + Name + "\r\n"; 
                Msg += err.Message;
                if (err.InnerException != null)
                    Msg += "\r\n" + err.InnerException.Message + "\r\n" + err.InnerException.StackTrace;
                else
                    Msg += "\r\n" + err.StackTrace;

                ErrorMessage = Msg; 
                ISummary summary = Apsim.Find(this, typeof(Summary)) as ISummary;
                summary.WriteMessage(this, Msg);
                CleanupRun(Msg);

                throw new Exception(Msg);
            }
            if (e != null)
                e.Result = this;
        }

        /// <summary>Startup the run.</summary>
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

        /// <summary>Perform the run. Will throw if error occurs.</summary>
        /// <param name="sender">The sender.</param>
        /// <exception cref="ApsimXException">Cannot invoke Commenced</exception>
        public void DoRun(object sender)
        {
            if (DoCommence != null)
                DoCommence.Invoke(sender, new EventArgs());
            else
                throw new ApsimXException(this, "Cannot invoke Commenced");
        }

        /// <summary>Cleanup after the run.</summary>
        public void CleanupRun(string errorMessage)
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
            if (errorMessage == null)
                Console.WriteLine("Completed: " + Path.GetFileNameWithoutExtension(FileName) + " - " + Name + " [" + timer.Elapsed.TotalSeconds.ToString("#.00") + " sec]");
            else
            {
                Console.WriteLine("Completed with errors: " + Path.GetFileNameWithoutExtension(FileName));
                Console.WriteLine(errorMessage);
            }
        }


    }


}