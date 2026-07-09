using System.ComponentModel;
using Models.CLEM.Resources;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// A conception model capable of returning conception rate given an individual female.
    /// </summary>
    public interface IConceptionModel
    {
        /// <summary>
        /// Get current conception rate for given female
        /// </summary>
        double ConceptionRate(RuminantFemale female);
    }
}
