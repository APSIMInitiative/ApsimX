using Models.Core;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// An individual labour specification item with monthly values
    /// </summary>
    public interface ILabourSpecificationItem : IModel
    {
        /// <summary>
        /// Provide availability for the specified month 
        /// </summary>
        double GetAvailability(int month);
    }
}
