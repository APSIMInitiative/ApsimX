namespace UserInterface.Commands
{
    using System;
    using Interfaces;
    using Models.Core;

    /// <summary>
    /// This command moves a model up or down one spot in the siblings
    /// </summary>
    public class MoveModelUpDownCommand : ICommand
    {
        /// <summary>The model to move</summary>
        private IModel modelToMove;

        /// <summary>The move up</summary>
        public bool MoveUp { get; private set; }

        /// <summary>The model was moved</summary>
        public bool ModelWasMoved { get; private set; }

        /// <summary>
        /// The model which was changed by the command. This will be selected
        /// in the user interface when the command is undone/redone.
        /// </summary>
        public IModel AffectedModel => modelToMove;

        /// <summary>Constructor.</summary>
        /// <param name="modelToMove">The model to move.</param>
        /// <param name="up">if set to <c>true</c> [up].</param>
        public MoveModelUpDownCommand(IModel modelToMove, bool up)
        {
            if (modelToMove.ReadOnly)
                throw new ApsimXException(modelToMove, string.Format("Unable to move {0} - it is read-only.", modelToMove.Name));
            this.modelToMove = modelToMove;
            this.MoveUp = up;
        }

        /// <summary>Perform the command</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Do(ITreeView tree, Action<object> modelChanged)
        {
            IModel parent = modelToMove.Parent as IModel;

            int modelIndex = parent.Children.IndexOf(modelToMove as Model);

            ModelWasMoved = false;
            if (MoveUp)
            {
                if (modelIndex != 0)
                    MoveModelUp(parent, modelIndex, tree);
            }
            else
            {
                if (modelIndex != parent.Children.Count - 1)
                    MoveModelDown(parent, modelIndex, tree);
            }
            tree.SelectedNode = modelToMove.FullPath;
        }

        /// <summary>Undo the command</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Undo(ITreeView tree, Action<object> modelChanged)
        {
            if (ModelWasMoved)
            {
                Model parent = modelToMove.Parent as Model;
                int modelIndex = parent.Children.IndexOf(modelToMove as Model);
                if (MoveUp)
                    MoveModelDown(parent, modelIndex, tree);
                else
                    MoveModelUp(parent, modelIndex, tree);
                tree.SelectedNode = modelToMove.FullPath;
            }
        }

        /// <summary>Moves the model down.</summary>
        /// <param name="parent">The parent.</param>
        /// <param name="modelIndex">Index of the model.</param>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        private void MoveModelDown(IModel parent, int modelIndex, ITreeView tree)
        {
            parent.Children.Remove(modelToMove as Model);
            parent.Children.Insert(modelIndex + 1, modelToMove as Model);
            ModelWasMoved = true;
            tree.MoveDown(modelToMove.FullPath);
        }

        /// <summary>Moves the model up.</summary>
        /// <param name="parent">The parent.</param>
        /// <param name="modelIndex">Index of the model.</param>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        private void MoveModelUp(IModel parent, int modelIndex, ITreeView tree)
        {
            parent.Children.Remove(modelToMove as Model);
            parent.Children.Insert(modelIndex - 1, modelToMove as Model);
            ModelWasMoved = true;
            tree.MoveUp(modelToMove.FullPath);
        }
    }
}
