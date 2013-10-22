using UserInterface.Views;
using Models.Core;
using System.Xml;
using System;

namespace UserInterface.Commands
{
    /// <summary>
    /// This command moves a model from one Parent Node to another.
    /// </summary>
    class MoveModelCommand : ICommand
    {
        Model FromModel;
        ModelCollection ToParent;
        private bool ModelMoved;
        private string OriginalName;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MoveModelCommand(Model FromModel, ModelCollection ToParent)
        {
            this.FromModel = FromModel;
            this.ToParent = ToParent;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            ModelCollection FromParent = FromModel.Parent as ModelCollection;
            
            // Remove old model.
            ModelMoved = FromParent.RemoveModel(FromModel);

            // Add model to new parent.
            if (ModelMoved)
            {
                // The AddModel method may rename the FromModel. Go get the original name in case of
                // Undo later.
                OriginalName = FromModel.Name;

                ToParent.AddModel(FromModel, true);
                CommandHistory.InvokeModelStructureChanged(FromParent.FullPath);
                CommandHistory.InvokeModelStructureChanged(ToParent.FullPath);
            }
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory CommandHistory)
        {
            if (ModelMoved)
            {
                ModelCollection FromParent = FromModel.Parent as ModelCollection;
            
                ToParent.RemoveModel(FromModel);
                FromModel.Name = OriginalName;
                FromParent.AddModel(FromModel, true);

                CommandHistory.InvokeModelStructureChanged(FromParent.FullPath);
                CommandHistory.InvokeModelStructureChanged(ToParent.FullPath);
            }
        }

    }
}
