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
    using APSIM.Shared.Utilities;

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
                    soilNode = XmlUtilities.FindByType(newDoc.DocumentElement, "Soil");
                    if (XmlUtilities.FindByType(soilNode, "Sample") == null &&
                        XmlUtilities.FindByType(soilNode, "InitialWater") == null)
                    {
                        // Add in an initial water and initial conditions models.
                        XmlNode initialWater = soilNode.AppendChild(soilNode.OwnerDocument.CreateElement("InitialWater"));
                        XmlUtilities.SetValue(initialWater, "Name", "Initial water");
                        XmlUtilities.SetValue(initialWater, "PercentMethod", "FilledFromTop");
                        XmlUtilities.SetValue(initialWater, "FractionFull", "1");
                        XmlUtilities.SetValue(initialWater, "DepthWetSoil", "NaN");
                        XmlNode initialConditions = soilNode.AppendChild(soilNode.OwnerDocument.CreateElement("Sample"));
                        XmlUtilities.SetValue(initialConditions, "Name", "Initial conditions");
                        XmlUtilities.SetValue(initialConditions, "Thickness/double", "1800");
                        XmlUtilities.SetValue(initialConditions, "NO3/double", "10");
                        XmlUtilities.SetValue(initialConditions, "NH4/double", "1");
                        XmlUtilities.SetValue(initialConditions, "NO3Units", "kgha");
                        XmlUtilities.SetValue(initialConditions, "NH4Units", "kgha");
                        XmlUtilities.SetValue(initialConditions, "SWUnits", "Volumetric");
                    }
                }

                this.modelToAdd = Apsim.Add(this.toParent, soilNode) as Model;

                commandHistory.InvokeModelStructureChanged(this.toParent);

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
