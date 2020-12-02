namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.Core.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Reflection;
    using Newtonsoft.Json;

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
    [ValidParent(ParentType = typeof(Factorial.CompositeFactor))]
    [ValidParent(ParentType = typeof(Factorial.Factor))]
    [ValidParent(ParentType = typeof(Soils.Soil))]
    public class Manager : Model, IOptionallySerialiseChildren, ICustomDocumentation
    {
        [NonSerialized]
        [Link]
        private ScriptCompiler scriptCompiler = null;

        /// <summary>The code to compile.</summary>
        private string cSharpCode = ReflectionUtilities.GetResourceAsString("Models.Resources.Scripts.BlankManager.cs");

        /// <summary>Is the model after creation.</summary>
        private bool afterCreation = false;

        /// <summary>
        /// At design time the [Link] above will be null. In that case search for a 
        /// Simulations object and get its compiler.
        /// 
        /// </summary>
        private ScriptCompiler Compiler()
        {
            if (TryGetCompiler())
                return scriptCompiler;
            else
                throw new Exception("Cannot find a script compiler in manager.");
        }

        /// <summary>
        /// At design time the [Link] above will be null. In that case search for a 
        /// Simulations object and get its compiler.
        /// </summary>
        /// <returns>True if compiler was found.</returns>
        private bool TryGetCompiler()
        {
            if (scriptCompiler == null)
            {
                var simulations = FindAncestor<Simulations>();
                if (simulations == null)
                    return false;
                scriptCompiler = simulations.ScriptCompiler;
            }
            return true;
        }

        /// <summary>Gets or sets the code to compile.</summary>
        public string Code
        {
            get
            {
                return cSharpCode;
            }
            set
            {
                cSharpCode = value;
                RebuildScriptModel();
            }
        }

        /// <summary>The script Model that has been compiled</summary>
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        /// <summary>Allow children to be serialised?</summary>
        public bool DoSerialiseChildren { get { return false; } }

        /// <summary>
        /// Stores column and line of caret, and scrolling position when editing in GUI
        /// This isn't really a Rectangle, but the Rectangle class gives us a convenient
        /// way to store both the caret position and scrolling information.
        /// </summary>
        [JsonIgnore]
        public Rectangle Location { get; set; }  = new Rectangle(1, 1, 0, 0);

        /// <summary>
        /// Stores whether we are currently on the tab displaying the script.
        /// Meaningful only within the GUI
        /// </summary>
        [JsonIgnore]
        public int ActiveTabIndex { get; set; }

        /// <summary>
        /// Called when the model has been newly created in memory whether from 
        /// cloning or deserialisation.
        /// </summary>
        public override void OnCreated()
        {
            afterCreation = true;

            // During ModelReplacement.cs, OnCreated is called. When this happens links haven't yet been
            // resolved and there is no parent Simulations object which leads to no ScriptCompiler
            // instance. This needs to be fixed.
            if (TryGetCompiler())
                RebuildScriptModel();
        }

        /// <summary>
        /// Invoked at start of simulation.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            if (Children.Count != 0)
            {
                GetParametersFromScriptModel();
                SetParametersInScriptModel();
            }
        }

        /// <summary>Rebuild the script model and return error message if script cannot be compiled.</summary>
        public void RebuildScriptModel()
        {
            if (Enabled && afterCreation && !string.IsNullOrEmpty(Code))
            {
                // If the script child model exists. Then get its parameter values.
                if (Children.Count != 0)
                    GetParametersFromScriptModel();

                var results = Compiler().Compile(Code, this);
                if (results.ErrorMessages == null)
                {
                    if (Children.Count != 0)
                        Children.Clear();
                    var newModel = results.Instance as IModel;
                    if (newModel != null)
                    {
                        newModel.IsHidden = true;
                        Structure.Add(newModel, this);
                    }
                }
                else
                    throw new Exception($"Errors found in manager model {Name}{Environment.NewLine}{results.ErrorMessages}");
                SetParametersInScriptModel();
            }
        }

        /// <summary>Set the scripts parameters from the 'xmlElement' passed in.</summary>
        private void SetParametersInScriptModel()
        {
            if (Enabled && Children.Count > 0)
            {
                var script = Children[0];
                if (Parameters != null)
                {
                    List<Exception> errors = new List<Exception>();
                    foreach (var parameter in Parameters)
                    {
                        try
                        {
                            PropertyInfo property = script.GetType().GetProperty(parameter.Key);
                            if (property != null)
                            {
                                object value;
                                if (parameter.Value.StartsWith(".") || parameter.Value.StartsWith("["))
                                    value = this.FindByPath(parameter.Value)?.Value;
                                else if (property.PropertyType == typeof(IPlant))
                                    value = this.FindInScope(parameter.Value);
                                else
                                    value = ReflectionUtilities.StringToObject(property.PropertyType, parameter.Value);
                                property.SetValue(script, value, null);
                            }
                        }
                        catch (Exception err)
                        {
                            errors.Add(err);
                        }
                    }
                    if (errors.Count > 0)
                    {
                        string message = "";
                        foreach (Exception error in errors)
                            message += error.Message;
                        throw new Exception(message);
                    }
                }
            }
        }

        /// <summary>Get all parameters from the script model and store in our parameters list.</summary>
        /// <returns></returns>
        private void GetParametersFromScriptModel()
        {
            if (Children.Count > 0)
            {
                var script = Children[0];

                if (Parameters == null)
                    Parameters = new List<KeyValuePair<string, string>>();
                Parameters.Clear();
                foreach (PropertyInfo property in script.GetType().GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public))
                {
                    if (property.CanRead && property.CanWrite &&
                        ReflectionUtilities.GetAttribute(property, typeof(JsonIgnoreAttribute), false) == null)
                    {
                        object value = property.GetValue(script, null);
                        if (value == null)
                            value = "";
                        else if (value is IModel)
                            value = "[" + (value as IModel).Name + "]";
                        Parameters.Add(new KeyValuePair<string, string>
                                            (property.Name,
                                             ReflectionUtilities.ObjectToString(value)));
                    }
                }
            }
        }

        /// <summary>Ovewrite default auto-doc.</summary>
        /// <param name="tags"></param>
        /// <param name="headingLevel"></param>
        /// <param name="indent"></param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // document children
                foreach (IModel child in Children)
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }
    }
}
