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
        [NonSerialized]
        private List<Operation> PreviousSchedule = null;

        [NonSerialized]
        private Model _Model;

        // Parameter
        [XmlElement("Operation")]
        public List<Operation> Schedule { get; set; }

        //[XmlIgnore]
        //public Model Model { get; set;}

        /// <summary>
        /// We're about to be serialised. Remove our 'Script' model from the list
        /// of all models so that is isn't serialised. Seems .NET has a problem
        /// with serialising objects that have been compiled dynamically.
        /// </summary>
        public override void OnSerialising(bool xmlSerialisation)
        {
            if (_Model != null)
                Models.Remove(_Model);
        }

        /// <summary>
        /// Serialisation has completed. Readd our 'Script' model if necessary.
        /// </summary>
        public override void OnSerialised(bool xmlSerialisation)
        {
            if (_Model != null)
                Models.Add(_Model);
        }

        /// <summary>
        /// Simulation is commencing.
        /// </summary>
        public override void OnCommencing()
        {
            if (ScheduleHasChanged())
            {

                if (_Model != null)
                    RemoveModel(_Model);

                // Writes some c# code which then gets compiled to an assembly and added as a model.
                string classHeader = "using System;\r\n" +
                                     "using Models.Core;\r\n" +
                                     "using Models.PMF;\r\n" +
                                     "using Models.PMF.OldPlant;\r\n" +
                                     "using Models.Soils;\r\n" +
                                     "using Models.SurfaceOM;\r\n" +
                                     "namespace Models\r\n" +
                                     "{\r\n" +
                                     "   [Serializable]\r\n" +
                                     "   public class OperationsScript : Model\r\n" +
                                     "   {\r\n";

                string methodHeader = "      [EventSubscribe(\"StartOfDay\")]\r\n" +
                                      "      private void OnStartOfDay(object sender, EventArgs e)\r\n" +
                                      "      {\r\n";

                string methodFooter = "      }\r\n";

                string classFooter = "   }\r\n" +
                                     "}\r\n";

                StringWriter code = new StringWriter();
                code.Write(classHeader);

                // Loop though all operations and get a list of models the script will need to link to.
                List<string> linkNames = new List<string>();
                List<string> linkTypeNames = new List<string>();
                foreach (Operation operation in Schedule)
                {
                    Model modelToLinkTo = this.Find(operation.GetActionModel());
                    if (modelToLinkTo == null)
                        throw new ApsimXException(FullPath, "Cannot find model '" + operation.GetActionModel() +
                                                            "' as specified in operations schedule");
                    if (!linkNames.Contains(modelToLinkTo.Name))
                    {
                        linkNames.Add(modelToLinkTo.Name);
                        linkTypeNames.Add(modelToLinkTo.GetType().Name);
                    }
                }

                // write all links.
                code.WriteLine("      [Link] Clock Clock = null;\r\n");
                for (int i = 0; i < linkNames.Count; i++)
                    code.WriteLine("      [Link] " + linkTypeNames[i] + " " + linkNames[i] + " = null;\r\n");

                // write the start of day method.
                code.Write(methodHeader);
                foreach (Operation operation in Schedule)
                {
                    code.WriteLine("         if (Clock.Today == DateTime.Parse(\"" + operation.Date.ToString() + "\"))");
                    code.WriteLine("            " + operation.Action);
                }
                code.Write(methodFooter);

                // Write class footer 
                code.Write(classFooter);

                // Go look for our class name.
                Assembly CompiledAssembly = Utility.Reflection.CompileTextToAssembly(code.ToString(), null);
                Type ScriptType = CompiledAssembly.GetType("Models.OperationsScript");
                if (ScriptType == null)
                    throw new ApsimXException(FullPath, "Cannot find a public class called OperationsScript");

                _Model = Activator.CreateInstance(ScriptType) as Model;
                _Model.Name = "OperationsScript";
                AddModel(_Model);
                PreviousSchedule = Schedule;
            }
        }

        /// <summary>
        /// Return true if the schedule has been changed by the user.
        /// </summary>
        private bool ScheduleHasChanged()
        {
            if (PreviousSchedule == null)
                return true;
            if (PreviousSchedule.Count != Schedule.Count)
                return true;

            for (int i = 0; i < Schedule.Count; i++)
            {
                if (Schedule[i].Date != PreviousSchedule[i].Date ||
                    Schedule[i].Action != PreviousSchedule[i].Action)
                    return true;
            }
            return false;
        }


    }
}
