using Models.CLEM.Resources;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Interface for transmutation costs
    /// </summary>
    public interface ITransmute
    {
        /// <summary>
        /// Resource type to transmute
        /// </summary>
        IResourceType TransmuteResourceType { get; set; }

        /// <summary>
        /// Resource type to transmute
        /// </summary>
        ResourceBaseWithTransactions ResourceGroup { get; set; }

        /// <summary>
        /// Amount (B) per packet (A)
        /// </summary>
        double AmountPerPacket { get; set; }

        /// <summary>
        /// Name of resource type to transmute
        /// </summary>
        string TransmuteResourceTypeName { get; set; }

        /// <summary>
        /// Style of transmute
        /// </summary>
        TransmuteStyle TransmuteStyle { get; set; }

        /// <summary>
        /// Finance account for recording price based transactions
        /// </summary>
        string FinanceTypeForTransactionsName { get; set; }

        /// <summary>
        /// Calculate the number of packets needed based on shortfall supplied and style of Transmute
        /// </summary>
        /// <param name="amount">The amount of shortfall resource</param>
        /// <returns>The number of packets</returns>
        double ShortfallPackets(double amount);

        /// <summary>
        /// Method to transform the resource and return amount remaining in Transmute resource (B)
        /// </summary>
        /// <param name="request">The resource request defining the amount of transmute resource (B) needed</param>
        /// <param name="shortfall">The amount the resource is in shortfall</param>
        /// <param name="requiredByActivities">the amount of the transmute resource needed by other activities in the time-step</param>
        /// <param name="holder">Resource holder</param>
        /// <param name="queryOnly">Only perfrom initial query, do not take resources</param>
        /// <returns>Whether the Transmute resource (B) into (A) is successful</returns>
        bool DoTransmute(ResourceRequest request, double shortfall, double requiredByActivities, ResourcesHolder holder, bool queryOnly);
    }

}
