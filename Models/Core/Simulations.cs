﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core.ApsimFile;
using Models.Core.Interfaces;
using Models.Storage;
using Newtonsoft.Json;

namespace Models.Core
{
    /// <summary>
    /// Encapsulates a collection of simulations. It is responsible for creating this collection, changing the structure of the components within the simulations, renaming components, adding new ones, deleting components. The user interface talks to an instance of this class.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.MarkdownView")]
    [PresenterName("UserInterface.Presenters.GenericPresenter")]
    public class Simulations : Model, ISimulationEngine, IScopedModel, IScopeDependency
    {
        /// <summary>Scope supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IScope Scope { private get; set; }

        [NonSerialized]
        private Links links;

        /// <summary>Gets or sets the version.</summary>
        [System.Xml.Serialization.XmlAttribute("Version")]
        public int Version { get; set; }

        /// <summary>The name of the file containing the simulations.</summary>
        /// <value>The name of the file.</value>
        [JsonIgnore]
        public string FileName { get; set; }

        /// <summary>Returns an instance of a links service</summary>
        [JsonIgnore]
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

        /// <summary>Constructor</summary>
        public Simulations()
        {
            Version = FileFormat.JSONVersion;
        }

        /// <summary>
        /// Return the current APSIM version number.
        /// </summary>
        public static string ApsimVersion
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
        /// Initialise model.
        /// </summary>
        public override void OnCreated()
        {
            base.OnCreated();
            FileName = Node.FileName;
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
            DataStore storage = Scope.Find<DataStore>();
            if (storage != null)
            {
                storage.Writer.AddCheckpoint(checkpointName, filesReferenced);
                storage.Reader.Refresh();
            }
        }

        /// <summary>Write the specified simulation set to the specified filename</summary>
        /// <param name="FileName">Name of the file.</param>
        public void Write(string FileName)
        {
            string tempFileName = Path.GetTempFileName();
            File.WriteAllText(tempFileName, Node.ToJSONString());

            // If we get this far without an exception then copy the tempfilename over our filename,
            // creating a backup (.bak) in the process.
            string bakFileName = FileName + ".bak";
            File.Delete(bakFileName);
            if (File.Exists(FileName))
                File.Move(FileName, bakFileName);
            File.Move(tempFileName, FileName);
            this.FileName = FileName;
            Node.FileName = FileName;
            SetFileNameInAllSimulations();
        }

        /// <summary>Write the specified simulation set to the specified directory path.</summary>
        /// <param name="currentFileName">FileName property of the simulation set.</param>
        /// <param name="savePath">The location where the simulation should be saved.</param>
        public void Write(string currentFileName, string savePath) // TODO: needs testing in conjunction with Main.cs --apply switch.
        {
            try
            {
                string tempFileName = Path.GetTempFileName();
                File.WriteAllText(tempFileName, Node.ToJSONString());

                // If we get this far without an exception then copy the tempfilename over our filename,
                // creating a backup (.bak) in the process.
                string bakFileName = currentFileName + ".bak";
                File.Delete(bakFileName);
                if (File.Exists(currentFileName))
                    File.Move(currentFileName, bakFileName);
                File.Move(tempFileName, currentFileName);
                File.Move(currentFileName, savePath, true);
                this.FileName = savePath;
                SetFileNameInAllSimulations();
            }
            catch (Exception e)
            {
                throw new Exception($"An error occured trying to save a simulation to {savePath}. {e}");
            }

        }

        /// <summary>
        /// Resets the FileName property of each Simulation model in the APSIMX file.
        /// </summary>
        public void ResetSimulationFileNames()
        {
            SetFileNameInAllSimulations();
        }

        /// <summary>Look through all models. For each simulation found set the filename.</summary>
        private void SetFileNameInAllSimulations()
        {
            foreach (Model child in this.FindAllDescendants().ToList())
            {
                if (child is Simulation)
                {
                    (child as Simulation).FileName = FileName;
                    (child as Simulation).Node.FileName = FileName;
                }
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
            var storage = Scope.Find<IDataStore>();
            if (storage != null)
                services.Add(storage);
            return services;
        }

        /// <summary>Create a links object</summary>
        private void CreateLinks()
        {
            if (links == null)
                links = new Links(GetServices());
        }

        /// <summary>
        /// A cleanup routine to be used when we close this set of simulations
        /// The goal is to avoid cyclic references that can prevent the garbage collector
        /// from clearing the memory we have used
        /// </summary>
        public void ClearSimulationReferences()
        {
            // Explicitly clear the child lists
            ClearChildLists();
        }

        /// <summary>Find all referenced files from all models.</summary>
        public IEnumerable<string> FindAllReferencedFiles(bool isAbsolute = true)
        {
            SortedSet<string> fileNames = new SortedSet<string>();
            foreach (IReferenceExternalFiles model in this.FindAllDescendants<IReferenceExternalFiles>().Where(m => m.Enabled))
                foreach (string fileName in model.GetReferencedFileNames())
                    if (isAbsolute == true)
                    {
                        fileNames.Add(PathUtilities.GetAbsolutePath(fileName, FileName));
                    }
                    else
                    {
                        fileNames.Add(fileName);
                    }
            return fileNames;
        }
    }
}
