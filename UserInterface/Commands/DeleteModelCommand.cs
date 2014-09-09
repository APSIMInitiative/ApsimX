// -----------------------------------------------------------------------
// <copyright file="DeleteModelCommand.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Commands
{
    using Models.Core;

    /// <summary>
    /// This command deletes a model
    /// </summary>
    public class DeleteModelCommand : ICommand
    {
        /// <summary>
        /// The model to delete
        /// </summary>
        private IModel modelToDelete;

        /// <summary>
        /// Indicates whether the model was deleted successfully
        /// </summary>
        private bool modelWasRemoved;

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="modelToDelete">The model to delete</param>
        public DeleteModelCommand(Model modelToDelete)
        {
            this.modelToDelete = modelToDelete;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        /// <param name="commandHistory">The command history instance</param>
        public void Do(CommandHistory commandHistory)
        {
            Apsim api = Apsim.Create(this.modelToDelete.Parent);
            this.modelWasRemoved = api.Remove(this.modelToDelete);
            commandHistory.InvokeModelStructureChanged(api.FullPath);
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        /// <param name="commandHistory">The command history instance</param>
        public void Undo(CommandHistory commandHistory)
        {
            if (this.modelWasRemoved)
            {
                Apsim api = Apsim.Create(this.modelToDelete.Parent);
            
                api.Add(this.modelToDelete);
                commandHistory.InvokeModelStructureChanged(api.FullPath);
            }
        }
    }
}
