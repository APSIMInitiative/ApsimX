using System;
using System.Xml;
using System.Reflection;
using System.Xml.Serialization;
using Models.Core;
using System.Xml.Schema;
using System.Runtime.Serialization;
using System.IO;
using System.Windows.Forms;
using APSIM.Shared.Utilities;
namespace Models
{
    /// <summary>
    /// The manager model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ManagerView")]
    [PresenterName("UserInterface.Presenters.ManagerPresenter")]
    [ValidParent(ParentModels=new Type[] { typeof(Simulation), typeof(Zone), typeof(Zones.RectangularZone), typeof(Zones.CircularZone), typeof(Agroforestry.AgroforestrySystem) })]
    public class Manager : Model
    {
        // ----------------- Privates
        /// <summary>The _ code</summary>
        private string _Code;
        /// <summary>The has deserialised</summary>
        private bool HasDeserialised = false;
        /// <summary>The elements as XML</summary>
        private string elementsAsXml = null;


        private string assemblyName = null;

        /// <summary>The _ script</summary>
        [NonSerialized] private Model _Script;
        /// <summary>The _elements</summary>
        [NonSerialized] private XmlElement[] _elements;

        /// <summary>The compiled code</summary>
        [NonSerialized] private string CompiledCode = "";

        // ----------------- Parameters (XML serialisation)
        /// <summary>Gets or sets the elements.</summary>
        /// <value>The elements.</value>
        [XmlAnyElement]
        public XmlElement[] elements 
        { 
            get 
            {
                // Capture the current values of all parameters.
                EnsureParametersAreCurrent();

                return _elements;
            } 
            
            set 
            { 
                _elements = value; 
            } 
        }

        /// <summary>Gets or sets the code c data.</summary>
        /// <value>The code c data.</value>
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
        /// <summary>The script Model that has been compiled</summary>
        /// <value>The script.</value>
        [XmlIgnore]
        public Model Script 
        { 
            get { return _Script; } 
            set { _Script = value; } 
        }

        /// <summary>The code for the Manager script</summary>
        /// <value>The code.</value>
        [Summary]
        [Description("Script code")]
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

        /// <summary>The model has been loaded.</summary>
        [EventSubscribe("Loaded")]
        private void OnLoaded()
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
        /// <param name="xmlSerialisation">if set to <c>true</c> [XML serialisation].</param>
        [EventSubscribe("Serialising")]
        private void OnSerialising(bool xmlSerialisation)
        {
            if (Script != null)
                Children.Remove(Script);
        }

        /// <summary>Serialisation has completed. Read our 'Script' model if necessary.</summary>
        /// <param name="xmlSerialisation">if set to <c>true</c> [XML serialisation].</param>
        [EventSubscribe("Serialised")]
        private void OnSerialised(bool xmlSerialisation)
        {
            if (Script != null)
                Children.Add(Script);
        }

        /// <summary>At simulation commencing time, rebuild the script assembly if required.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            RebuildScriptModel();
        }

        private bool lastCompileFailed = false;

        /// <summary>Rebuild the script model and return error message if script cannot be compiled.</summary>
        /// <exception cref="ApsimXException">
        /// Cannot find a public class called 'Script'
        /// </exception>
        public void RebuildScriptModel()
        {
            if (HasDeserialised)
            {
                // Capture the current values of all parameters.
                EnsureParametersAreCurrent();

                if (NeedToCompileCode())
                {
                    // If a script model already exists, then get rid of it.
                    if (Script != null)
                    {
                        Children.Remove(Script);
                        Script = null;
                    }
                    
                    // Compile the code.
                    Assembly compiledAssembly = null;
                    
                    if (assemblyName != null)
                    {
                        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            if (assembly.FullName == assemblyName)
                            {
                                compiledAssembly = assembly;
                                break;
                            }
                        }
                    }
                    
                    // When running a simulation, we don't want to waste time by re-compiling
                    // the script unnecessarily. But we need to be careful to avoid two sorts
                    // of problems: (1) failing to recompile when the script has been changed
                    // and (2) attempting to use a previously compiled assembly when the script 
                    // has been modified and will now not compile correctly.
                    if (compiledAssembly == null || CompiledCode != null || lastCompileFailed)
                    {
                        try
                        {
                            compiledAssembly = ReflectionUtilities.CompileTextToAssembly(Code, null);
                            // Get the script 'Type' from the compiled assembly.
                            if (compiledAssembly.GetType("Models.Script") == null)
                                throw new ApsimXException(this, "Cannot find a public class called 'Script'");
                        }
                        catch (Exception err)
                        {
                            lastCompileFailed = true;
                            throw new ApsimXException(this, err.Message);
                        }
                    }

                    assemblyName = compiledAssembly.FullName;
                    CompiledCode = _Code;
                    lastCompileFailed = false;

                    // Create a new script model.
                    Script = compiledAssembly.CreateInstance("Models.Script") as Model;
                    Script.Children = new System.Collections.Generic.List<Model>();
                    Script.Name = "Script";
                    Script.IsHidden = true;
                    XmlElement parameters;

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(elementsAsXml);
                    parameters = doc.DocumentElement;
                    
                    SetParametersInObject(Script, parameters);

                    // Add the new script model to our models collection.
                    Children.Add(Script);
                    Script.Parent = this;
                }
            }
        }



        /// <summary>Return true if the code needs to be recompiled.</summary>
        /// <returns></returns>
        private bool NeedToCompileCode()
        {
            return (Script == null || _Code != CompiledCode);
        }

        /// <summary>Ensures the parameters are up to date and reflect the current 'Script' model.</summary>
        private void EnsureParametersAreCurrent()
        {
            if (Script != null)
            {
                if (_elements == null)
                    _elements = new XmlElement[1];
                _elements[0] = GetParametersInObject(Script);
            }
            
            if (_elements != null && _elements.Length >= 1)
                elementsAsXml = _elements[0].OuterXml;
            else if (elementsAsXml == null)
                elementsAsXml = "<Script />";
        }

        /// <summary>Set the scripts parameters from the 'xmlElement' passed in.</summary>
        /// <param name="script">The script.</param>
        /// <param name="xmlElement">The XML element.</param>
        private void SetParametersInObject(Model script, XmlElement xmlElement)
        {
            foreach (XmlElement element in xmlElement.ChildNodes)
            {
                PropertyInfo property = Script.GetType().GetProperty(element.Name);
                if (property != null)
                    property.SetValue(script, ReflectionUtilities.StringToObject(property.PropertyType, element.InnerText), null);
            }
        }

        /// <summary>Get the scripts parameters as a returned xmlElement.</summary>
        /// <param name="script">The script.</param>
        /// <returns></returns>
        private XmlElement GetParametersInObject(Model script)
        {
            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement("Script"));
            foreach (PropertyInfo property in script.GetType().GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public))
            {
                if (property.CanRead && property.CanWrite && 
                    ReflectionUtilities.GetAttribute(property, typeof(XmlIgnoreAttribute), false) == null)
                {
                    object value = property.GetValue(script, null);
                    if (value == null)
                        value = "";
                    XmlUtilities.SetValue(doc.DocumentElement, property.Name, 
                                         ReflectionUtilities.ObjectToString(value));
                }
            }
            return doc.DocumentElement;
        }


    }
}