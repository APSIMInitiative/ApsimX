using DocumentFormat.OpenXml.Office.CoverPageProps;
using Models.CLEM.Resources;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Interface for a packet of food resources
    /// </summary>
    public interface IFoodResourceStore
    {
        /// <summary>
        /// Amount in the packet
        /// </summary>
        double Amount { get; }

        /// <summary>
        /// The feed packet details of the FoodResourcePacket
        /// </summary>
        public FoodResourcePacket Details { get; }
    }
}