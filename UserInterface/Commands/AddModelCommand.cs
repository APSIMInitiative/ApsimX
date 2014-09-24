using UserInterface.Views;
using Models.Core;
using System.Xml;
using System;
using System.Collections.Generic;

namespace UserInterface.Commands
{
    /// <summary>
    /// This command changes the 'CurrentNode' in the ExplorerView.
    /// </summary>
    class AddModelCommand : ICommand
    {
        private string FromModelXml;
        private Model ToParent;
        private Model FromModel;
        private bool ModelAdded;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AddModelCommand(string FromModelXml, Model ToParent)
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
                Apsim.ParentAllChildren(FromModel);

                ToParent.Children.Add(FromModel);
                Apsim.EnsureNameIsUnique(FromModel);
                CommandHistory.InvokeModelStructureChanged(ToParent);

                // Call OnLoaded
                Apsim.CallEventHandler(FromModel, "Loaded", null);

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
                ToParent.Children.Remove(FromModel);
                CommandHistory.InvokeModelStructureChanged(ToParent);
            }
        }

    }
}
