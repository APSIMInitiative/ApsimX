﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using APSIM.Shared.JobRunning;
using Models.Core.ApsimFile;
using Models.PostSimulationTools;
using Models.Storage;

namespace Models.Core.Run
{

    /// <summary>
    /// Encapsulates a collection of jobs that are to be run. A job can be a simulation run or 
    /// a class instance that implements IRunnable e.g. EXCEL input run.
    /// </summary>
    public class SimulationGroup : JobManager, IReportsStatus
    {
        /// <summary>The model to use to search for simulations to run.</summary>
        private IModel relativeTo;

        /// <summary>Top level model.</summary>
        private IModel rootModel;

        /// <summary>Run simulations?</summary>
        private bool runSimulations;

        /// <summary>Run post simulation tools?</summary>
        private bool runPostSimulationTools;

        /// <summary>Run tests?</summary>
        private bool runTests;

        /// <summary>Has this instance been initialised?</summary>
        private bool initialised = false;

        /// <summary>Specific simulation names to run.</summary>
        private IEnumerable<string> simulationNamesToRun;

        /// <summary>A pattern used to determine simulations to run.</summary>
        private Regex patternMatch;

        /// <summary>The related storage model.</summary>
        private IDataStore storage;

        /// <summary>Time when job collection first started.</summary>
        private DateTime startTime;

        /// <summary>Contstructor</summary>
        /// <param name="relativeTo">The model to use to search for simulations to run.</param>
        /// <param name="runSimulations">Run simulations?</param>
        /// <param name="runPostSimulationTools">Run post simulation tools?</param>
        /// <param name="runTests">Run tests?</param>
        /// <param name="simulationNamesToRun">Only run these simulations.</param>
        /// <param name="simulationNamePatternMatch">A regular expression used to match simulation names to run.</param>
        public SimulationGroup(IModel relativeTo,
                             bool runSimulations = true,
                             bool runPostSimulationTools = true,
                             bool runTests = true,
                             IEnumerable<string> simulationNamesToRun = null,
                             string simulationNamePatternMatch = null)
        {
            this.relativeTo = relativeTo;
            this.runSimulations = runSimulations;
            this.runPostSimulationTools = runPostSimulationTools;
            this.runTests = runTests;
            this.simulationNamesToRun = simulationNamesToRun;

            if (relativeTo is Playlist) //check if node was a playlist
            {
                if ((relativeTo as Playlist).Text.Length > 0)
                {
                    this.simulationNamesToRun = (relativeTo as Playlist).GetListOfSimulations();
                    if (this.simulationNamesToRun == null || this.simulationNamesToRun.Count() == 0)
                        throw new Exception("Playlist was used but no simulations or experiments match the contents of the list.");
                }
                //need to set the relative back to simulations so the runner can find all the simulations 
                //when it comes time to run.
                this.relativeTo = relativeTo.FindAncestor<Simulations>();
            }

            if (simulationNamePatternMatch != null)
                patternMatch = new Regex(simulationNamePatternMatch);

            Initialise();
        }

        /// <summary>Contstructor</summary>
        /// <param name="fileName">The name of the file to run.</param>
        /// <param name="runTests">Run tests?</param>
        /// <param name="simulationNamePatternMatch">A regular expression used to match simulation names to run.</param>
        public SimulationGroup(string fileName,
                             bool runTests = true,
                             string simulationNamePatternMatch = null)
        {
            this.FileName = fileName;
            this.runSimulations = true;
            this.runPostSimulationTools = true;
            this.runTests = runTests;

            if (simulationNamePatternMatch != null)
                patternMatch = new Regex(simulationNamePatternMatch);

            Task.Run(() => { Initialise(); });
        }

        /// <summary>Name of file where the jobs came from.</summary>
        public string FileName { get; set; }

        /// <summary>A list of exceptions thrown before and after the simulation runs. Will be null when no exceptions found.</summary>
        public List<Exception> PrePostExceptionsThrown { get; private set; }

        /// <summary>
        /// Status of the jobs.
        /// </summary>
        /// <remarks>
        /// I'm not sure that this really belongs here, but since this class
        /// handles the running of post-simulation tools, it kind of has to be here.
        /// </remarks>
        public string Status
        {
            get
            {
                if (_status.StartsWith("Waiting for datastore"))
                {
                    return _status + " - " + storage.Writer.CommandCount().ToString() + " operations remaining";
                }
                return _status;
            }
            private set
            {
                _status = value;
            }
        }

        private string _status;

        /// <summary>
        /// List all simulation names beneath a given model.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="simulationNamesToRun"></param>
        /// <returns></returns>
        public List<string> FindAllSimulationNames(IModel model, IEnumerable<string> simulationNamesToRun)
        {
            return FindListOfSimulationsToRun(model, simulationNamesToRun).Select(j => j.Name).ToList();
        }

        /// <summary>Find and return a list of duplicate simulation names.</summary>
        private List<string> FindDuplicateSimulationNames()
        {
            if (rootModel == null)
                return new List<string>();

            IEnumerable<ISimulationDescriptionGenerator> allSims = rootModel.FindAllDescendants<ISimulationDescriptionGenerator>();

            List<string> dupes = new List<string>();
            foreach (var simType in allSims.GroupBy(s => s.GetType()))
            {
                dupes.AddRange(simType.Select(s => (s as IModel).Name)
                    .GroupBy(i => i)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key));
            }

            return dupes;
        }

        /// <summary>Called once to do initialisation before any jobs are run. Should throw on error.</summary>
        protected override void PreRun()
        {
            if (!initialised)
                SpinWait.SpinUntil(() => initialised);

            if (storage?.Writer != null)
                storage.Writer.TablesModified.Clear();
        }

        /// <summary>Called once when all jobs have completed running. Should throw on error.</summary>
        protected override void PostAllRuns()
        {
            Status = "Waiting for datastore to finish writing";
            StorageFinishWriting();

            if (runPostSimulationTools)
                RunPostSimulationTools();

            if (runTests)
                RunTests();

            StorageFinishWriting();
        }

        /// <summary>
        /// Tell the datastore to finish writing.
        /// </summary>
        public void StorageFinishWriting()
        {
            storage?.Writer.Stop();
            storage?.Reader.Refresh();
        }

        /// <summary>Initialise the instance.</summary>
        private void Initialise()
        {
            startTime = DateTime.Now;
            Status = "Finding simulations to run";

            try
            {
                if (relativeTo == null)
                {

                    if (!File.Exists(FileName))
                        throw new Exception("Cannot find file: " + FileName);
                    Simulations sims = FileFormat.ReadFromFile<Simulations>(FileName, e => throw e, false).NewModel as Simulations;
                    relativeTo = sims;
                }

                if (relativeTo != null)
                {
                    // If this simulation was not created from deserialisation then we need
                    // to parent all child models correctly and call OnCreated for each model.
                    bool hasBeenDeserialised = relativeTo.Children.Count > 0 &&
                                               relativeTo.Children[0].Parent == relativeTo;
                    if (!hasBeenDeserialised)
                    {
                        // Parent all models.
                        relativeTo.ParentAllDescendants();

                        // Call OnCreated in all models.
                        foreach (IModel model in relativeTo.FindAllDescendants().ToList())
                            model.OnCreated();
                    }

                    // Find the root model.
                    rootModel = relativeTo;
                    while (rootModel.Parent != null)
                        rootModel = rootModel.Parent;

                    if (rootModel is Simulations)
                        FileName = (rootModel as Simulations).FileName;
                    else if (rootModel is Simulation)
                        FileName = (rootModel as Simulation).FileName;

                    // Check for duplicate simulation names.
                    string[] duplicates = FindDuplicateSimulationNames().ToArray();
                    if (duplicates != null && duplicates.Any())
                        throw new Exception($"Duplicate simulation names found: {string.Join(", ", duplicates)}");

                    // Check for duplicate soils
                    foreach (var zone in rootModel.FindAllDescendants<Zone>().Where(z => z.Enabled))
                    {
                        bool duplicateSoils = zone.FindAllChildren<Models.Soils.Soil>().Where(s => s.Enabled).Count() > 1;
                        if (duplicateSoils)
                            throw new Exception($"Duplicate soils found in zone: {zone.FullPath}");
                    }

                    // Publish BeginRun event.
                    var e = new Events(rootModel);
                    e.Publish("BeginRun", new object[] { this, new EventArgs() });

                    // Find a storage model.
                    storage = rootModel.FindChild<IDataStore>();

                    // Find simulations to run.
                    if (runSimulations)
                    {
                        var jobs = FindListOfSimulationsToRun(relativeTo, simulationNamesToRun).ToList();

                        if (storage != null)
                        {
                            var names = new List<string>();
                            foreach (var job in jobs)
                            {
                                if (job is SimulationDescription)
                                {
                                    var description = job as SimulationDescription;
                                    foreach (var name in description.Descriptors.Where(d => d.Name == "SimulationName").Select(d => d.Value))
                                        names.Add(name);
                                }
                            }
                            storage.Writer.Clean(names, false);
                        }
                        foreach (IRunnable job in jobs)
                            Add(job);
                    }

                    if (numJobsToRun == 0)
                        Add(new EmptyJob());
                }
            }
            catch (Exception readException)
            {
                Exception exceptionToStore = readException;
                if (FileName != null)
                    exceptionToStore = new Exception("Error in file:" + FileName, readException);
                AddException(exceptionToStore);
            }
            initialised = true;
        }

        /// <summary>Determine the list of jobs to run</summary>
        /// <param name="relativeTo">The model to use to search for simulations to run.</param>
        /// <param name="simulationNamesToRun">Only run these simulations.</param>
        private IEnumerable<IRunnable> FindListOfSimulationsToRun(IModel relativeTo, IEnumerable<string> simulationNamesToRun)
        {
            if (relativeTo is Simulation sim)
            {
                if (SimulationNameIsMatched(relativeTo.Name))
                    yield return sim.GenerateSimulationDescriptions().FirstOrDefault();
            }
            else if (relativeTo is ISimulationDescriptionGenerator)
            {
                foreach (var description in (relativeTo as ISimulationDescriptionGenerator).GenerateSimulationDescriptions())
                    if (SimulationNameIsMatched(description.Name))
                        yield return description;
            }
            else if (relativeTo is Folder || relativeTo is Simulations)
            {
                // Get a list of all models we're going to run.
                foreach (var child in relativeTo.Children)
                    foreach (IRunnable job in FindListOfSimulationsToRun(child, simulationNamesToRun))
                        yield return job;
            }
            else if (relativeTo is IRunnable runnable)
                yield return runnable;
        }

        /// <summary>Return true if simulation name is a match.</summary>
        /// <param name="simulationName">Simulation name to look for.</param>
        /// <returns>True if matched.</returns>
        private bool SimulationNameIsMatched(string simulationName)
        {
            if (patternMatch != null)
                return patternMatch.Match(simulationName).Success;
            else
                return simulationNamesToRun == null || simulationNamesToRun.Contains(simulationName);
        }

        /// <summary>Run all post simulation tools.</summary>
        private void RunPostSimulationTools()
        {
            // Call all post simulation tools.
            object[] args = new object[] { this, new EventArgs() };
            foreach (IPostSimulationTool tool in FindPostSimulationTools())
            {
                storage?.Writer.WaitForIdle();
                storage?.Reader.Refresh();

                DateTime startTime = DateTime.Now;
                Exception exception = null;
                try
                {
                    if (tool.Enabled)
                    {
                        Status = $"Running post-simulation tool {(tool as IModel).Name}";
                        if (rootModel is Simulations)
                            (rootModel as Simulations).Links.Resolve(tool as IModel);
                        tool.Run();
                    }
                }
                catch (Exception err)
                {
                    exception = err;
                    AddException(err);
                }
            }
        }

        private IEnumerable<IPostSimulationTool> FindPostSimulationTools()
        {
            return relativeTo.FindAllInScope<IPostSimulationTool>()
                            .Where(t => t.FindAllAncestors()
                                         .All(a => !(a is ParallelPostSimulationTool || a is SerialPostSimulationTool)));
        }

        /// <summary>Run all tests.</summary>
        private void RunTests()
        {
            storage?.Writer.WaitForIdle();
            storage?.Reader.Refresh();

            List<object> services;
            if (relativeTo is Simulations)
                services = (relativeTo as Simulations).GetServices();
            else
            {
                Simulations sims = relativeTo.FindInScope<Simulations>();
                if (sims != null)
                    services = sims.GetServices();
                else if (relativeTo is Simulation)
                    services = (relativeTo as Simulation).Services;
                else
                {
                    services = new List<object>();
                    if (storage != null)
                        services.Add(storage);
                }
            }

            var links = new Links(services);
            foreach (ITest test in rootModel.FindAllDescendants<ITest>())
            {
                DateTime startTime = DateTime.Now;

                links.Resolve(test as IModel, true);

                // If we run into problems, we will want to include the name of the test in the 
                // exception's message. However, tests may be manager scripts, which always have
                // a name of 'Script'. Therefore, if the test's parent is a Manager, we use the
                // manager's name instead.
                string testName = test.Parent is Manager ? test.Parent.Name : test.Name;
                Exception exception = null;
                try
                {
                    Status = "Running tests";
                    test.Run();
                }
                catch (Exception err)
                {
                    exception = err;
                    AddException(new Exception("Encountered an error while running test " + testName + " (" + FileName + ")", err));
                }
            }
        }

        /// <summary>
        /// Add an exception to our list of exceptions.
        /// </summary>
        /// <param name="err">The exception to add.</param>
        private void AddException(Exception err)
        {
            if (err != null)
            {
                if (PrePostExceptionsThrown == null)
                    PrePostExceptionsThrown = new List<Exception>();
                PrePostExceptionsThrown.Add(err);
            }
        }

        /// <summary>
        /// Tells us to Dispose (close) our datastore
        /// </summary>
        public void DisposeStorage()
        {
            storage?.Dispose();
        }
    }
}