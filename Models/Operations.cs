using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace Models
{
    [Serializable]
    public class Operation
    {
        public DateTime Date { get; set; }
        public string Action { get; set; }

        public string GetActionModel()
        {
            int PosPeriod = Action.IndexOf('.');
            if (PosPeriod >= 0)
                return Action.Substring(0, PosPeriod);

            return "";
        }


    }

    /// <summary>
    /// This class encapsulates an operations schedule.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.EditorView")]
    [PresenterName("UserInterface.Presenters.OperationsPresenter")]
    public class Operations : Model
    {
        [Link] Clock Clock = null;

        
        [XmlElement("Operation")]
        public List<Operation> Schedule { get; set; }

        /// <summary>
        /// Simulation is commencing.
        /// </summary>
        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            foreach (Operation operation in Schedule)
            {
                if (operation.Date == Clock.Today)
                {
                    string st = operation.Action;
                    string argumentsString = Utility.String.SplitOffBracketedValue(ref st, '(', ')');
                    string[] arguments = argumentsString.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    int posPeriod = st.LastIndexOf('.');
                    if (posPeriod == -1)
                        throw new ApsimXException(this, "Bad operations action found: " + operation.Action);
                    string modelName = st.Substring(0, posPeriod);
                    string methodName = st.Substring(posPeriod+1).Replace(";", "").Trim();

                    Model model = Apsim.Get(this, modelName) as Model;
                    if (model == null)
                        throw new ApsimXException(this, "Cannot find model: " + modelName);
                    MethodInfo method = model.GetType().GetMethod(methodName);
                    if (method == null)
                        throw new ApsimXException(this, "Cannot find method: " + methodName + " in model: " + modelName);

                    // convert arguments to an object array.
                    ParameterInfo[] parameters = method.GetParameters();
                    object[] parameterValues = new object[parameters.Length];
                    for (int i = 0; i < parameterValues.Length; i++)
                    {
                        if (i >= arguments.Length)
                        {
                            // no more arguments were specified - use default value.
                            parameterValues[i] = parameters[i].DefaultValue;
                        }
                        else
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
                                parameterValues[argumentIndex] = Convert.ToDouble(value);
                            else if (parameters[argumentIndex].ParameterType == typeof(float))
                                parameterValues[argumentIndex] = Convert.ToSingle(value);
                            else if (parameters[argumentIndex].ParameterType == typeof(int))
                                parameterValues[argumentIndex] = Convert.ToInt32(value);
                            else if (parameters[argumentIndex].ParameterType == typeof(string))
                                parameterValues[argumentIndex] = value.Replace("\"", "").Trim();
                            else if (parameters[argumentIndex].ParameterType.IsEnum)
                            {
                                value = value.Trim();
                                int posLastPeriod = value.LastIndexOf('.');
                                if (posLastPeriod != -1)
                                    value = value.Substring(posLastPeriod+1);
                                parameterValues[argumentIndex] = Enum.Parse(parameters[argumentIndex].ParameterType, value);
                            }
                        }
                    }

                    // invoke method.
                    method.Invoke(model, parameterValues);

                }
            }
        }

       
    }
}
