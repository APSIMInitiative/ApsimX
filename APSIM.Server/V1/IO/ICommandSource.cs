using System;
using APSIM.Server.Commands;

namespace APSIM.Server.IO
{
    /// <summary>
    /// An interface for classes which can receive commands.
    /// </summary>
    public interface ICommandSource
    {
        /// <summary>
        /// Wait for a command from the conencted client.
        /// </summary>
        ICommand WaitForCommand();

        /// <summary>
        /// Called when a command finishes.
        /// The expectation is that the client will need to be signalled
        /// somehow when this occurs.
        /// </summary>
        /// <param name="command">The command that was run.</param>
        /// <param name="error">Error details (if command failed). If command succeeded, this will be null.</param>
        void OnCommandFinished(ICommand command, Exception error = null);
    }
}
