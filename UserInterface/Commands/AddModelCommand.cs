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

                // ensure the simulations have all the events connected and links resolved
                Model sims = FromModel;
                while ((sims != null) && !(sims is Simulations))
                    sims = (Model)sims.Parent;

                // initialise the simulation
                Model sim = FromModel;
                while ((sim != null) && !(sim is Simulation))
                    sim = (Model)sim.Parent;

                if (sim != null)
                {
                    sim.OnLoaded();
                    sim.OnCommencing();
                }

                ModelAdded = true;
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
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
