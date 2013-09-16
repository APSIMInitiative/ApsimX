
namespace UserInterface.Commands
{
    class RenameModelCommand : ICommand
    {
        private object Model;
        private string ParentModelPath;
        private string NewName;
        private string OriginalName;

        public RenameModelCommand(object Model, string ParentModelPath, string NewName)
        {
            this.Model = Model;
            this.ParentModelPath = ParentModelPath;
            this.NewName = NewName;
        }

        public void Do(CommandHistory CommandHistory)
        {
            // Get original value of property so that we can restore it in Undo if needed.
            OriginalName = Utility.Reflection.Name(Model);

            // Set the new name.
            Utility.Reflection.SetName(Model, NewName);
            CommandHistory.InvokeModelStructureChanged(ParentModelPath);
        }

        public void Undo(CommandHistory CommandHistory)
        {
            Utility.Reflection.SetName(Model, OriginalName);
            CommandHistory.InvokeModelStructureChanged(ParentModelPath);
        }
    }
}
