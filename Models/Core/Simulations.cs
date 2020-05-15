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
using Models.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Models.Core.ApsimFile;
using Models.Core.Run;

namespace Models.Core
{
    /// <summary>
    /// # [Name]
    /// Encapsulates a collection of simulations. It is responsible for creating this collection,
    /// changing the structure of the components within the simulations, renaming components, adding
    /// new ones, deleting components. The user interface talks to an instance of this class.
    /// </summary>
    [Serializable]
    [ScopedModel]
    [ViewName("UserInterface.Views.HTMLView")]
    [PresenterName("UserInterface.Presenters.GenericPresenter")]
    public class Simulations : Model, ISimulationEngine
    {
        [NonSerialized]
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
        public string FileName { get; set; }

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
        public Simulations()
        {
            Version = ApsimFile.Converter.LatestVersion;
        }

        /// <summary>
        /// Create a simulations model
        /// </summary>
        /// <param name="children">The child models</param>
        public static Simulations Create(IEnumerable<IModel> children)
        {
            Simulations newSimulations = new Core.Simulations();
            newSimulations.Children.AddRange(children.Cast<Model>());

            // Parent all models.
            newSimulations.Parent = null;
            Apsim.ParentAllChildren(newSimulations);

            // Call OnCreated in all models.
            Apsim.ChildrenRecursively(newSimulations).ForEach(m => m.OnCreated());

            return newSimulations;
        }

        /// <summary>
        /// Return the current APSIM version number.
        /// </summary>
        public string ApsimVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            set
            {
                // Setter is provided so that this property gets serialized.
            }
        }

        /// <summary>
        /// Return the current APSIM version number.
        /// </summary>
        public static string GetApsimVersion()
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            FileInfo info = new FileInfo(Assembly.GetExecutingAssembly().Location);
            string buildDate = info.LastWriteTime.ToString("yyyy-MM-dd");
            return "Version " + version + ", built " + buildDate;
        }

        /// <summary>
        /// Checkpoint the simulation.
        /// </summary>
        /// <param name="checkpointName">Name of checkpoint</param>
        public void AddCheckpoint(string checkpointName)
        {
            List<string> filesReferenced = new List<string>();
            filesReferenced.Add(FileName);
            filesReferenced.AddRange(FindAllReferencedFiles());
            DataStore storage = Apsim.Find(this, typeof(DataStore)) as DataStore;
            if (storage != null)
            {
                storage.Writer.AddCheckpoint(checkpointName, filesReferenced);
                storage.Reader.Refresh();
            }
        }

        /// <summary>
        /// Revert this object to a previous one.
        /// </summary>
        /// <param name="checkpointName">Name of checkpoint</param>
        /// <returns>A new simulations object that represents the file on disk</returns>
        public Simulations RevertCheckpoint(string checkpointName)
        {
            IDataStore storage = Apsim.Find(this, typeof(DataStore)) as DataStore;
            if (storage != null)
            {
                storage.Writer.RevertCheckpoint(checkpointName);
                storage.Reader.Refresh();
            }
            List<Exception> creationExceptions = new List<Exception>();
            return FileFormat.ReadFromFile<Simulations>(FileName, out creationExceptions);
        }

        /// <summary>Write the specified simulation set to the specified filename</summary>
        /// <param name="FileName">Name of the file.</param>
        public void Write(string FileName)
        {
            string tempFileName = Path.GetTempFileName();
            File.WriteAllText(tempFileName, FileFormat.WriteToString(this));

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

        /// <summary>Look through all models. For each simulation found set the filename.</summary>
        private void SetFileNameInAllSimulations()
        {
            foreach (Model child in Apsim.ChildrenRecursively(this))
            {
                if (child is Simulation)
                    (child as Simulation).FileName = FileName;
                else if (child is DataStore)
                {
                    DataStore storage = child as DataStore;
                    storage.Close();
                    storage.UpdateFileName();
                    storage.Open();
                }
            }
        }

        /// <summary>
        /// Nulls the link object, which will force it to be recreated when it's needed
        /// </summary>
        public void ClearLinks()
        {
            links = null;
        }
        
        /// <summary>
        /// Gets the services objects.
        /// </summary>
        public List<object> GetServices()
        {
            List<object> services = new List<object>();
            var storage = Apsim.Find(this, typeof(IDataStore)) as IDataStore;
            if (storage != null)
                services.Add(storage);
            services.Add(this);
            return services;
        }

        /// <summary>Create a links object</summary>
        private void CreateLinks()
        {
            links = new Links(GetServices());
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

        /// <summary>Find all referenced files from all models.</summary>
        public IEnumerable<string> FindAllReferencedFiles()
        {
            SortedSet<string> fileNames = new SortedSet<string>();
            foreach (IReferenceExternalFiles model in Apsim.ChildrenRecursively(this, typeof(IReferenceExternalFiles)))
                foreach (string fileName in model.GetReferencedFileNames())
                    fileNames.Add(PathUtilities.GetAbsolutePath(fileName, FileName));
            
            return fileNames;
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
                    SimulationDescription simDescription = new SimulationDescription(simulation);

                    Simulation clonedSimulation = simDescription.ToSimulation();

                    // Now use the path to get the model we want to document.
                    modelToDocument = Apsim.Get(clonedSimulation, pathOfModelToDocument) as IModel;

                    if (modelToDocument == null)
                        throw new Exception("Cannot find model to document: " + modelNameToDocument);

                    // resolve all links in cloned simulation.
                    Links.Resolve(clonedSimulation, true);

                    modelToDocument.IncludeInDocumentation = true;
                    foreach (IModel child in Apsim.ChildrenRecursively(modelToDocument))
                        child.IncludeInDocumentation = true;

                    // Document the model.
                    AutoDocumentation.DocumentModel(modelToDocument, tags, headingLevel, 0, documentAllChildren:true);

                    // Unresolve links.
                    Links.Unresolve(clonedSimulation, true);
                }
            }
        }

    }
}
