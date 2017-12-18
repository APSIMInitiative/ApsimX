using System;
using System.Collections.Generic;
using Models.Core;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Holder for all initial ruminant cohorts
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("This holds the list of initial cohorts for a given (parent) ruminant herd or type.")]
    public class RuminantInitialCohorts : Model
    {
        /// <summary>
        /// Create the individual ruminant animals for this Ruminant Type (Breed)
        /// </summary>
        /// <returns></returns>
        public List<Ruminant> CreateIndividuals()
        {
            List<Ruminant> Individuals = new List<Ruminant>();
            foreach (RuminantTypeCohort cohort in Apsim.Children(this, typeof(RuminantTypeCohort)))
            {
                Individuals.AddRange(cohort.CreateIndividuals());
            }
            return Individuals;
        }
    }
}



