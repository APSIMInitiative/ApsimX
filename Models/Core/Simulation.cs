using APSIM.Shared.JobRunning;
using Models.Core.Run;
using Models.Factorial;
using Models.Storage;
using Models.Optimisation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Data;
using Models.Soils;
using APSIM.Core;

namespace Models.Core
{
    /// <summary>
    /// A simulation model
    /// </summary>
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Experiment))]
    [ValidParent(ParentType = typeof(Morris))]
    [ValidParent(ParentType = typeof(Sobol))]
    [ValidParent(ParentType = typeof(CroptimizR))]
    [Serializable]
    public class Simulation : Model, IRunnable, ISimulationDescriptionGenerator, IReportsStatus, IScopedModel, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        [Link]
        private ISummary summary = null;

        /// <summary>Invoked when simulation is about to commence.</summary>
        public event EventHandler Commencing;

        /// <summary>Invoked to signal start of simulation.</summary>
        public event EventHandler<CommenceArgs> DoCommence;

        /// <summary>Invoked when the simulation is completed.</summary>
        public event EventHandler Completed;

        /// <summary>Return total area.</summary>
        public double Area
        {
            get
            {
                return this.Node.FindChildren<Zone>().Sum(z => (z as Zone).Area);
            }
        }


        /// <summary>
        /// An enum that is used to indicate message severity when writing messages to the .db
        /// </summary>
        public enum ErrorLevel
        {
            /// <summary>Information</summary>
            Information,

            /// <summary>Warning</summary>
            Warning,

            /// <summary>Error</summary>
            Error
        };

        /// <summary>
        /// An enum that is used to indicate message severity when writing messages to the status window.
        /// </summary>
        public enum MessageType
        {
            /// <summary>Information</summary>
            Information,

            /// <summary>Warning</summary>
            Warning
        };

        /// <summary>
        /// Returns the job's progress as a real number in range [0, 1].
        /// </summary>
        public double Progress
        {
            get
            {
                Clock c = Structure.FindChild<Clock>();
                if (c == null)
                    return 0;
                else
                    return c.FractionComplete;
            }
        }

        /// <summary>Is the simulation running?</summary>
        public bool IsRunning { get; private set; } = false;

        /// <summary>
        /// Is this Simulation in the process of being initialised?
        /// Use with caution! Leaving this set to "true" will block its
        /// execution thread.
        /// </summary>
        [JsonIgnore]
        public bool IsInitialising => Node.IsInitialising;

        /// <summary>A list of keyword/value meta data descriptors for this simulation.</summary>
        public List<SimulationDescription.Descriptor> Descriptors { get; set; }

        /// <summary>Return the filename that this simulation sits in.</summary>
        /// <value>The name of the file.</value>
        [JsonIgnore]
        public string FileName { get; set; }

        /// <summary>Collection of models that will be used in resolving links. Can be null.</summary>
        [JsonIgnore]
        public List<object> ModelServices { get; set; } = new List<object>();

        /// <summary>Status message.</summary>
        public string Status => Structure.FindChildren<IReportsStatus>(recurse: true).FirstOrDefault(s => !string.IsNullOrEmpty(s.Status))?.Status;

        /// <summary>
        /// Called when models should disconnect from events to which they've
        /// dynamically subscribed.
        /// </summary>
        public event EventHandler UnsubscribeFromEvents;

        /// <summary>
        /// Initialise model.
        /// </summary>
        public override void OnCreated()
        {
            base.OnCreated();
            FileName = Node.FileName;
        }

        /// <summary>Gets the next job to run</summary>
        public List<SimulationDescription> GenerateSimulationDescriptions()
        {
            var simulationDescription = new SimulationDescription(this);

            // Add a folderName descriptor.
            var folderNode = Structure.FindParent<Folder>(recurse: true);
            if (folderNode != null)
                simulationDescription.Descriptors.Add(new SimulationDescription.Descriptor("FolderName", folderNode.Name));

            simulationDescription.Descriptors.Add(new SimulationDescription.Descriptor("SimulationName", Name));

            foreach (var zone in Structure.FindChildren<Zone>(recurse: true))
                simulationDescription.Descriptors.Add(new SimulationDescription.Descriptor("Zone", zone.Name));

            return new List<SimulationDescription>() { simulationDescription };
        }

        /// <summary>
        /// Prepare the simulation for running.
        /// </summary>
        public void Prepare()
        {
            try
            {
                // Remove disabled models.
                RemoveDisabledModels(this);

                // Standardise the soil.
                var soils = Structure.FindChildren<Soil>(recurse: true);
                foreach (Soils.Soil soil in soils)
                    soil.Sanitise();

                CheckNotMultipleSoilWaterModels(this);

                // Call OnPreLink in all models.
                // Note the ToList(). This is important because some models can
                // add/remove models from the simulations tree in their OnPreLink()
                // method, and FindAllDescendants() is lazy.
                Structure.FindChildren<IModel>(recurse: true).ToList().ForEach(model => model.OnPreLink());

                if (ModelServices == null || ModelServices.Count < 1)
                {
                    var simulations = Structure.FindParent<Simulations>(recurse: true);
                    if (simulations != null)
                        ModelServices = simulations.GetServices();
                    else
                    {
                        ModelServices = new List<object>();
                        IDataStore storage = Structure.Find<IDataStore>();
                        if (storage != null)
                            ModelServices.Add(Structure.Find<IDataStore>());
                    }
                }

                var links = new Links(ModelServices);
                var events = new Events(this);

                // Connect all events.
                events.ConnectEvents();

                // Resolve all links
                links.Resolve(this, true, throwOnFail: true);

                StoreFactorsInDataStore();

                events.Publish("SubscribeToEvents", new object[] { this, EventArgs.Empty });
            }
            catch (Exception err)
            {
                throw new SimulationException("", err, Name, FileName);
            }
        }

        /// <summary>
        /// Runs the simulation on the current thread and waits for the simulation
        /// to complete before returning to caller. Simulation is NOT cloned before
        /// running. Use instance of Runner to get more options for running a
        /// simulation or groups of simulations.
        /// </summary>
        /// <param name="cancelToken">Is cancellation pending?</param>
        public void Run(CancellationTokenSource cancelToken = null)
        {
            IsRunning = true;
            Exception simulationError = null;

            // If the cancelToken is null then give it a default one. This can happen
            // when called from the unit tests.
            if (cancelToken == null)
                cancelToken = new CancellationTokenSource();

            try
            {
                // Invoke our commencing event to let all models know we're about to start.
                Commencing?.Invoke(this, new EventArgs());

                // Begin running the simulation.
                DoCommence?.Invoke(this, new CommenceArgs() { CancelToken = cancelToken });
            }
            catch (Exception err)
            {
                // Exception occurred. Write error to summary.
                simulationError = new SimulationException("", err, Name, FileName);
                summary?.WriteMessage(this, simulationError.ToString(), Models.Core.MessageType.Error);

                // Rethrow exception
                throw simulationError;
            }
            finally
            {
                try
                {
                    // Signal that the simulation is complete.
                    Completed?.Invoke(this, new EventArgs());
                    IsRunning = false;
                }
                catch (Exception error)
                {
                    // If an exception was thrown at this point
                    Exception cleanupError = new SimulationException($"Error while performing simulation cleanup", error, Name, FileName);
                    if (simulationError == null)
                        throw cleanupError;
                    throw new AggregateException(simulationError, cleanupError);
                }
            }
        }

        /// <summary>
        /// Cleanup the simulation after the run.
        /// </summary>
        public void Cleanup(System.Threading.CancellationTokenSource cancelToken)
        {
            UnsubscribeFromEvents?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Remove all disabled child models from the specified model.
        /// </summary>
        /// <param name="model"></param>
        private void RemoveDisabledModels(IModel model)
        {
            foreach (var disabledChild in model.Node.Walk().Where(n => !n.Model.Enabled).ToArray())
            {
                disabledChild.Parent?.RemoveChild(disabledChild.Model);
            }
        }

        /// <summary>
        /// Check that there aren't multiple soil water models in a zone.
        /// </summary>
        /// <param name="parentZone">The zone to check.</param>
        private static void CheckNotMultipleSoilWaterModels(IModel parentZone)
        {
            foreach (var soil in parentZone.Node.FindChildren<Soils.Soil>())
                if (soil.Node.FindChildren<Models.Interfaces.ISoilWater>().Where(c => (c as IModel).Enabled).Count() > 1)
                    throw new Exception($"More than one water balance found in zone {parentZone.Name}");

            // Check to make sure there is only one ISoilWater in each zone.
            foreach (IModel zone in parentZone.Node.FindChildren<Models.Interfaces.IZone>())
                CheckNotMultipleSoilWaterModels(zone);
        }

        /// <summary>Store descriptors in DataStore.</summary>
        private void StoreFactorsInDataStore()
        {
            IEnumerable<IDataStore> ss = ModelServices.OfType<IDataStore>();
            IDataStore storage = null;
            if (ss != null && ss.Count() > 0)
                storage = ss.First();

            if (storage != null && Descriptors != null)
            {
                var table = new DataTable("_Factors");
                table.Columns.Add("ExperimentName", typeof(string));
                table.Columns.Add("SimulationName", typeof(string));
                table.Columns.Add("FolderName", typeof(string));
                table.Columns.Add("FactorName", typeof(string));
                table.Columns.Add("FactorValue", typeof(string));

                var experimentDescriptor = Descriptors.Find(d => d.Name == "Experiment");
                var simulationDescriptor = Descriptors.Find(d => d.Name == "SimulationName");
                var folderDescriptor = Descriptors.Find(d => d.Name == "FolderName");

                foreach (var descriptor in Descriptors)
                {
                    if (descriptor.Name != "Experiment" &&
                        descriptor.Name != "SimulationName" &&
                        descriptor.Name != "FolderName" &&
                        descriptor.Name != "Zone")
                    {
                        var row = table.NewRow();
                        if (experimentDescriptor != null)
                            row[0] = experimentDescriptor.Value;
                        if (simulationDescriptor != null)
                            row[1] = simulationDescriptor.Value;
                        if (folderDescriptor != null)
                            row[2] = folderDescriptor.Value;
                        row[3] = descriptor.Name;
                        row[4] = descriptor.Value;
                        table.Rows.Add(row);
                    }
                }

                // Report tables are automatically cleaned before the simulation is run,
                // as an optimisation specifically designed for this call to WriteTable().
                // Therefore, we do not need to delete existing data here.
                storage.Writer.WriteTable(table, false);
            }
        }

    }
}