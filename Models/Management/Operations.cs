using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using APSIM.Shared.Utilities;
using Models.Core;
using Newtonsoft.Json;

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

        /// <summary>
        /// 
        /// </summary>
        public Operation(bool enabled, string date, string action, string line)
        {
            Enabled = enabled;
            Date = date;
            Action = action;
            Line = line;
        }

        /// <summary>
        /// Used to determine whether the operation is enabled or not.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>Gets or sets the date.</summary>
        public string Date { get; set; }

        /// <summary>Gets or sets the action.</summary>
        /// <value>The action.</value>
        public string Action { get; set; }

        /// <summary>Gets or sets the line shown in the view.</summary>
        /// <value>A string</value>
        public string Line { get; set; }

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
        /// Parses a string into an Operation
        /// Format: // 2000-01-01 [NodeName].Function(1000)
        /// </summary>
        /// <param name="line">The string to parse</param>
        /// <returns>An Operation or null if there was an error parsing the string</returns>
        public static Operation ParseOperationString(string line)
        {
            try
            {
                if (line == null)
                    return null;

                if (line.Length == 0)
                    return null;

                string lineTrimmed = line.Trim();

                Regex parser = new Regex(@"\s*(\S*)\s+(.+)$");
                Regex commentParser = new Regex(@"^(\/\/)");

                Match match = commentParser.Match(lineTrimmed);
                if (match.Success)
                {
                    Operation operation = new Operation();
                    operation.Line = line;
                    operation.Enabled = false;
                    operation.Date = null;
                    operation.Action = null;
                    return operation;
                }

                match = parser.Match(lineTrimmed);
                if (match.Success)
                {
                    Operation operation = new Operation();
                    operation.Line = line;
                    operation.Enabled = true;

                    string dateString = match.Groups[1].Value;
                    operation.Date = DateUtilities.ValidateDateString(dateString);
                    if (operation.Date == null)
                        return null;

                    if (match.Groups[2].Value.Length > 0)
                        operation.Action = match.Groups[2].Value;
                    else
                        return null;

                    return operation;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
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
        [Link] IClock Clock = null;

        /// <summary>Gets or sets the schedule.</summary>
        /// <value>The schedule.</value>
        public List<Operation> OperationsList { get; set; }

        /// <summary>Gets or sets the schedule.</summary>
        /// <value>The schedule.</value>
        [JsonIgnore]
        public string OperationsAsString { 
            get {
                string output = "";
                if (OperationsList != null) 
                {
                    foreach (Operation operation in OperationsList)
                    {
                        if (operation.Action != null)
                        {
                            string dateStr = null;
                            if (!string.IsNullOrEmpty(operation.Date))
                                dateStr = DateUtilities.ValidateDateString(operation.Date);
                            string commentChar = operation.Enabled ? string.Empty : "// ";
                            output += commentChar + dateStr + " " + operation.Action;
                        }
                        else
                        {
                            output += operation.Line;
                        }
                        output += Environment.NewLine;
                    }
                }
                return output;
            }
            set {
                List<string> lines = value.Split('\n').ToList();
                OperationsList = new List<Operation>();
                foreach (string line in lines)
                {
                    if (line.Length > 0)
                    {
                        string lineTrimmed = line;
                        lineTrimmed = lineTrimmed.Replace("\n", string.Empty);
                        lineTrimmed = lineTrimmed.Replace("\r", string.Empty);
                        lineTrimmed = lineTrimmed.Trim();
                        
                        Operation operation = Operation.ParseOperationString(lineTrimmed);
                        if (operation != null)
                        {
                            OperationsList.Add(operation);
                        }
                        else
                        {
                            OperationsList.Add(new Operation(false, null, null, lineTrimmed));
                        }
                    }
                }
            }
        }

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
            if (OperationsList == null)
                OperationsList = new List<Operation>();

            DateTime operationDate;
            foreach (Operation operation in OperationsList.Where(o => o.Enabled))
            {
                if (operation.Date == null || operation.Action == null)
                    throw new Exception($"Error: Operation line '{operation.Line}' cannot be parsed.");

                operationDate = DateUtilities.GetDate(operation.Date, Clock.Today.Year);
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
            int numMandatoryParameters = parameters.Where(p => !p.HasDefaultValue).Count();
            object[] parameterValues = new object[parameters.Length];
            if (arguments.Length < numMandatoryParameters)
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
                else if (parameters[argumentIndex].ParameterType.IsArray)
                {
                    string[] tokens = value.Split(' ');
                    var elementType = parameters[argumentIndex].ParameterType.GetElementType();
                    if (elementType == typeof(double))
                        parameterValues[argumentIndex] = MathUtilities.StringsToDoubles(tokens);
                    else if (elementType == typeof(int))
                        parameterValues[argumentIndex] = MathUtilities.StringsToIntegers(tokens);
                    else if (elementType == typeof(string))
                        parameterValues[argumentIndex] = tokens;
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
