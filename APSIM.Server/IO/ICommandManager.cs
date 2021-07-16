using System;
using APSIM.Server.Commands;

namespace APSIM.Server.IO
{
    /// <summary>
    /// An interface for a class which can send and receive commands.
    /// </summary>
    public interface ICommandManager : ICommandSource, ICommandSink
    {
    }
}
