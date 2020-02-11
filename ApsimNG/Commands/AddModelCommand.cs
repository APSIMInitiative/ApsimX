namespace UserInterface.Commands
{
    using global::UserInterface.Presenters;
    using Interfaces;
    using Models.Core;
    using Models.Core.ApsimFile;
    using System;
    using System.Collections.Generic;

    /// <summary>This command changes the 'CurrentNode' in the ExplorerView.</summary>
    public class AddModelCommand : ICommand
    {
        /// <summary>The parent model to add the model to.</summary>
        private IModel parent;

        /// <summary>The path of the parent to add the model to.</summary>
        private string parentPath;

        /// <summary>A string representation of the child model to add.</summary>
        private IModel child;

        /// <summary>A string representation of the child model to add.</summary>
        private string xmlOrJson;

        /// <summary>The explorer presenter.</summary>
        ExplorerPresenter presenter;

        /// <summary>The model we're to add.</summary>
        private IModel modelToAdd;

        /// <summary>True if model was added.</summary>
        private bool modelAdded;

        /// <summary>Constructor.</summary>
        /// <param name="pathOfParent">The path of the parent model to add the child to.</param>
        /// <param name="child">The model to add.</param>
        /// <param name="explorerView">The explorer view to work with.</param>
        /// <param name="explorerPresenter">The explorer presenter to work with.</param>
        public AddModelCommand(string pathOfParent, IModel child, ExplorerPresenter explorerPresenter)
        {
            parentPath = pathOfParent;
            this.child = child;
            presenter = explorerPresenter;
        }

        /// <summary>Constructor.</summary>
        /// <param name="pathOfParent">The path of the parent model to add the child to.</param>
        /// <param name="textToAdd">The text string representation of the model to add.</param>
        /// <param name="explorerPresenter">The explorer presenter to work with.</param>
        public AddModelCommand(string pathOfParent, string textToAdd, ExplorerPresenter explorerPresenter)
        {
            parentPath = pathOfParent;
            xmlOrJson = textToAdd;
            presenter = explorerPresenter;
        }

        /// <summary>Perform the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Do(CommandHistory commandHistory)
        {
            try
            {
                parent = Apsim.Get(presenter.ApsimXFile, parentPath) as IModel;
                if (parent == null)
                    throw new Exception("Cannot find model " + parentPath);

                if (xmlOrJson != null)
                    modelToAdd = Structure.Add(xmlOrJson, parent);
                else
                    modelToAdd = Structure.Add(child, parent);

                presenter.AddChildToTree(parentPath, modelToAdd);
                modelAdded = true;
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
                modelAdded = false;
            }
        }

        /// <summary>Undoes the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Undo(CommandHistory commandHistory)
        {
            if (modelAdded && modelToAdd != null)
            {
                parent.Children.Remove(modelToAdd as Model);
                presenter.DeleteFromTree(Apsim.FullPath(modelToAdd));
            }
        }
    }
}
