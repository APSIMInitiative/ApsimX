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
        Model ToParent;
        private bool ModelMoved;
        private string OriginalName;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MoveModelCommand(Model FromModel, Model ToParent)
        {
            this.FromModel = FromModel;
            this.ToParent = ToParent;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            Model FromParent = FromModel.Parent as Model;
            
            // Remove old model.
            ModelMoved = FromParent.Children.Remove(FromModel);

            // Add model to new parent.
            if (ModelMoved)
            {
                // The AddModel method may rename the FromModel. Go get the original name in case of
                // Undo later.
                OriginalName = FromModel.Name;

                ToParent.Children.Add(FromModel);
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
                Model FromParent = FromModel.Parent as Model;

                ToParent.Children.Remove(FromModel);
                FromModel.Name = OriginalName;
                FromParent.Children.Add(FromModel);

                CommandHistory.InvokeModelStructureChanged(FromParent.FullPath);
                CommandHistory.InvokeModelStructureChanged(ToParent.FullPath);
            }
        }

    }
}
