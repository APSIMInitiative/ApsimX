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

        [NonSerialized] private Model _Script;
        [NonSerialized] private XmlElement[] _elements;

        [NonSerialized] private string CompiledCode = "";
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
        /// The model has been loaded.
        /// </summary>
        public override void OnLoaded()
        {
            HasDeserialised = true;
            if (Script == null)
                RebuildScriptModel();
        }

        /// <summary>
        /// We're about to be serialised. Remove our 'Script' model from the list
        /// of all models so that is isn't serialised. Seems .NET has a problem
        /// with serialising objects that have been compiled dynamically.
        /// </summary>
        public override void OnSerialising(bool xmlSerialisation)
        {
            if (Script != null)
                RemoveModel(Script);
        }

        /// <summary>
        /// Serialisation has completed. Readd our 'Script' model if necessary.
        /// </summary>
        public override void OnSerialised(bool xmlSerialisation)
        {
            if (Script != null)
                AddModel(Script);
        }

        /// <summary>
        /// Rebuild the script model and return error message if script cannot be compiled.
        /// </summary>
        public void RebuildScriptModel()
        {
            if (HasDeserialised && Simulation != null)
            {
                // Capture the current values of all parameters.
                EnsureParametersAreCurrent();

                if (NeedToCompileCode())
                {
                    // If a script model already exists, then get rid of it.
                    if (Script != null)
                    {
                        RemoveModel(Script);
                        Script = null;
                    }

                    // Compile the code.
                    Assembly compiledAssembly = Utility.Reflection.CompileTextToAssembly(Code, null);
                    CompiledCode = _Code;

                    // Get the script 'Type' from the compiled assembly.
                    Type scriptType = compiledAssembly.GetType("Models.Script");
                    if (scriptType == null)
                        throw new ApsimXException(FullPath, "Cannot find a public class called 'Script'");

                    // Create a new script model.
                    XmlSerializer newSerial = new XmlSerializer(scriptType);
                    Script = newSerial.Deserialize(new StringReader(elementsAsXml)) as Model;

                    // Add the new script model to our models collection.
                    AddModel(Script);
                    Script.HiddenModel = true;
                }
            }
        }

        /// <summary>
        /// Return true if the code needs to be recompiled.
        /// </summary>
        private bool NeedToCompileCode()
        {
            return (Script == null || _Code != CompiledCode);
        }

        /// <summary>
        /// Ensures the parameters are up to date and reflect the current 'Script' model.
        /// </summary>
        private void EnsureParametersAreCurrent()
        {
            if (Script != null)
            {
                XmlSerializer oldSerial = new XmlSerializer(Script.GetType());
                elementsAsXml = Utility.Xml.Serialise(Script, true);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(elementsAsXml);
                Utility.Xml.DeleteAttribute(doc.DocumentElement, "xmlns:xsi");
                if (elements == null)
                    elements = new XmlElement[1];
                elements[0] = doc.DocumentElement;
            }
            else if (elementsAsXml == null && elements != null && elements.Length >= 1)
                elementsAsXml = elements[0].OuterXml;
            else if (elementsAsXml == null)
                elementsAsXml = "<Script />";
        }


    }
}