using APSIM.Shared.JobRunning;
using Models.Core;
using Models.Core.Run;

namespace APSIM.Server.Commands
{
    /// <summary>
    /// An interface for any command which may be passed to the APSIM Server.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Run the command.
        /// </summary>
        void Run(Runner runner, ServerJobRunner jobRunner);
    }
}
