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
    [ViewName("UserInterface.Views.OperationsView")]
    [PresenterName("UserInterface.Presenters.OperationsPresenter")]
    public class Operations : ModelCollection
    {
        [Link] Clock Clock = null;

        
        [XmlElement("Operation")]
        public List<Operation> Schedule { get; set; }

        /// <summary>
        /// Simulation is commencing.
        /// </summary>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            foreach (Operation operation in Schedule)
            {
                if (operation.Date == Clock.Today)
                {
                    string st = operation.Action;
                    string argumentsString = Utility.String.SplitOffBracketedValue(ref st, '(', ')');
                    string[] arguments = argumentsString.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    int posPeriod = st.IndexOf('.');
                    if (posPeriod == -1)
                        throw new ApsimXException(FullPath, "Bad operations action found: " + operation.Action);
                    string modelName = st.Substring(0, posPeriod);
                    string methodName = st.Substring(posPeriod+1).Replace(";", "").Trim();

                    Model model = Find(modelName);
                    if (model == null)
                        throw new ApsimXException(FullPath, "Cannot find model: " + modelName);
                    MethodInfo method = model.GetType().GetMethod(methodName);
                    if (method == null)
                        throw new ApsimXException(FullPath, "Cannot find method: " + methodName + " in model: " + modelName);

                    // convert arguments to an object array.
                    ParameterInfo[] parameters = method.GetParameters();
                    object[] args = new object[parameters.Length];
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
                                throw new ApsimXException(FullPath, "Cannot find argument: " + argumentName + " in operation call: " + operation.Action);
                            value = value.Substring(posColon + 1);
                        }

                        // convert value to correct type.
                        if (parameters[argumentIndex].ParameterType == typeof(double))
                            args[argumentIndex] = Convert.ToDouble(value);
                        else if (parameters[argumentIndex].ParameterType == typeof(float))
                            args[argumentIndex] = Convert.ToSingle(value);
                        else if (parameters[argumentIndex].ParameterType == typeof(int))
                            args[argumentIndex] = Convert.ToInt32(value);
                        else if (parameters[argumentIndex].ParameterType == typeof(string))
                            args[argumentIndex] = value.Replace("\"", "").Trim();
                    }

                    // invoke method.
                    method.Invoke(model, args);

                }
            }


            //if (ScheduleHasChanged())
            //{

            //    if (_Model != null)
            //        RemoveModel(_Model);

            //    // Writes some c# code which then gets compiled to an assembly and added as a model.
            //    string classHeader = "using System;\r\n" +
            //                         "using Models.Core;\r\n" +
            //                         "using Models.PMF;\r\n" +
            //                         "using Models.PMF.OldPlant;\r\n" +
            //                         "using Models.Soils;\r\n" +
            //                         "using Models.SurfaceOM;\r\n" +
            //                         "namespace Models\r\n" +
            //                         "{\r\n" +
            //                         "   [Serializable]\r\n" +
            //                         "   public class OperationsScript : Model\r\n" +
            //                         "   {\r\n";

            //    string methodHeader = "      [EventSubscribe(\"StartOfDay\")]\r\n" +
            //                          "      private void OnStartOfDay(object sender, EventArgs e)\r\n" +
            //                          "      {\r\n";

            //    string methodFooter = "      }\r\n";

            //    string classFooter = "   }\r\n" +
            //                         "}\r\n";

            //    StringWriter code = new StringWriter();
            //    code.Write(classHeader);

            //    // Loop though all operations and get a list of models the script will need to link to.
            //    List<string> linkNames = new List<string>();
            //    List<string> linkTypeNames = new List<string>();
            //    foreach (Operation operation in Schedule)
            //    {
            //        Model modelToLinkTo = this.Find(operation.GetActionModel().Trim());
            //        if (modelToLinkTo == null)
            //            throw new ApsimXException(FullPath, "Cannot find model '" + operation.GetActionModel() +
            //                                                "' as specified in operations schedule");
            //        if (!linkNames.Contains(modelToLinkTo.Name.Trim()))
            //        {
            //            linkNames.Add(modelToLinkTo.Name.Trim());
            //            linkTypeNames.Add(modelToLinkTo.GetType().Name);
            //        }
            //    }

            //    // write all links.
            //    code.WriteLine("      [Link] Clock Clock = null;\r\n");
            //    for (int i = 0; i < linkNames.Count; i++)
            //        code.WriteLine("      [Link] " + linkTypeNames[i] + " " + linkNames[i] + " = null;\r\n");

            //    // write the start of day method.
            //    code.Write(methodHeader);
            //    foreach (Operation operation in Schedule)
            //    {
            //        code.WriteLine("         if (Clock.Today == DateTime.Parse(\"" + operation.Date.ToString() + "\"))");
            //        code.WriteLine("            " + operation.Action);
            //    }
            //    code.Write(methodFooter);

            //    // Write class footer 
            //    code.Write(classFooter);

            //    // Go look for our class name.
            //    Assembly CompiledAssembly = Utility.Reflection.CompileTextToAssembly(code.ToString(), null);
            //    Type ScriptType = CompiledAssembly.GetType("Models.OperationsScript");
            //    if (ScriptType == null)
            //        throw new ApsimXException(FullPath, "Cannot find a public class called OperationsScript");

            //    _Model = Activator.CreateInstance(ScriptType) as Model;
            //    _Model.Name = "OperationsScript";
            //    AddModel(_Model);
            //    PreviousSchedule = Schedule;
            //}
        }

        /// <summary>
        /// Return true if the schedule has been changed by the user.
        /// </summary>
        //private bool ScheduleHasChanged()
        //{
        //    if (PreviousSchedule == null)
        //        return true;
        //    if (PreviousSchedule.Count != Schedule.Count)
        //        return true;

        //    for (int i = 0; i < Schedule.Count; i++)
        //    {
        //        if (Schedule[i].Date != PreviousSchedule[i].Date ||
        //            Schedule[i].Action != PreviousSchedule[i].Action)
        //            return true;
        //    }
        //    return false;
        //}


    }
}
