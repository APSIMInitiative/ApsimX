using UserInterface.Views;
using Model.Core;
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
        private string ToParentPath;
        private IZone ToParentZone;
        private object FromModel;
        private bool ModelAdded;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AddModelCommand(string FromModelXml, string ToParentPath, IZone ToParentZone)
        {
            this.FromModelXml = FromModelXml;
            this.ToParentPath = ToParentPath;
            this.ToParentZone = ToParentZone;
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
                FromModel = Utility.Xml.Deserialise(Doc.DocumentElement);
                ToParentZone.AddModel(FromModel);
                CommandHistory.InvokeModelStructureChanged(ToParentPath);
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
                ToParentZone.Models.Remove(FromModel);
                CommandHistory.InvokeModelStructureChanged(ToParentPath);
            }
        }

    }
}
