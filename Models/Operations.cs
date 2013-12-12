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
    public class Operations : Model
    {
        // Links
        [Link] Simulations Simulations = null;

        // Parameter
        [XmlElement("Operation")]
        public List<Operation> Schedule { get; set; }

        [XmlIgnore]
        public Model Model { get; set;}


        [EventSubscribe("Initialised")]
        private void OnInitialised(object sender, EventArgs e)
        {
            if (Model != null)
                RemoveModel(Model);
            // Writes some c# code which then gets compiled to an assembly and added as a model.
            string classHeader = "using System;\r\n" +
                                 "using Models.Core;\r\n" +
                                 "using Models.PMF;\r\n" +
                                 "using Models.Soils;\r\n" +
                                 "namespace Models\r\n" +
                                 "{\r\n" +
                                 "   public class OperationsScript : Model\r\n" +
                                 "   {\r\n";

            string methodHeader = "      [EventSubscribe(\"StartOfDay\")]\r\n" +
                                  "      private void OnStartOfDay(object sender, EventArgs e)\r\n" +
                                  "      {\r\n";

            string methodFooter =  "      }\r\n";

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
            string assemblyFileName = Path.Combine(Path.GetDirectoryName(Simulations.FileName),
                                       Name) + ".dll";
            Assembly CompiledAssembly = Utility.Reflection.CompileTextToAssembly(code.ToString(), assemblyFileName);
            Type ScriptType = CompiledAssembly.GetType("Models.OperationsScript");
            if (ScriptType == null)
                throw new ApsimXException(FullPath, "Cannot find a public class called OperationsScript");

            Model = Activator.CreateInstance(ScriptType) as Model;
            Model.Name = "OperationsScript";
            AddModel(Model);
        }


    }
}
