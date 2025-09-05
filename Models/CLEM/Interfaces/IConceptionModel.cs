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
        /// Concpetion rate for given female
        /// </summary>
        [Description("Concpetion rate for given female")]
        double ConceptionRate(RuminantFemale female);
    }
}
