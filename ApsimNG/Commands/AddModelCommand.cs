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
        private string childString;

        /// <summary>The explorer view.</summary>
        IExplorerView view;

        /// <summary>The explorer presenter.</summary>
        ExplorerPresenter presenter;

        /// <summary>The model we're to add.</summary>
        private IModel modelToAdd;

        /// <summary>True if model was added.</summary>
        private bool modelAdded;

        /// <summary>Constructor.</summary>
        /// <param name="pathOfParent">The path of the parent model to add the child to.</param>
        /// <param name="childStringToAdd">The string representation of the model to add.</param>
        /// <param name="explorerView">The explorer view to work with.</param>
        /// <param name="explorerPresenter">The explorer presenter to work with.</param>
        public AddModelCommand(string pathOfParent, string childStringToAdd, IExplorerView explorerView, ExplorerPresenter explorerPresenter)
        {
            parentPath = pathOfParent;
            childString = childStringToAdd;
            view = explorerView;
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

                IModel newModel = FileFormat.ReadFromString<IModel>(childString, out List<Exception> exceptions);
                if (exceptions != null && exceptions.Count > 0)
                {
                    presenter.MainPresenter.ShowError(exceptions);
                    return;
                }
                modelToAdd = newModel;

                if (modelToAdd is Simulations && modelToAdd.Children.Count == 1)
                    modelToAdd = modelToAdd.Children[0];

                Structure.Add(modelToAdd, parent);
                var nodeDescription = presenter.GetNodeDescription(modelToAdd);
                view.Tree.AddChild(Apsim.FullPath(parent), nodeDescription);
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
                view.Tree.Delete(Apsim.FullPath(modelToAdd));
            }
        }
    }
}
