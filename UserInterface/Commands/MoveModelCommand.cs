using UserInterface.Views;
using Model.Core;
using System.Xml;
using System;

namespace UserInterface.Commands
{
    /// <summary>
    /// This command moves a model from one Parent Node to another.
    /// </summary>
    class MoveModelCommand : ICommand
    {
        private string FromParentPath;
        private IZone FromParentZone;
        private object FromModel;
        private string ToParentPath;
        private IZone ToParentZone;
        private bool ModelMoved;
        private string OriginalName;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MoveModelCommand(string FromParentPath, IZone FromParentZone, object FromModel,
                                string ToParentPath, IZone ToParentZone)
        {
            this.FromParentPath = FromParentPath;
            this.FromParentZone = FromParentZone;
            this.FromModel = FromModel;
            this.ToParentPath = ToParentPath;
            this.ToParentZone = ToParentZone;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            // Remove old model.
            ModelMoved = FromParentZone.Models.Remove(FromModel);

            // Add model to new parent.
            if (ModelMoved)
            {
                // The AddModel method may rename the FromModel. Go get the original name in case of
                // Undo later.
                OriginalName = Utility.Reflection.Name(FromModel);

                ToParentZone.AddModel(FromModel);
                CommandHistory.InvokeModelStructureChanged(FromParentPath);
                CommandHistory.InvokeModelStructureChanged(ToParentPath);
            }
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory CommandHistory)
        {
            if (ModelMoved)
            {
                ToParentZone.Models.Remove(FromModel);
                Utility.Reflection.SetName(FromModel, OriginalName);
                FromParentZone.AddModel(FromModel);

                CommandHistory.InvokeModelStructureChanged(FromParentPath);
                CommandHistory.InvokeModelStructureChanged(ToParentPath);
            }
        }

    }
}
