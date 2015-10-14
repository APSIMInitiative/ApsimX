using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using System.Globalization;
using APSIM.Shared.Utilities;

namespace Models
{
    /// <summary>
    /// Operations model
    /// </summary>
    [Serializable]
    public class Operation
    {
        /// <summary>Gets or sets the date.</summary>
        /// <value>The date.</value>
        public DateTime Date { get; set; }
        /// <summary>Gets or sets the action.</summary>
        /// <value>The action.</value>
        public string Action { get; set; }

        /// <summary>Gets the action model.</summary>
        /// <returns></returns>
        public string GetActionModel()
        {
            int PosPeriod = Action.IndexOf('.');
            if (PosPeriod >= 0)
                return Action.Substring(0, PosPeriod);

            return "";
        }


    }

    /// <summary>This class encapsulates an operations schedule.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.EditorView")]
    [PresenterName("UserInterface.Presenters.OperationsPresenter")]
    [ValidParent(typeof(Zone))]
    public class Operations : Model
    {
        /// <summary>The clock</summary>
        [Link] Clock Clock = null;


        /// <summary>Gets or sets the schedule.</summary>
        /// <value>The schedule.</value>
        [XmlElement("Operation")]
        public List<Operation> Schedule { get; set; }

        /// <summary>Simulation is commencing.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="ApsimXException">
        /// Bad operations action found:  + operation.Action
        /// or
        /// Cannot find model:  + modelName
        /// or
        /// Cannot find method:  + methodName +  in model:  + modelName
        /// or
        /// Cannot find argument:  + argumentName +  in operation call:  + operation.Action
        /// </exception>
        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            foreach (Operation operation in Schedule)
            {
                if (operation.Date == Clock.Today)
                {
                    string st = operation.Action;
                    int posComment = operation.Action.IndexOf("//");
                    if (posComment != -1)
                        operation.Action = operation.Action.Remove(posComment);

                    if (operation.Action.Trim() != string.Empty)
                    {
                        string argumentsString = StringUtilities.SplitOffBracketedValue(ref st, '(', ')');
                        string[] arguments = argumentsString.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                        int posPeriod = st.LastIndexOf('.');
                        if (posPeriod == -1)
                            throw new ApsimXException(this, "Bad operations action found: " + operation.Action);
                        string modelName = st.Substring(0, posPeriod);
                        string methodName = st.Substring(posPeriod + 1).Replace(";", "").Trim();

                        Model model = Apsim.Get(this, modelName) as Model;
                        if (model == null)
                            throw new ApsimXException(this, "Cannot find model: " + modelName);
                        MethodInfo method = model.GetType().GetMethod(methodName);
                        if (method == null)
                            throw new ApsimXException(this, "Cannot find method: " + methodName + " in model: " + modelName);

                        // convert arguments to an object array.
                        ParameterInfo[] parameters = method.GetParameters();
                        object[] parameterValues = new object[parameters.Length];

                        //retrieve the values for the named arguments that were provided. (not all the named arguments for the method may have been provided)
                        for (int i = 0; i < arguments.Length; i++)
                        {
                            string value = arguments[i];
                            int argumentIndex;
                            int posColon = arguments[i].IndexOf(':');
                            if (posColon == -1)
                                argumentIndex = i;
                            else
                            {
                                string argumentName = arguments[i].Substring(0, posColon).Trim();
                                // find parameter with this name.
                                for (argumentIndex = 0; argumentIndex < parameters.Length; argumentIndex++)
                                {
                                    if (parameters[argumentIndex].Name == argumentName)
                                        break;
                                }
                                if (argumentIndex == parameters.Length)
                                    throw new ApsimXException(this, "Cannot find argument: " + argumentName + " in operation call: " + operation.Action);
                                value = value.Substring(posColon + 1);
                            }

                            // convert value to correct type.
                            if (parameters[argumentIndex].ParameterType == typeof(double))
                                parameterValues[argumentIndex] = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                            else if (parameters[argumentIndex].ParameterType == typeof(float))
                                parameterValues[argumentIndex] = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                            else if (parameters[argumentIndex].ParameterType == typeof(int))
                                parameterValues[argumentIndex] = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                            else if (parameters[argumentIndex].ParameterType == typeof(bool))
                                parameterValues[argumentIndex] = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                            else if (parameters[argumentIndex].ParameterType == typeof(string))
                                parameterValues[argumentIndex] = value.Replace("\"", "").Trim();
                            else if (parameters[argumentIndex].ParameterType.IsEnum)
                            {
                                value = value.Trim();
                                int posLastPeriod = value.LastIndexOf('.');
                                if (posLastPeriod != -1)
                                    value = value.Substring(posLastPeriod + 1);
                                parameterValues[argumentIndex] = Enum.Parse(parameters[argumentIndex].ParameterType, value);
                            }
                        }


                        //if there were missing named arguments in the method call then use the default values for them.
                        for (int i = 0; i < parameterValues.Length; i++)
                        {
                            if (parameterValues[i] == null)
                            {
                                parameterValues[i] = parameters[i].DefaultValue;
                            }
                        }


                        // invoke method.
                        method.Invoke(model, parameterValues);
                    }
                }
            }
        }

       
    }
}
