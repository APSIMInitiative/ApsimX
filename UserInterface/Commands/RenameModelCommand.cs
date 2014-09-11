
using Models.Core;
namespace UserInterface.Commands
{
    class RenameModelCommand : ICommand
    {
        private Model modelToRename;
        private string ParentModelPath;
        private string NewName;
        private string OriginalName;

        public RenameModelCommand(Model modelToRename, string ParentModelPath, string NewName)
        {
            this.modelToRename = modelToRename;
            this.ParentModelPath = ParentModelPath;
            this.NewName = NewName;
        }

        public void Do(CommandHistory CommandHistory)
        {
            // Get original value of property so that we can restore it in Undo if needed.
            OriginalName = Utility.Reflection.Name(modelToRename);

            // Set the new name.
            this.modelToRename.Name = NewName;
            Apsim.EnsureNameIsUnique(this.modelToRename);
            CommandHistory.InvokeModelStructureChanged(this.modelToRename.Parent);
        }

        public void Undo(CommandHistory CommandHistory)
        {
            Utility.Reflection.SetName(modelToRename, OriginalName);
            CommandHistory.InvokeModelStructureChanged(this.modelToRename.Parent);
        }
    }
}
