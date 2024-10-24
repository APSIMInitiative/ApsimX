using Models.CLEM.Groupings;
using Models.Core;
using Models.Core.Attributes;
using Models.PMF.Organs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Parent model of Ruminant Types.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("Resource group for all other animals types (not ruminants) in the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Other animals/OtherAnimals.htm")]
    public class OtherAnimals : ResourceBaseWithTransactions
    {
        /// <summary>
        /// The last group of individuals to be added or removed (for reporting)
        /// </summary>
        [JsonIgnore]
        public OtherAnimalsTypeCohort LastCohortChanged { get; set; }

        /// <summary>
        /// Method to return the selected cohorts based on filtering by multiple cohort groups
        /// </summary>
        /// <returns>An IEnumberable list of selected cohorts from all OtherAnimalTypes.</returns>
        public IEnumerable<OtherAnimalsTypeCohort> GetCohorts(IEnumerable<OtherAnimalsGroup> filtergroups, bool includeTakeFilters)
        {
            IEnumerable<OtherAnimalsType> otherAnimalTypes;
            
            if(filtergroups != null && filtergroups.Any())
                otherAnimalTypes = FindAllChildren<OtherAnimalsType>().Where(a => filtergroups.Any(b => b.SelectedOtherAnimalsType == a));
            else
                otherAnimalTypes = FindAllChildren<OtherAnimalsType>();

            foreach (OtherAnimalsType otherAnimalType in otherAnimalTypes)
            {
                foreach (OtherAnimalsTypeCohort cohort in otherAnimalType.GetCohorts(filtergroups?.Where(a => a.SelectedOtherAnimalsType == otherAnimalType)??null, includeTakeFilters))
                    yield return cohort;
            }
        }
    }
}
