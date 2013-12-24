using UserInterface.Views;
using Models.Core;
using System.Xml;
using System;

namespace UserInterface.Commands
{
    /// <summary>
    /// This command changes the 'CurrentNode' in the ExplorerView.
    /// </summary>
    class AddModelCommand : ICommand
    {
        private string FromModelXml;
        private ModelCollection ToParent;
        private Model FromModel;
        private bool ModelAdded;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AddModelCommand(string FromModelXml, ModelCollection ToParent)
        {
            this.FromModelXml = FromModelXml;
            this.ToParent = ToParent;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            try
            {
                XmlDocument Doc = new XmlDocument();
                Doc.LoadXml(FromModelXml);
                FromModel = Utility.Xml.Deserialise(Doc.DocumentElement) as Model;
                FromModel.Parent = ToParent;
                ToParent.AddModel(FromModel);
                CommandHistory.InvokeModelStructureChanged(ToParent.FullPath);

                // need to resolve all the links and event handlers for the pasted simulation/model
                Utility.ModelFunctions.ResolveLinks((Model)FromModel);

                // ensure the simulation has all the events connected and models initialised
                Model sim = FromModel;
                while ((sim != null) && !(sim is Simulation))
                    sim = (Model)sim.Parent;

                if (sim != null)
                {
                    Utility.ModelFunctions.ConnectEventsInAllModels(sim);
                    ((Simulation)sim).Initialise();
                }

                ModelAdded = true;
            }
            catch (Exception)
            {
                ModelAdded = false;
            }

        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory CommandHistory)
        {
            if (ModelAdded && FromModel != null)
            {
                ToParent.RemoveModel(FromModel);
                CommandHistory.InvokeModelStructureChanged(ToParent.FullPath);
            }
        }

    }
}
