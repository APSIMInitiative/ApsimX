namespace UserInterface.Commands
{
    using global::UserInterface.Presenters;
    using Interfaces;
    using Models.Core;
    using Models.Core.ApsimFile;
    using System;

    /// <summary>This command moves a model from one Parent Node to another.</summary>
    class MoveModelCommand : ICommand
    {
        private Model fromModel;
        private Model toParent;
        private Model fromParent;
        private bool modelMoved;
        private string originalName;

        /// <summary>The node description.</summary>
        private TreeViewNode nodeDescription;

        /// <summary>The explorer presenter.</summary>
        private ExplorerPresenter presenter;

        /// <summary>
        /// The model which was changed by the command. This will be selected
        /// in the user interface when the command is undone/redone.
        /// </summary>
        public IModel AffectedModel => fromModel;

        /// <summary>Constructor.</summary>
        public MoveModelCommand(Model model, Model newParent, TreeViewNode treeNodeDescription, ExplorerPresenter explorerPresenter)
        {
            if (model.ReadOnly)
                throw new ApsimXException(model, string.Format("Unable to move {0} to {1} - {0} is read-only.", model.Name, newParent.Name));
            if (newParent.ReadOnly)
                throw new ApsimXException(newParent, string.Format("Unable to move {0} to {1} - {1} is read-only.", model.Name, newParent.Name));
            fromModel = model;
            toParent = newParent;
            nodeDescription = treeNodeDescription;
            presenter = explorerPresenter;
        }

        /// <summary>Perform the command.</summary>
        public void Do(CommandHistory commandHistory)
        {
            fromParent = fromModel.Parent as Model;
            
            // The Move method may rename the FromModel. Go get the original name in case of
            // Undo later.
            originalName = fromModel.Name;
            string originalPath = this.fromModel.FullPath;

            // Move model.
            try
            {
                Structure.Move(fromModel, toParent);
                presenter.Move(originalPath, toParent, nodeDescription);
                commandHistory.InvokeModelStructureChanged(fromParent);
                commandHistory.InvokeModelStructureChanged(toParent);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
                modelMoved = false;
            }
        }

        /// <summary>Undo the command.</summary>
        public void Undo(CommandHistory commandHistory)
        {
            if (modelMoved)
            {
                presenter.Move(this.fromModel.FullPath, fromParent, nodeDescription);
                Structure.Move(fromModel, fromParent);
                fromModel.Name = originalName;
                nodeDescription.Name = originalName;

                commandHistory.InvokeModelStructureChanged(fromParent);
                commandHistory.InvokeModelStructureChanged(toParent);
            }
        }

    }
}
