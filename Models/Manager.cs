using System;
using System.Xml;
using System.Reflection;
using System.Xml.Serialization;
using Models.Core;
using System.Xml.Schema;

namespace Models
{

    [ViewName("UserInterface.Views.ManagerView")]
    [PresenterName("UserInterface.Presenters.ManagerPresenter")]
    public class Manager : Model
    {
        // Privates
        private Assembly CompiledAssembly;
        private string _Code;
        private Type ScriptType;
        private bool HasDeserialised = false;

        // Links
        [Link]
        private Zone Zone = null;

        [Link]
        private ISummary Summary = null;

        // Publics
        [XmlIgnore]
        public Model Script { get; set; }

        [XmlAnyElement]
        public XmlElement[] elements { get; set; }

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

        public string[] ParameterNames { get; set; }
        public string[] ParameterValues { get; set; }

        public Zone ParentZone { get { return Zone; } }

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
                string scriptXml = null;
                if (Script != null)
                {
                    // First serialise the existing model.
                    XmlSerializer serial = new XmlSerializer(ScriptType);
                    scriptXml = Utility.Xml.Serialise(Script, true);

                    // Get rid of old script model.
                    this.RemoveModel(Script);
                    Script = null;
                }

                // Compile the script
                try
                {
                    CompileScript();

                    if (ScriptType != null)
                    {
                        if (elements.Length > 0 || scriptXml != null)
                        {
                            XmlElement scriptNode;

                            if (scriptXml != null)
                            {
                                XmlDocument doc = new XmlDocument();
                                doc.LoadXml(scriptXml);
                                scriptNode = doc.DocumentElement;
                            }
                            else
                                scriptNode = elements[0];

                            XmlSerializer serial = new XmlSerializer(ScriptType);
                            Script = serial.Deserialize(new XmlNodeReader(scriptNode)) as Model;
                        }
                        else
                            Script = Activator.CreateInstance(ScriptType) as Model;

                        this.AddModel(Script, true);

                        // Call the OnInitialised if present.
                        MethodInfo OnInitialised = Script.GetType().GetMethod("OnInitialised", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (OnInitialised != null)
                            OnInitialised.Invoke(Script, new object[] { this, null });
                    }
                }
                catch (Exception err)
                {
                    Summary.WriteWarning(FullPath, err.Message);
                }
            }
        }

        private void CompileScript()
        {
            try
            {
                CompiledAssembly = Utility.Reflection.CompileTextToAssembly(Code);
                // Go look for our class name.
                ScriptType = CompiledAssembly.GetType("Models.Script");
                if (ScriptType == null)
                    Summary.WriteWarning(FullPath, "Cannot find a public class called Script");
                Summary.WriteMessage(FullPath, "Script compiled ok");
            }
            catch (Exception err)
            {
                Summary.WriteWarning(FullPath, err.Message);
            }
            
        }
    }
}