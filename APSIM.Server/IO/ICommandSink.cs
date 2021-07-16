using System;
using APSIM.Server.Commands;

namespace APSIM.Server.IO
{
    /// <summary>
    /// An interface for classes which can send commands.
    /// </summary>
    public interface ICommandSink
    {
        /// <summary>
        /// Send a command to the connected client.
        /// </summary>
        /// <param name="command">The command to be sent.</param>
        void SendCommand(ICommand command);
    }
}
