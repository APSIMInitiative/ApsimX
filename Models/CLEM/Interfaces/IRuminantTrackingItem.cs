
using Models.CLEM.Resources;

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
        double Amount { get; }
        /// <summary>
        /// Recent change
        /// </summary>
        double Change { get; }
        /// <summary>
        /// Amount extra offered to other sources
        /// </summary>
        double Net { get; set; }
        /// <summary>
        /// Previous amount
        /// </summary>
        double Previous { get; }
        /// <summary>
        /// Adjust this tracking item with change.
        /// </summary>
        /// <param name="change">Amount to change by</param>
        /// <param name="ind">Individual to provide age for dry protein</param>
        void Adjust(double change, Ruminant ind);
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