using UserInterface.Interfaces;
using Models.Core;

namespace UserInterface.Commands
{
    /// <summary>
    /// This command moves a model from one Parent Node to another.
    /// </summary>
    class MoveModelCommand : ICommand
    {
        Model FromModel;
        Model ToParent;
        Model FromParent;
        private bool ModelMoved;
        private string OriginalName;

        /// <summary>The node description</summary>
        TreeViewNode nodeDescription;

        /// <summary>The explorer view</summary>
        IExplorerView explorerView;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MoveModelCommand(Model FromModel, Model ToParent, TreeViewNode nodeDescription, IExplorerView explorerView)
        {
            if (FromModel.ReadOnly)
                throw new ApsimXException(FromModel, string.Format("Unable to move {0} to {1} - {0} is read-only.", FromModel.Name, ToParent.Name));
            if (ToParent.ReadOnly)
                throw new ApsimXException(ToParent, string.Format("Unable to move {0} to {1} - {1} is read-only.", FromModel.Name, ToParent.Name));
            this.FromModel = FromModel;
            this.ToParent = ToParent;
            this.nodeDescription = nodeDescription;
            this.explorerView = explorerView;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            FromParent = FromModel.Parent as Model;
            
            // Remove old model.
            ModelMoved = FromParent.Children.Remove(FromModel);

            // Add model to new parent.
            if (ModelMoved)
            {
                this.explorerView.Tree.Delete(Apsim.FullPath(this.FromModel));
                // The AddModel method may rename the FromModel. Go get the original name in case of
                // Undo later.
                OriginalName = FromModel.Name;

                ToParent.Children.Add(FromModel);
                FromModel.Parent = ToParent;
                Apsim.EnsureNameIsUnique(FromModel);
                nodeDescription.Name = FromModel.Name;
                this.explorerView.Tree.AddChild(Apsim.FullPath(ToParent), nodeDescription);
                CommandHistory.InvokeModelStructureChanged(FromParent);
                CommandHistory.InvokeModelStructureChanged(ToParent);
            }
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory CommandHistory)
        {
            if (ModelMoved)
            {
                ToParent.Children.Remove(FromModel);
                this.explorerView.Tree.Delete(Apsim.FullPath(this.FromModel));
                FromModel.Name = OriginalName;
                nodeDescription.Name = OriginalName;
                FromParent.Children.Add(FromModel);
                FromModel.Parent = FromParent;
                this.explorerView.Tree.AddChild(Apsim.FullPath(FromParent), nodeDescription);

                CommandHistory.InvokeModelStructureChanged(FromParent);
                CommandHistory.InvokeModelStructureChanged(ToParent);
            }
        }

    }
}
