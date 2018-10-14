
namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// The manager model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ManagerView")]
    [PresenterName("UserInterface.Presenters.ManagerPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Zones.RectangularZone))]
    [ValidParent(ParentType = typeof(Zones.CircularZone))]
    [ValidParent(ParentType = typeof(Agroforestry.AgroforestrySystem))]
    public class Manager : Model, IDontSerialiseChildren
    {
        // ----------------- Privates
        /// <summary>The _ code</summary>
        private string _Code;
        /// <summary>The has deserialised</summary>
        private bool HasLoaded = false;
        /// <summary>The elements as XML</summary>
        private string elementsAsXml = null;
        /// <summary>Name of compiled assembly</summary>
        [NonSerialized]
        private string assemblyName = null;

        /// <summary>The _ script</summary>
        [NonSerialized] private Model _Script;
        /// <summary>The _elements</summary>
        [NonSerialized] private XmlElement[] _elements;

        /// <summary>The compiled code</summary>
        [NonSerialized] private string CompiledCode = "";

        // ----------------- Parameters (XML serialisation)
        /// <summary>Gets or sets the elements.</summary>
        [XmlAnyElement(Name = "Script")]
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
                if (value != null && value.Length > 1)
                {
                    _elements = new XmlElement[1];
                    _elements[0] = value[value.Length - 1];
                }
                else
                    _elements = value;
            }
        }

        /// <summary>Gets or sets the code c data.</summary>
        [XmlElement("Code")]
        public string CodeCData
        {
            get
            {
                return Code;
            }
            set
            {
                if (value == null)
                {
                    Code = null;
                    return;
                }

                Code = value;
            }
        }

        /// <summary>The script Model that has been compiled</summary>
        [XmlIgnore]
        public Model Script 
        { 
            get { return _Script; } 
            set { _Script = value; } 
        }

        /// <summary>
        /// Stores column and line of caret, and scrolling position when editing in GUI
        /// This isn't really a Rectangle, but the Rectangle class gives us a convenient
        /// way to store both the caret position and scrolling information.
        /// </summary>
        [XmlIgnore] public System.Drawing.Rectangle Location = new System.Drawing.Rectangle(1, 1, 0, 0);

        /// <summary>
        /// Stores whether we are currently on the tab displaying the script.
        /// Meaningful only within the GUI
        /// </summary>
        [XmlIgnore] public int ActiveTabIndex = 0;


        /// <summary>The code for the Manager script</summary>
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

        /// <summary>
        /// Called when the model has been newly created in memory whether from 
        /// cloning or deserialisation.
        /// </summary>
        public override void OnCreated()
        {
            HasLoaded = true;
            if (Script == null && Code != string.Empty)
                RebuildScriptModel();
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
            if (HasLoaded)
            {
                // Capture the current values of all parameters.
                EnsureParametersAreCurrent();

                if (_Code != CompiledCode)
                {
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
                            compiledAssembly = ReflectionUtilities.CompileTextToAssembly(Code, GetAssemblyFileName());
                            // Get the script 'Type' from the compiled assembly.
                            if (compiledAssembly.GetType("Models.Script") == null)
                                throw new ApsimXException(this, "Cannot find a public class called 'Script'");

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
                            Children.Clear();
                            Children.Add(Script);
                            Script.Parent = this;
                        }
                        catch (Exception err)
                        {
                            lastCompileFailed = true;
                            throw new Exception("Unable to compile \"" + Name + "\"", err);
                        }
                    }
                }
            }
        }

        /// <summary>Work out the assembly file name (with path).</summary>
        public string GetAssemblyFileName()
        {
            return Path.ChangeExtension(Path.GetTempFileName(), ".dll");
        }

        /// <summary>A handler to resolve the loading of manager assemblies when binary deserialization happens.</summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <remarks>
        /// Seems like it will only look for DLL's in the bin folder. We can't put the manager DLLs in there
        /// because when ApsimX is installed, the bin folder will be under c:\program files and we won't have
        /// permission to save the manager dlls there. Instead we put them in %TEMP%\ApsimX and use this 
        /// event handler to resolve the assemblies to that location.
        /// </remarks>
        /// <returns></returns>
        public static Assembly ResolveManagerAssembliesEventHandler(object sender, ResolveEventArgs args)
        {
            string tempDLLPath = Path.GetTempPath();
            if (!Path.GetTempPath().Contains("ApsimX"))
                tempDLLPath = Path.Combine(tempDLLPath, "ApsimX");
            if (Directory.Exists(tempDLLPath))
            {
                foreach (string fileName in Directory.GetFiles(tempDLLPath, "*.dll"))
                    if (args.Name.Split(',')[0] == Path.GetFileNameWithoutExtension(fileName))
                        return Assembly.LoadFrom(fileName);
            }
            return null;
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
                {
                    object value;
                    if (element.InnerText.StartsWith(".Simulations."))
                        value = Apsim.Get(this, element.InnerText);
                    else if (property.PropertyType == typeof(IPlant))
                        value = Apsim.Find(this, element.InnerText);
                    else
                        value = ReflectionUtilities.StringToObject(property.PropertyType, element.InnerText);
                    property.SetValue(script, value, null);
                }
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
                    else if (value is IModel)
                        value = Apsim.FullPath(value as IModel);
                    XmlUtilities.SetValue(doc.DocumentElement, property.Name, 
                                         ReflectionUtilities.ObjectToString(value));
                }
            }
            return doc.DocumentElement;
        }

    }
}
