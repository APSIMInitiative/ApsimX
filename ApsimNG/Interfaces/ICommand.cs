using System;
using Models.Core;
using UserInterface.Interfaces;
using UserInterface.Presenters;

namespace UserInterface.Commands
{
    /// <summary>
    /// An interface for a command - something that the user interface can do and is
    /// undoable
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Perform the command.
        /// </summary>
        /// <remarks>
        /// This will update both the model and the UI.
        /// </remarks>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        void Do(ITreeView tree, Action<object> modelChanged);

        /// <summary>
        /// Undo a command.
        /// </summary>
        /// <remarks>
        /// This will update both the model and the UI.
        /// </remarks>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        void Undo(ITreeView tree, Action<object> modelChanged);

        /// <summary>
        /// The model which was changed by the command. This will be selected
        /// in the user interface when the command is undone/redone.
        /// </summary>
        IModel AffectedModel { get; }
    }
}
