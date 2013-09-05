using UserInterface.Views;
using Model.Core;
using System.Xml;

namespace UserInterface.Commands
{
    /// <summary>
    /// This command changes the 'CurrentNode' in the ExplorerView.
    /// </summary>
    class AddModelCommand : ICommand
    {
        private object Parent;
        private string ChildXml;
        private object NewlyCreatedObject;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AddModelCommand(object ParentZone, string ChildXml)
        {
            this.Parent = ParentZone;
            this.ChildXml = ChildXml;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public object Do()
        {
            XmlDocument Doc = new XmlDocument();
            Doc.LoadXml(ChildXml);
            NewlyCreatedObject = Utility.Xml.Deserialise(Doc.DocumentElement);
            if (NewlyCreatedObject != null)
            {
                if (Parent is Zone)
                    (Parent as Zone).Models.Add(NewlyCreatedObject);
                else
                    NewlyCreatedObject = null;
            }
            if (NewlyCreatedObject != null)
                return Parent;
            else
                return null;
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public object Undo()
        {
            if (NewlyCreatedObject != null)
            {
                if (Parent is Zone)
                    (Parent as IZone).Models.Remove(NewlyCreatedObject);
            }
            return null;
        }

    }
}
