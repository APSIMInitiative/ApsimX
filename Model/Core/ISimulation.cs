
namespace Model.Core
{
    public enum CriticalEnum { Information = 1, Warning = 2, Critical = 3 };
    public interface ISimulation : IZone
    {
        /// <summary>
        /// To commence the simulation, this event will be invoked.
        /// </summary>
        event NullTypeDelegate Commenced;

        /// <summary>
        /// When the simulation is finished, this event will be invoked
        /// </summary>
        event NullTypeDelegate Completed;

        /// <summary>
        /// When all simulations have finished, this event will be invoked
        /// </summary>
        event NullTypeDelegate AllCompleted;

        /// <summary>
        /// Name of file containing the simulation.
        /// </summary>
        string FileName { get; }
    }
}
