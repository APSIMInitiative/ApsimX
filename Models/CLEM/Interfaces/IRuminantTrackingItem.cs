using Docker.DotNet.Models;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Ruminant tracking item
    /// </summary>

    public interface IRuminantTrackingItem
    {
        /// <summary>
        /// Current amount
        /// </summary>
        double Amount { get; set; }
        /// <summary>
        /// Recent change
        /// </summary>
        double Change { get; }
        /// <summary>
        /// Amount remobilised in last step
        /// </summary>
        double ChangeRemobilised { get; set; }
        /// <summary>
        /// Amount unused last step
        /// </summary>
        double ChangeWasted { get; set; }
        /// <summary>
        /// Previous amount
        /// </summary>
        double Previous { get; }
        /// <summary>
        /// Adjust this tracking item with change.
        /// </summary>
        /// <param name="change">Amount to change by.</param>
        void Adjust(double change);
        /// <summary>
        /// Reset this tracking item to 0.
        /// </summary>
        void Reset();
        /// <summary>
        /// Set this tracking item.
        /// </summary>
        /// <param name="amount">Amount to set.</param>
        void Set(double amount);
    }
}