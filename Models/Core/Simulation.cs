using System.Reflection;
using System;
using Models;
using System.Diagnostics;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using APSIM.Shared.Utilities;
using Models.Factorial;
using System.ComponentModel;

namespace Models.Core 
{
    /// <summary>
    /// A simulation model
    /// </summary>
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Experiment))]
    [Serializable]
    public class Simulation : Zone, JobManager.IRunnable, JobManager.IComputationalyTimeConsuming
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

        /// <summary>Argument for a DoCommence event.</summary>
        public class CommenceArgs : EventArgs
        {
            /// <summary>Is a cancellation pending?</summary>
            public BackgroundWorker workerThread;
        }

        /// <summary>To commence the simulation, this event will be invoked.</summary>
        public event EventHandler<CommenceArgs> DoCommence;

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

        /// <summary>Called to start the job.</summary>
        /// <param name="jobManager">The job manager running this job.</param>
        /// <param name="workerThread">The thread this job is running on.</param>
        public void Run(JobManager jobManager, BackgroundWorker workerThread)
        {
            try
            {
                StartRun();
                DoRun(jobManager, workerThread);
                CleanupRun();
            }
            catch (ApsimXException err)
            {
                ErrorMessage = "ERROR in file: " + FileName + "\r\n" +
                               "Simulation name: " + Name + "\r\n" +
                               err.ToString();

                ISummary summary = Apsim.Find(this, typeof(Summary)) as ISummary;
                summary.WriteMessage(this, ErrorMessage);
                CleanupRun();

                throw new Exception(ErrorMessage);
            }
            catch (Exception err)
            {
                ErrorMessage = "ERROR in file: " + FileName + "\r\n" +
                               "Simulation name: " + Name + "\r\n" + 
                               err.ToString();

                ISummary summary = Apsim.Find(this, typeof(Summary)) as ISummary;
                summary.WriteMessage(this, ErrorMessage);
                CleanupRun();

                throw new Exception(ErrorMessage);
            }
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
            if (Commencing != null)
                Commencing.Invoke(this, new EventArgs());
        }

        /// <summary>Perform the run. Will throw if error occurs.</summary>
        /// <param name="jobManager">The job manager</param>
        /// <param name="workerThread">The thread this job is running on.</param>
        public void DoRun(JobManager jobManager, BackgroundWorker workerThread)
        {
            Console.WriteLine("File: " + Path.GetFileNameWithoutExtension(this.FileName) + ", Simulation " + this.Name + " has commenced.");
            if (DoCommence != null)
                DoCommence.Invoke(jobManager, new CommenceArgs() { workerThread = workerThread } );
            else
                throw new ApsimXException(this, "Cannot invoke Commenced");
        }

        /// <summary>Cleanup after the run.</summary>
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
            Console.WriteLine("File: " + Path.GetFileNameWithoutExtension(this.FileName) + ", Simulation " + this.Name + " complete. Time: " + timer.Elapsed.TotalSeconds.ToString("0.00 sec"));
        }
    }
}