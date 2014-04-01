using System;
using System.Xml;
using System.Reflection;
using System.Xml.Serialization;
using Models.Core;
using System.Xml.Schema;
using System.Runtime.Serialization;
using System.IO;

namespace Models
{
    [Serializable]
    [ViewName("UserInterface.Views.ManagerView")]
    [PresenterName("UserInterface.Presenters.ManagerPresenter")]
    public class Manager : ModelCollection
    {
        // ----------------- Privates
        private string _Code;
        private bool HasDeserialised = false;
        private string elementsAsXml = null;
        [NonSerialized]
        private Model _Script;
        [NonSerialized]
        private Utility.TempFileNames TemporaryFiles = null;
        [NonSerialized]
        private XmlElement[] _elements;
        // store the details of the compilation for this instance
        [NonSerialized]
        private string AssemblyFile = "";
        [NonSerialized]
        private Assembly CompiledAssembly = null;
        [NonSerialized]
        private string CompiledCode = "";
        [Link]
        Simulation Simulation = null;

        // ----------------- Parameters (XML serialisation)
        [XmlAnyElement]
        public XmlElement[] elements { get { return _elements; } set { _elements = value; } }

        [XmlElement("Code")]
        public XmlNode CodeCData
        {
            get
            {
                XmlDocument dummy = new XmlDocument();
                return dummy.CreateCDataSection(Code);
            }
            set
            {
                if (value == null)
                {
                    Code = null;
                    return;
                }

                Code = value.Value;
            }
        }

        // ----------------- Outputs
        [XmlIgnore]
        public Model Script { get { return _Script; } set { _Script = value; } }

        [XmlIgnore]
        public string Code
        {
            get
            {
                return _Code;
            }
            set
            {
                _Code = value;
                RebuildScriptModel();
            }
        }

        /// <summary>
        /// Called just before a simulation commences.
        /// </summary>
        public override void OnCommencing() {
            Script.OnCommencing();
        }

        /// <summary>
        /// Called just after a simulation has completed.
        /// </summary>
        public override void OnCompleted() {
            Script.OnCompleted();
        }

        /// <summary>
        /// The model has been loaded.
        /// </summary>
        public override void OnLoaded()
        {
            HasDeserialised = true;
            if (Script == null)
                RebuildScriptModel();
        }

        /// <summary>
        /// Rebuild the script model and return error message if script cannot be compiled.
        /// </summary>
        public string RebuildScriptModel()
        {
            if (HasDeserialised && Simulation != null)
            {
                // If a script model already exists, then serialise it so that we capture the current state,
                // and then create a new script model using those values.
                string currentState = null;
                if (Script != null)
                {
                    XmlSerializer oldSerial = new XmlSerializer(Script.GetType());
                    currentState = Utility.Xml.Serialise(Script, true);

                    Model.UnresolveLinks(Script);
                    Model.DisconnectEvents(Script);
                    Model.DisconnectSubscriptions(Script);
                    Script.Parent = null;
                    Script = null;
                }
                
                // determine if the script needs to be recompiled
                if (TemporaryFiles == null)
                    TemporaryFiles = new Utility.TempFileNames(Simulation.FileName, this, ".dll");

                if ((CompiledAssembly == null) || (String.Compare(_Code, CompiledCode) != 0))
                {
                    CompiledAssembly = Utility.Reflection.CompileTextToAssembly(Code, TemporaryFiles.GetUniqueFileName());
                    CompiledCode = _Code;
                }

                // Look for a "class Script" - throw if not found.
                Type ScriptType = CompiledAssembly.GetType("Models.Script");
                if (ScriptType == null)
                    throw new ApsimXException(FullPath, "Cannot find a public class called 'Script'");

                // If a script model already exists, then serialise it so that we capture the current state,
                // and then create a new script model using those values.
                if (currentState != null)
                {
                    XmlSerializer newSerial = new XmlSerializer(ScriptType);
                    Script = newSerial.Deserialize(new StringReader(currentState)) as Model;
                }

                // If this manager model has been XML deserialised, then use the "elements" member to 
                // create a new script model.
                else if (elements != null && elements.Length > 0)
                {
                    XmlSerializer serial = new XmlSerializer(ScriptType);
                    Script = serial.Deserialize(new XmlNodeReader(elements[0])) as Model;

                    // setup the elementsAsXml for BinaryFormatter.Serialization.
                    elementsAsXml = elements[0].OuterXml;
                }

                // If this manager model has been binary deserialised, the use the "elementsAsXml" member
                // to create a new script model.
                else if (elementsAsXml != "" && elementsAsXml != null)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(elementsAsXml);
                    XmlSerializer serial = new XmlSerializer(ScriptType);
                    Script = serial.Deserialize(new XmlNodeReader(doc.DocumentElement)) as Model;
                }

                // Nothing else was specified so just create a default script model.
                else
                    Script = Activator.CreateInstance(ScriptType) as Model;

                if (Script != null)
                {
                    Script.Parent = this;
                    // Need to reconnect all events and links in all models in simulation because
                    // some may want to connect or link to this new script.
                    Model.ConnectEventPublishers(Script);
                    Model.ConnectEventSubscribers(Script);
                    Model.ResolveLinks(Script);
                }


                // Call the OnInitialised if present.
                Script.OnLoaded();
            }
            return null;
        }



        /// <summary>
        /// Return a unique temporary .dll filename that is predictable so that we can
        /// remove .dlls from previous runs.
        /// </summary>
        private string UniqueTempFileNameBase
        {
            get
            {
                string fileName = Simulation.FileName + FullPath;
                fileName = fileName.Replace(@"/", "");
                fileName = fileName.Replace(@"\", "");
                fileName = fileName.Replace(@".", "");
                fileName = fileName.Replace(@":", "");
                return Path.Combine(Path.GetTempPath(), fileName);
            }
        }

        /// <summary>
        /// Get rid of the temporary .dll file.
        /// </summary>
        private void CleanupPreviousBuilds()
        {
            string baseFileName = UniqueTempFileNameBase;
            int counter = 1;
            bool finished = false;
            do
            {
                string tempFileName = baseFileName + counter.ToString() + ".dll";
                if (File.Exists(tempFileName))
                    File.Delete(tempFileName);
                else
                    finished = true;
                counter++;
            }
            while (!finished);
        }

        /// <summary>
        /// Return a unique filename that is specific to this manager module.
        /// </summary>
        /// <returns></returns>
        private string GetUniqueAssemblyFileName()
        {
            string baseFileName = UniqueTempFileNameBase;
            int counter = 1;
            do
            {
                string tempFileName = baseFileName + counter.ToString() + ".dll";
                if (!File.Exists(tempFileName))
                    return tempFileName;
                counter++;
            }
            while (counter < 1000);
            throw new ApsimXException(FullPath, "Cannot create a unique, temporary filename while trying to compile script code");
        }


    }
}