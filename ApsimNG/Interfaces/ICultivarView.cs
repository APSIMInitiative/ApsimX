namespace UserInterface.Interfaces
{
    using System;
    using EventArguments;

    /// <summary>
    /// An interface for a cultivar view.
    /// </summary>
    public interface ICultivarView
    {
        /// <summary>
        /// Invoked when the aliases have changed.
        /// </summary>
        event EventHandler AliasesChanged;

        /// <summary>
        /// Invoked when the commands have changed.
        /// </summary>
        event EventHandler CommandsChanged;

        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.')
        /// </summary>
        event EventHandler<NeedContextItemsArgs> ContextItemsNeeded; 

        /// <summary>
        /// Gets or sets a list of all aliases.
        /// </summary>
        string[] Aliases { get; set; }

        /// <summary>
        /// Gets or sets a list of commands.
        /// </summary>
        string[] Commands { get; set; }
    }
}
