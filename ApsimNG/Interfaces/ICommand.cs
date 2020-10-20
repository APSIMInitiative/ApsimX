using Models.Core;

namespace UserInterface.Commands
{
    /// <summary>
    /// An interface for a command - something that the user interface can do and is
    /// undoable
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Must be implemented to perform the command
        /// </summary>
        /// <param name="commandHistory">A reference to the parent command history</param>
        void Do(CommandHistory commandHistory);

        /// <summary>
        /// Must be implemented to undo a command.
        /// </summary>
        /// <param name="commandHistory">A reference to the parent command history</param>
        void Undo(CommandHistory commandHistory);

        /// <summary>
        /// The model which was changed by the command. This will be selected
        /// in the user interface when the command is undone/redone.
        /// </summary>
        IModel AffectedModel { get; }
    }
}
