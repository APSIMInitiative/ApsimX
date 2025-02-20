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

        /// <summary>
        /// Calculate and set the initial fat and protein weights of the specified individual on creation using RuminantCohort details
        /// </summary>
        /// <param name="individual">The individual ruminant</param>
        /// <param name="cohortDetails">Details from cohort for individual  to create</param>
        /// <param name="initialWeight">The specified body weight of the individual</param>
        /// <returns>The resulting empty body weight</returns>
        public void SetInitialFatProtein(Ruminant individual, RuminantTypeCohort cohortDetails, double initialWeight);
    }
}
