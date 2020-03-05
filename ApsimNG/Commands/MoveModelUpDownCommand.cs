namespace UserInterface.Commands
{
    using Interfaces;
    using Models.Core;

    /// <summary>
    /// This command moves a model up or down one spot in the siblings
    /// </summary>
    public class MoveModelUpDownCommand : ICommand
    {
        /// <summary>The explorer view</summary>
        private IExplorerView explorerView;

        /// <summary>The model to move</summary>
        private IModel modelToMove;

        /// <summary>The move up</summary>
        private bool moveUp;

        /// <summary>The model was moved</summary>
        private bool modelWasMoved;

        /// <summary>Constructor.</summary>
        /// <param name="explorerView">The explorer view.</param>
        /// <param name="modelToMove">The model to move.</param>
        /// <param name="up">if set to <c>true</c> [up].</param>
        public MoveModelUpDownCommand(IModel modelToMove, bool up, IExplorerView explorerView)
        {
            if (modelToMove.ReadOnly)
                throw new ApsimXException(modelToMove, string.Format("Unable to move {0} - it is read-only.", modelToMove.Name));
            this.modelToMove = modelToMove;
            this.moveUp = up;
            this.explorerView = explorerView;            
        }

        /// <summary>Perform the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Do(CommandHistory commandHistory)
        {
            IModel parent = modelToMove.Parent as IModel;

            int modelIndex = parent.Children.IndexOf(modelToMove as Model);

            modelWasMoved = false;
            if (moveUp)
            {
                if (modelIndex != 0)
                    MoveModelUp(commandHistory, parent, modelIndex);
            }
            else
            {
                if (modelIndex != parent.Children.Count - 1)
                    MoveModelDown(commandHistory, parent, modelIndex);
            }
        }

        /// <summary>Undo the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Undo(CommandHistory commandHistory)
        {
            if (modelWasMoved)
            {
                Model parent = modelToMove.Parent as Model;
                int modelIndex = parent.Children.IndexOf(modelToMove as Model);
                if (moveUp)
                    MoveModelDown(commandHistory, parent, modelIndex);
                else
                    MoveModelUp(commandHistory, parent, modelIndex);
            }
        }


        /// <summary>Moves the model down.</summary>
        /// <param name="commandHistory">The command history.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="modelIndex">Index of the model.</param>
        private void MoveModelDown(CommandHistory commandHistory, IModel parent, int modelIndex)
        {
            if (explorerView != null)
                explorerView.Tree.MoveDown(Apsim.FullPath(modelToMove));
            parent.Children.Remove(modelToMove as Model);
            parent.Children.Insert(modelIndex + 1, modelToMove as Model);
            modelWasMoved = true;
        }

        /// <summary>Moves the model up.</summary>
        /// <param name="commandHistory">The command history.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="modelIndex">Index of the model.</param>
        private void MoveModelUp(CommandHistory commandHistory, IModel parent, int modelIndex)
        {
            if (explorerView != null)
                explorerView.Tree.MoveUp(Apsim.FullPath(modelToMove));
            parent.Children.Remove(modelToMove as Model);
            parent.Children.Insert(modelIndex - 1, modelToMove as Model);
            modelWasMoved = true;
        }

    }
}
