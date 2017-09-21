using System.IO;
using System.Xml;
using Models.Core;
using System.Xml.Serialization;
using System;
using System.Reflection;
using System.Collections.Generic;
using Models.Factorial;
using APSIM.Shared.Utilities;
using System.Linq;
using Models.Core.Interfaces;
using Models.Core.Runners;

namespace Models.Core
{
    /// <summary>
    /// Encapsulates a collection of simulations. It is responsible for creating this collection,
    /// changing the structure of the components within the simulations, renaming components, adding
    /// new ones, deleting components. The user interface talks to an instance of this class.
    /// </summary>
    [Serializable]
    [ScopedModel]
    public class Simulations : Model, ISimulationEngine
    {
        /// <summary>The _ file name</summary>
        private string _FileName;

        private Links links;

        /// <summary>Gets or sets the width of the explorer.</summary>
        /// <value>The width of the explorer.</value>
        public Int32 ExplorerWidth { get; set; }

        /// <summary>Gets or sets the version.</summary>
        [XmlAttribute("Version")]
        public int Version { get; set; }

        /// <summary>The name of the file containing the simulations.</summary>
        /// <value>The name of the file.</value>
        [XmlIgnore]
        public string FileName
        {
            get
            {
                return _FileName;
            }
            set
            {
                _FileName = value;
            }
        }

        /// <summary>
        /// A list of all exceptions thrown during the creation and loading of the simulation.
        /// </summary>
        /// <value>The load errors.</value>
        [XmlIgnore]
        public List<Exception> LoadErrors { get; private set; }

        /// <summary>Returns an instance of a links service</summary>
        [XmlIgnore]
        public Links Links
        {
            get
            {
                if (links == null)
                    CreateLinks();
                return links;
            }
        }

        /// <summary>Returns an instance of an events service</summary>
        /// <param name="model">The model the service is for</param>
        public IEvent GetEventService(IModel model)
        {
            return new Events(model);
        }

        /// <summary>Returns an instance of an locator service</summary>
        /// <param name="model">The model the service is for</param>
        public ILocator GetLocatorService(IModel model)
        {
            return new Locator(model);
        }

        /// <summary>Constructor</summary>
        private Simulations()
        {
            Version = APSIMFileConverter.LastestVersion;
        }

        /// <summary>
        /// Create a simulations model
        /// </summary>
        /// <param name="children">The child models</param>
        public static Simulations Create(IEnumerable<Model> children)
        {
            Simulations newSimulations = new Core.Simulations();
            newSimulations.Children.AddRange(children);

            // Call the OnDeserialised method in each model.
            Events events = new Core.Events(newSimulations);
            object[] args = new object[] { true };
            events.Publish("Deserialised", args);

            // Parent all models.
            newSimulations.Parent = null;
            Apsim.ParentAllChildren(newSimulations);

            // Call OnLoaded in all models.
            newSimulations.LoadErrors = events.Publish("Loaded", null);
            return newSimulations;
        }

        /// <summary>Create a simulations object by reading the specified filename</summary>
        /// <param name="FileName">Name of the file.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Simulations.Read() failed. Invalid simulation file.\n</exception>
        public static Simulations Read(string FileName)
        {
            // Run the converter.
            APSIMFileConverter.ConvertToLatestVersion(FileName);

            // Deserialise
            Simulations simulations = XmlUtilities.Deserialise(FileName, Assembly.GetExecutingAssembly()) as Simulations;

            if (simulations != null)
            {
                // Set the filename
                simulations.FileName = FileName;
                simulations.SetFileNameInAllSimulations();

                // Call the OnDeserialised method in each model.
                Events events = new Core.Events(simulations);
                object[] args = new object[] { true };
                events.Publish("Deserialised", args);

                // Parent all models.
                simulations.Parent = null;
                Apsim.ParentAllChildren(simulations);

                // Call OnLoaded in all models.
                simulations.LoadErrors = events.Publish("Loaded", null);
            }

            return simulations;
        }

        /// <summary>Create a simulations object by reading the specified filename</summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Simulations.Read() failed. Invalid simulation file.\n</exception>
        public static Simulations Read(XmlNode node)
        {
            // Run the converter.
            APSIMFileConverter.ConvertToLatestVersion(node, null);

            // Deserialise
            Simulations simulations = XmlUtilities.Deserialise(node, Assembly.GetExecutingAssembly()) as Simulations;

            if (simulations != null)
            {
                // Set the filename
                simulations.SetFileNameInAllSimulations();

                // Call the OnSerialised method in each model.
                object[] args = new object[] { true };
                Events events = new Events(simulations);
                events.Publish("Deserialised", args);

                // Parent all models.
                simulations.Parent = null;
                Apsim.ParentAllChildren(simulations);

                events.Publish("Loaded", null);
            }
            else
                throw new Exception("Simulations.Read() failed. Invalid simulation file.\n");
            return simulations;
        }

        /// <summary>Run a simulation</summary>
        /// <param name="simulation">The simulation to run</param>
        /// <param name="doClone">Clone the simulation before running?</param>
        public void Run(Simulation simulation, bool doClone)
        {
            Apsim.ParentAllChildren(simulation);
            RunSimulation simulationRunner = new RunSimulation(simulation, this, doClone);
            simulationRunner.Run(null, null);
        }

        /// <summary>Make model substitutions if necessary.</summary>
        /// <param name="model">The model to make substitutions in.</param>
        public void MakeSubstitutions(IModel model)
        {
            IModel replacements = Apsim.Child(this, "Replacements");
            if (replacements != null)
            {
                foreach (IModel replacement in replacements.Children)
                {
                    foreach (IModel match in Apsim.FindAll(model))
                    {
                        if (!(match is Simulation) && match.Name.Equals(replacement.Name, StringComparison.InvariantCultureIgnoreCase))
                        {
                            // Do replacement.
                            IModel newModel = Apsim.Clone(replacement);
                            int index = match.Parent.Children.IndexOf(match as Model);
                            match.Parent.Children.Insert(index, newModel as Model);
                            newModel.Parent = match.Parent;
                            match.Parent.Children.Remove(match as Model);
                            Events events = new Events(newModel);
                            events.Publish("Loaded", null);
                        }
                    }
                }
            }
        }

        /// <summary>Write the specified simulation set to the specified filename</summary>
        /// <param name="FileName">Name of the file.</param>
        public void Write(string FileName)
        {
            string tempFileName = Path.Combine(Path.GetTempPath(), Path.GetFileName(FileName));
            StreamWriter Out = new StreamWriter(tempFileName);
            Write(Out);
            Out.Close();

            // If we get this far without an exception then copy the tempfilename over our filename,
            // creating a backup (.bak) in the process.
            string bakFileName = FileName + ".bak";
            File.Delete(bakFileName);
            if (File.Exists(FileName))
                File.Move(FileName, bakFileName);
            File.Move(tempFileName, FileName);
            this.FileName = FileName;
            SetFileNameInAllSimulations();
        }

        /// <summary>Write the specified simulation set to the specified 'stream'</summary>
        /// <param name="stream">The stream.</param>
        public void Write(TextWriter stream)
        {
            object[] args = new object[] { true };

            Events events = new Events(this);
            events.Publish("Serialising", args);

            try
            {
                stream.Write(XmlUtilities.Serialise(this, true));
            }
            finally
            {
                events.Publish("Serialised", args);
            }
        }

        /// <summary>Find all simulation names that are going to be run.</summary>
        /// <returns></returns>
        public string[] FindAllSimulationNames()
        {
            List<string> simulations = new List<string>();
            // Look for simulations.
            foreach (Model Model in Apsim.ChildrenRecursively(this))
            {
                if (Model is Simulation)
                {
                    // An experiment can have a base simulation - don't return that to caller.
                    if (!(Model.Parent is Experiment))
                        simulations.Add(Model.Name);
                }
            }

            // Look for experiments and get them to create their simulations.
            foreach (Model experiment in Apsim.ChildrenRecursively(this))
            {
                if (experiment is Experiment)
                    simulations.AddRange((experiment as Experiment).Names());
            }

            return simulations.ToArray();

        }

        /// <summary>Find and return a list of duplicate simulation names.</summary>
        public List<string> FindDuplicateSimulationNames()
        {
            List<IModel> allSims = Apsim.ChildrenRecursively(this, typeof(Simulation));
            List<string> allSimNames = allSims.Select(s => s.Name).ToList();
            var duplicates = allSimNames
                .GroupBy(i => i)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
            return duplicates.ToList();
        }

        /// <summary>Look through all models. For each simulation found set the filename.</summary>
        private void SetFileNameInAllSimulations()
        {
            foreach (Model simulation in Apsim.ChildrenRecursively(this))
                if (simulation is Simulation)
                    (simulation as Simulation).FileName = FileName;
        }

        /// <summary>Create a links object</summary>
        private void CreateLinks()
        {
            List<object> services = new List<object>();
            IStorageReader storage = Apsim.Find(this, typeof(IStorageReader)) as IStorageReader;
            if (storage != null)
                services.Add(storage);
            links = new Links(services);
        }

        /// <summary>
        /// A cleanup routine to be used when we close this set of simulations
        /// The goal is to avoid cyclic references that can prevent the garbage collector
        /// from clearing the memory we have used
        /// </summary>
        public void ClearSimulationReferences()
        {
            // Clears the locator caches for our Simulations.
            // These caches may result in cyclic references and memory leaks if not cleared
            foreach (Model simulation in Apsim.ChildrenRecursively(this))
                if (simulation is Simulation)
                    (simulation as Simulation).ClearCaches();
            // Explicitly clear the child lists
            ClearChildLists();
        }

        /// <summary>Documents the specified model.</summary>
        /// <param name="modelNameToDocument">The model name to document.</param>
        /// <param name="tags">The auto doc tags.</param>
        /// <param name="headingLevel">The starting heading level.</param>
        public void DocumentModel(string modelNameToDocument, List<AutoDocumentation.ITag> tags, int headingLevel)
        {
            Simulation simulation = Apsim.Find(this, typeof(Simulation)) as Simulation;
            if (simulation != null)
            {
                // Find the model of the right name.
                IModel modelToDocument = Apsim.Find(simulation, modelNameToDocument);

                // If not found then find a model of the specified type.
                if (modelToDocument == null)
                    modelToDocument = Apsim.Get(simulation, "[" + modelNameToDocument + "]") as IModel;

                // If the simulation has the same name as the model we want to document, dig a bit deeper
                if (modelToDocument == simulation)
                    modelToDocument = Apsim.ChildrenRecursivelyVisible(simulation).FirstOrDefault(m => m.Name.Equals(modelNameToDocument, StringComparison.OrdinalIgnoreCase));

                // If still not found throw an error.
                if (modelToDocument != null)
                {
                    // Get the path of the model (relative to parentSimulation) to document so that 
                    // when replacements happen below we will point to the replacement model not the 
                    // one passed into this method.
                    string pathOfSimulation = Apsim.FullPath(simulation) + ".";
                    string pathOfModelToDocument = Apsim.FullPath(modelToDocument).Replace(pathOfSimulation, "");

                    // Clone the simulation
                    Simulation clonedSimulation = Apsim.Clone(simulation) as Simulation;

                    // Make any substitutions.
                    MakeSubstitutions(clonedSimulation);

                    // Now use the path to get the model we want to document.
                    modelToDocument = Apsim.Get(clonedSimulation, pathOfModelToDocument) as IModel;

                    if (modelToDocument == null)
                        throw new Exception("Cannot find model to document: " + modelNameToDocument);

                    // resolve all links in cloned simulation.
                    Links.Resolve(clonedSimulation);

                    modelToDocument.IncludeInDocumentation = true;
                    foreach (IModel child in Apsim.ChildrenRecursively(modelToDocument))
                        child.IncludeInDocumentation = true;

                    // Document the model.
                    modelToDocument.Document(tags, headingLevel, 0);

                    // Unresolve links.
                    Links.Unresolve(clonedSimulation);
                }
            }
        }
    }
}
