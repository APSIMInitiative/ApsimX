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
    public class Manager : Model//, ISerializable
    {
        // ----------------- Privates
        private string _Code;
        private bool HasDeserialised = false;
        private string elementsAsXml = null;
        [NonSerialized] private Model _Script;
        [NonSerialized] private XmlElement[] _elements;

        // ----------------- Links
        [Link] private ISummary Summary = null;

        // ----------------- Parameters (XML serialisation)
        [XmlAnyElement]
        public XmlElement[] elements { get { return _elements; } set { _elements = value; } }

        [XmlText]
        [XmlElement("Code")]
        public XmlNode[] CodeCData
        {
            get
            {
                XmlDocument dummy = new XmlDocument();
                return new XmlNode[] { dummy.CreateCDataSection(Code) };
            }
            set
            {
                if (value == null)
                {
                    Code = null;
                    return;
                }

                if (value.Length != 1)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid array length {0}", value.Length));
                }

                Code = value[0].Value;
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
        /// The model has been loaded.
        /// </summary>
        [EventSubscribe("Initialised")]
        private void OnInitialised(object sender, EventArgs e)
        {
            HasDeserialised = true;
            RebuildScriptModel();
        }

        /// <summary>
        /// Rebuild the script model.
        /// </summary>
        public void RebuildScriptModel()
        {
            if (HasDeserialised)
            {
                // If a script model already exists, then serialise it so that we capture the current state,
                // and then create a new script model using those values.
                string currentState = null;
                if (Script != null)
                {
                    XmlSerializer oldSerial = new XmlSerializer(Script.GetType());
                    currentState = Utility.Xml.Serialise(Script, true);
                    
                    RemoveModel(Script);
                }

                try
                {
                    // Try compiling the script
                    //string assemblyFileName = Path.Combine(Path.GetDirectoryName(Simulations.FileName),
                    //                                       Name) + ".dll";
                    Assembly CompiledAssembly = Utility.Reflection.CompileTextToAssembly(Code, null);
                    Summary.WriteMessage(FullPath, "Script compiled ok");

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
                        AddModel(Script);


                    // Call the OnInitialised if present.
                    MethodInfo OnInitialised = Script.GetType().GetMethod("OnInitialised", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (OnInitialised != null)
                        OnInitialised.Invoke(Script, new object[] { this, null });
                }
                catch (Exception err)
                {
                    Summary.WriteWarning(FullPath, err.Message);
                }
            }
        }

        //private void AddScriptModel(Model Script)
        //{
        //    Script.Parent = this;
        //    Utility.ModelFunctions.ConnectEventsInModel(Script);
        //    Utility.ModelFunctions.ResolveLinks(Script);
        //}

        //private void RemoveScriptModel(Model Script)
        //{
        //    Script.Parent = null;
        //    Utility.ModelFunctions.DisconnectEventsInModel(Script);
        //}

        /// <summary>
        /// Default constructor
        /// </summary>
        //public Manager() { }

        ///// <summary>
        ///// Constructor called by BinaryFormatter.Deserialize
        ///// </summary>
        //protected Manager(SerializationInfo info, StreamingContext context)
        //{
        //    _Code = info.GetString("Code");
        //    elementsAsXml = info.GetString("ScriptXML");
        //}

        ///// <summary>
        ///// Method called by BinaryFormatter.Serialize
        ///// </summary>
        //public void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    if (Script != null)
        //        Utility.ModelFunctions.DisconnectEventsInModel(Script);
        //    info.AddValue("Code", _Code);
        //    info.AddValue("ScriptXML", elementsAsXml);
        //}
    }
}