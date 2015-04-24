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
    using Interfaces;

    /// <summary>This command changes the 'CurrentNode' in the ExplorerView.</summary>
    public class AddModelCommand : ICommand
    {
        /// <summary>The parent model to add the model to.</summary>
        private IModel parent;

        /// <summary>The child model to add.</summary>
        private XmlNode child;

        /// <summary>The node description</summary>
        NodeDescriptionArgs nodeDescription;

        /// <summary>The explorer view</summary>
        IExplorerView explorerView;

        /// <summary>The model we're to add</summary>
        private IModel modelToAdd;

        /// <summary>True if model was added</summary>
        private bool modelAdded;

        /// <summary>Initializes a new instance of the <see cref="AddModelCommand"/> class.</summary>
        /// <param name="xmlOfModelToAdd">The XML of the model to add</param>
        /// <param name="toParent">The parent model to add the child to</param>
        public AddModelCommand(IModel parent, XmlNode child, NodeDescriptionArgs nodeDescription, IExplorerView explorerView)
        {
            this.parent = parent;
            this.child = child;
            this.nodeDescription = nodeDescription;
            this.explorerView = explorerView;
        }

        /// <summary>Perform the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Do(CommandHistory commandHistory)
        {
            this.modelToAdd = Apsim.Add(parent, child);

            // The add method above may have renamed the model to avoid a clash with the
            // name of an existing model so just in case, reset the name for the tree.
            nodeDescription.Name = this.modelToAdd.Name;

            this.explorerView.AddChild(Apsim.FullPath(parent), nodeDescription);

            this.modelAdded = true;
        }

        /// <summary>Undoes the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Undo(CommandHistory commandHistory)
        {
            if (this.modelAdded && this.modelToAdd != null)
            {
                parent.Children.Remove(this.modelToAdd as Model);
                this.explorerView.Delete(Apsim.FullPath(this.modelToAdd));
            }
        }
    }
}
