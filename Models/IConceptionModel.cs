using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
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
