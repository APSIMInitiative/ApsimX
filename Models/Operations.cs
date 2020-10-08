using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Models.Core;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
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
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Operation()
        {
            Enabled = true;
        }

        /// <summary>Gets or sets the date.</summary>
        public string Date { get; set; }

        /// <summary>Gets or sets the action.</summary>
        /// <value>The action.</value>
        public string Action { get; set; }

        /// <summary>Gets the action model.</summary>
        /// <returns></returns>
        public string GetActionModel()
        {
            int posPeriod = Action.IndexOf('.');
            if (posPeriod >= 0)
                return Action.Substring(0, posPeriod);

            return "";
        }

        /// <summary>
        /// Used to determine whether the operation is enabled or not.
        /// </summary>
        public bool Enabled { get; set; }
    }

    /// <summary>This class encapsulates an operations schedule.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.EditorView")]
    [PresenterName("UserInterface.Presenters.OperationsPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Factorial.CompositeFactor))]
    [ValidParent(ParentType = typeof(Factorial.Factor))]
    public class Operations : Model
    {
        /// <summary>The clock</summary>
        [Link] Clock Clock = null;

        /// <summary>Gets or sets the schedule.</summary>
        /// <value>The schedule.</value>
        public List<Operation> Operation { get; set; }

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
            if (Operation == null)
                Operation = new List<Operation>();

            DateTime operationDate;
            foreach (Operation operation in Operation.Where(o => o.Enabled))
            {
                operationDate = DateUtilities.validateDateString(operation.Date, Clock.Today.Year);
                if (operationDate == Clock.Today)
                {
                    string st = operation.Action;

                    // If the action contains a comment anywhere, we ignore everything after (and including) the comment.
                    int commentPosition = st.IndexOf("//");
                    if (commentPosition >= 0)
                        st = st.Substring(0, commentPosition);

                    if (st.Contains("="))
                    {
                        string variableName = st;
                        string value = StringUtilities.SplitOffAfterDelimiter(ref variableName, "=").Trim();
                        variableName = variableName.Trim();
                        this.FindByPath(variableName).Value = value;
                    }
                    else if (st.Trim() != string.Empty)
                    {
                        string argumentsString = StringUtilities.SplitOffBracketedValue(ref st, '(', ')');
                        string[] arguments = argumentsString.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                        int posPeriod = st.LastIndexOf('.');
                        if (posPeriod == -1)
                            throw new ApsimXException(this, "Bad operations action found: " + operation.Action);
                        string modelName = st.Substring(0, posPeriod);
                        string methodName = st.Substring(posPeriod + 1).Replace(";", "").Trim();

                        Model model = this.FindByPath(modelName)?.Value as Model;
                        if (model == null)
                            throw new ApsimXException(this, "Cannot find model: " + modelName);


                        MethodInfo[] methods = model.GetType().GetMethods();
                        if (methods == null)
                            throw new ApsimXException(this, "Cannot find method: " + methodName + " in model: " + modelName);

                        object[] parameterValues = null;
                        foreach (MethodInfo method in methods)
                        {
                            if (method.Name.Equals(methodName, StringComparison.CurrentCultureIgnoreCase))
                            {
                                parameterValues = GetArgumentsForMethod(arguments, method);

                                // invoke method.
                                if (parameterValues != null)
                                {
                                    try
                                    {
                                        method.Invoke(model, parameterValues);
                                    }
                                    catch (Exception err)
                                    {
                                        throw err.InnerException;
                                    }
                                    break;
                                }
                            }
                        }

                        if (parameterValues == null)
                            throw new ApsimXException(this, "Cannot find method: " + methodName + " in model: " + modelName);
                    }
                }
            }
        }

        /// <summary>
        /// Try and get the arguments for the specified method. Will return null if arguments don't match the method.
        /// </summary>
        /// <param name="arguments">The arguments specified by user.</param>
        /// <param name="method">The method to try and match to.</param>
        /// <returns>The arguments or null if not matched.</returns>
        private object[] GetArgumentsForMethod(string[] arguments, MethodInfo method)
        {
            // convert arguments to an object array.
            ParameterInfo[] parameters = method.GetParameters();
            object[] parameterValues = new object[parameters.Length];
            if (arguments.Length > parameters.Length)
                return null;

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
                        return null;
                    value = value.Substring(posColon + 1);
                }

                if (argumentIndex >= parameterValues.Length)
                    return null;

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

            return parameterValues;
        }

    }
}
