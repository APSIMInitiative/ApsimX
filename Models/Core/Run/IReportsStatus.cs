namespace Models.Core.Run
{
    /// <summary>
    /// Encapsulates a class that reports status.
    /// </summary>
    public interface IReportsStatus
    {
        /// <summary>
        /// Status message.
        /// </summary>
        string Status { get; }
    }
}
