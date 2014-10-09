// -----------------------------------------------------------------------
// <copyright file="AddModelCommand.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Commands
{
    using System;
    using System.Xml;
    using Importer;
    using Models.Core;

    /// <summary>
    /// This command changes the 'CurrentNode' in the ExplorerView.
    /// </summary>
    public class AddModelCommand : ICommand
    {
        /// <summary>The XML of the model we're to add</summary>
        private string modelXmlToAdd;

        /// <summary>The parent model to add the model to</summary>
        private Model toParent;

        /// <summary>The model we're to add</summary>
        private Model modelToAdd;

        /// <summary>True if model was added</summary>
        private bool modelAdded;

        /// <summary>Initializes a new instance of the <see cref="AddModelCommand"/> class.</summary>
        /// <param name="xmlOfModelToAdd">The XML of the model to add</param>
        /// <param name="toParent">The parent model to add the child to</param>
        public AddModelCommand(string xmlOfModelToAdd, Model toParent)
        {
            this.modelXmlToAdd = xmlOfModelToAdd;
            this.toParent = toParent;
        }

        /// <summary>Perform the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Do(CommandHistory commandHistory)
        {
            try
            {
                // Load the model xml
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(this.modelXmlToAdd);
                XmlNode soilNode = doc.DocumentElement;

                // If the model xml is a soil object then try and convert from old
                // APSIM format to new.
                if (doc.DocumentElement.Name == "Soil")
                {
                    XmlDocument newDoc = new XmlDocument();
                    newDoc.AppendChild(newDoc.CreateElement("D"));
                    APSIMImporter importer = new APSIMImporter();
                    importer.ImportSoil(doc.DocumentElement, newDoc.DocumentElement, newDoc.DocumentElement);
                    soilNode = Utility.Xml.FindByType(newDoc.DocumentElement, "Soil");
                }

                this.modelToAdd = Utility.Xml.Deserialise(soilNode) as Model;
                this.modelToAdd.Parent = this.toParent;
                Apsim.ParentAllChildren(this.modelToAdd);

                this.toParent.Children.Add(this.modelToAdd);
                Apsim.EnsureNameIsUnique(this.modelToAdd);
                commandHistory.InvokeModelStructureChanged(this.toParent);

                // Call OnLoaded
                Apsim.CallEventHandler(this.modelToAdd, "Loaded", null);

                this.modelAdded = true;
            }
            catch (Exception)
            {
                this.modelAdded = false;
            }
        }

        /// <summary>Undoes the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Undo(CommandHistory commandHistory)
        {
            if (this.modelAdded && this.modelToAdd != null)
            {
                this.toParent.Children.Remove(this.modelToAdd);
                commandHistory.InvokeModelStructureChanged(this.toParent);
            }
        }
    }
}
