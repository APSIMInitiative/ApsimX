using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Interface for Ruminant Growth Activities
    /// </summary>
    public interface IRuminantActivityGrow
    {
        /// <summary>
        /// A switch to determine if fat and protein is included
        /// </summary>
        public bool IncludeFatAndProtein { get; }

        /// <summary>
        /// A switch to determine if visceral protein is required
        /// </summary>
        public bool IncludeVisceralProteinMass { get; }

        /// <summary>
        /// Method to calculate and set the initial protein and fat masses at birth.
        /// </summary>
        /// <param name="newborn">Newborn ruminant to have fat and protein set</param>
        public void SetProteinAndFatAtBirth(Ruminant newborn);
    }
}
