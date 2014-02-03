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
    public class Manager : Model 
    {
        // ----------------- Privates
        private string _Code;
        private bool HasDeserialised = false;
        private string elementsAsXml = null;
        private Model _Script;
        [NonSerialized] private XmlElement[] _elements;

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

                // Try compiling the script
                //string assemblyFileName = Path.Combine(Path.GetDirectoryName(Simulations.FileName),
                //                                       Name) + ".dll";
                Assembly CompiledAssembly = Utility.Reflection.CompileTextToAssembly(Code, null);

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
                Script.OnLoaded();
            }
            return null;
        }
    }
}