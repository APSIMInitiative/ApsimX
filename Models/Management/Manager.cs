using System;
using System.Collections.Generic;
using System.Reflection;
using APSIM.Shared.Utilities;
using Models.Core;
using Newtonsoft.Json;
using Shared.Utilities;
using APSIM.Core;
using System.Linq;
using System.IO;

namespace Models
{

    /// <summary>
    /// Manager scripts are used to modify the simulation with C# code.
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
    public class Manager : Model
    {
        /// <summary>The code to compile.</summary>
        private string[] cSharpCode = ReflectionUtilities.GetResourceAsStringArray("Models.Resources.Scripts.BlankManager.cs");
        /// <summary>
        /// Stores the code for the current child script model. This is used
        /// to check if the child script model needs recompiling.
        /// </summary>
        private string CodeForLastSuccessfullCompile;

        /// <summary>Get the compiled script model or null if none.</summary>
        [JsonIgnore]
        public IModel Script { get; private set; }

        /// <summary>The array of code lines that gets stored in file</summary>
        public string[] CodeArray
        {
            get
            {
                return cSharpCode;
            }
            set
            {
                cSharpCode = value;
            }
        }

        /// <summary>Gets or sets the code to compile.</summary>
        [JsonIgnore]
        public string Code
        {
            get
            {
                return CodeFormatting.Combine(cSharpCode);
            }
            set
            {
                if (value == null)
                {
                    throw new Exception("Value 'Null' cannot be stored in Manager.Code");
                }
                else
                {
                    cSharpCode = CodeFormatting.Split(value);
                    if (Node != null)
                        RebuildScriptModel();
                }
            }
        }

        /// <summary>The script Model that has been compiled</summary>
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        /// <summary>
        /// Stores the cursor position so the page location is saved when moving around the GUI
        /// Meaningful only within the GUI
        /// </summary>
        [JsonIgnore]
        public ManagerCursorLocation Cursor { get; set; } = new ManagerCursorLocation();

        /// <summary>
        /// Stores errors that were generated the last time the script was compiled.
        /// </summary>
        [JsonIgnore]
        public string Errors { get; private set; } = null;

        /// <summary>
        /// Instance has been created.
        /// </summary>
        public override void OnCreated()
        {
            base.OnCreated();
            RebuildScriptModel();
        }

        /// <summary>
        /// Called when the model is about to be deserialised.
        /// </summary>
        public override void OnSerialising()
        {
            GetParametersFromScriptModel();
        }

        /// <summary>
        /// Invoked at start of simulation.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            // Need to clear the locator cache so that when we set properties in the script model
            // parameters like [Lentil] get resolved, not from the cache, but from a new search
            // for the model. The cache can be out of date for models (e.g. lentil) that have been
            // overwritten from Replacements.
            Locator.Clear();

            // Need to update our parameter value collection and then reset them in the script.
            // Some manager scripts refer to a model (e.g. [Lentil]). Resetting these parameters
            // in the script model will force a lookup on a new model instance which may have come
            // from an override like Replacements or factor.
            GetParametersFromScriptModel();
            SetParametersInScriptModel();
        }

        /// <summary>Rebuild the script model and return error message if script cannot be compiled.</summary>
        /// <param name="allowDuplicateClassName">Optional to not throw if this has a duplicate class name (used when copying script node)</param>
        public void RebuildScriptModel(bool allowDuplicateClassName = false)
        {
            // Only compile if code is different to last successful compilation.
            if (Enabled && !string.IsNullOrEmpty(Code) && Code != CodeForLastSuccessfullCompile)
            {
                // If the script child model exists. Then get its parameter values.
                if (Script != null)
                    GetParametersFromScriptModel();

                var results = Node.Compiler.Compile(Code, Node, null, allowDuplicateClassName);
                Errors = results.ErrorMessages;

                // Remove old script node.
                if (Script != null)
                {
                    Node.RemoveChild(Script as INodeModel);
                    Script = null;
                }

                if (Errors == null)
                {
                    //add new script model
                    Script = results.Instance as IModel;
                    if (Script != null)
                    {
                        CodeForLastSuccessfullCompile = Code;
                        Script.IsHidden = true;
                        Node.AddChild(Script as INodeModel);
                    }
                }
                if (Script == null)
                {
                    CodeForLastSuccessfullCompile = null;
                    Parameters = null;
                    if (Errors != null)
                        throw new Exception($"Errors found in manager model {Name}{Environment.NewLine}{Errors}");
                }

                SetParametersInScriptModel();
            }
        }

        /// <summary>Set the scripts parameters from the 'xmlElement' passed in.</summary>
        private void SetParametersInScriptModel()
        {
            if (Enabled && Script != null && Parameters != null)
            {
                    List<Exception> errors = new List<Exception>();
                    foreach (var parameter in Parameters)
                    {
                        try
                        {
                            PropertyInfo property = Script.GetType().GetProperty(parameter.Key);
                            if (property != null)
                            {
                                object value;
                                if ((typeof(IModel).IsAssignableFrom(property.PropertyType) || property.PropertyType.IsInterface) && (parameter.Value.StartsWith(".") || parameter.Value.StartsWith("[")))
                                    value = this.FindByPath(parameter.Value)?.Value;
                                else if (property.PropertyType == typeof(IPlant))
                                    value = this.FindInScope(parameter.Value);
                                else
                                    value = ReflectionUtilities.StringToObject(property.PropertyType, parameter.Value);
                                property.SetValue(Script, value, null);
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

        /// <summary>Get all parameters from the script model and store in our parameters list.</summary>
        /// <returns></returns>
        public void GetParametersFromScriptModel()
        {
            if (Enabled && Script != null)
            {
                if (Parameters == null)
                    Parameters = new List<KeyValuePair<string, string>>();
                Parameters.Clear();

                foreach (PropertyInfo property in Script.GetType().GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public))
                {
                    if (property.CanRead && property.CanWrite &&
                        ReflectionUtilities.GetAttribute(property, typeof(JsonIgnoreAttribute), false) == null &&
                        Attribute.IsDefined(property, typeof(DescriptionAttribute)))
                    {
                        object value = property.GetValue(Script, null);
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

        /// <summary>Get the value of a property in this Manager</summary>
        /// <returns>The value of the property</returns>
        public object GetProperty(string name)
        {
            object script = this.Script;
            if (script == null)
                throw new Exception($"{this.Name} has not been compiled and cannot get the value of a property.");

            return ReflectionUtilities.GetValueOfFieldOrProperty(name, script);
        }

        /// <summary>Set the value of a property in this Manager</summary>
        public void SetProperty(string name, object newValue)
        {
            object script = this.Script;
            if (script == null)
                throw new Exception($"{this.Name} has not been compiled and cannot set the value of a property.");

            ReflectionUtilities.SetValueOfFieldOrProperty(name, script, newValue);
            return;
        }

        /// <summary>Run a function defined in this Manager, arguments can be passed if required for the function</summary>
        /// <returns>The value the function returns</returns>
        public object RunMethod(string name, object[] args)
        {
            object script = this.Script;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod;

            Type t = script.GetType();
            List<MethodInfo> methods = ReflectionUtilities.GetAllMethods(t, flags, false);

            foreach(MethodInfo method in methods)
            {
                if (method.Name.CompareTo(name) == 0)
                {
                    return method.Invoke(script, args);
                }
            }

            throw new Exception($"{this.Name} does not have an accessible method called {name}.");
        }

        /// <summary>Run a function defined in this Manager, up to four arguments can be passed
        /// Use the other version of this method with an object array to pass more arguments.</summary>
        /// <returns>The value the function returns</returns>
        public object RunMethod(string name, object arg1 = null, object arg2 = null, object arg3 = null, object arg4 = null)
        {
            int count = 0;
            if (arg1 != null)
                count += 1;
            if (arg2 != null)
                count += 1;
            if (arg3 != null)
                count += 1;
            if (arg4 != null)
                count += 1;

            object[] args = new object[count];
            int index = 0;
            if (arg1 != null)
            {
                args[index] = arg1;
                index += 1;
            }
            if (arg2 != null)
            {
                args[index] = arg2;
                index += 1;
            }
            if (arg3 != null)
            {
                args[index] = arg3;
                index += 1;
            }
            if (arg4 != null)
            {
                args[index] = arg4;
                index += 1;
            }
            return RunMethod(name, args);
        }

        /// <summary>
        /// Adjusts whitespace and newlines to fit dev team's normal formatting. For use with user scripts that have poor formatting.
        /// </summary>
        public void Reformat()
        {
            this.CodeArray = CodeFormatting.Reformat(this.CodeArray);
        }
    }
}
