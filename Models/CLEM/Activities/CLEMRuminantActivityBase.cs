using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.CLEM.Resources;
using Models.CLEM.Groupings;
using Newtonsoft.Json;

namespace Models.CLEM.Activities
{
    ///<summary>
    /// CLEM ruminant specific activity base model
    /// This has the ability of identify herd to be used.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("This is the Ruminant specific version of the CLEM Activity Base Class and should not be used directly.")]
    public abstract class CLEMRuminantActivityBase : CLEMActivityBase
    {
        private bool reportedRestrictedBreed = false;
        private bool reportedRestrictedHerd = false;
        private bool allowMultipleBreeds;
        private bool allowMultipleHerds;

        /// <summary>
        /// List of filters that define the herd
        /// </summary>
        [JsonIgnore]
        public List<RuminantActivityGroup> HerdFilters { get; set; }

        /// <summary>
        /// Herd name determined for this activity
        /// </summary>
        [JsonIgnore]
        public string PredictedHerdName { get; set; }

        /// <summary>
        /// Breed determined for this activity
        /// </summary>
        [JsonIgnore]
        public string PredictedHerdBreed { get; set; }

        /// <summary>
        /// The herd resource for this simulation
        /// </summary>
        [JsonIgnore]
        public RuminantHerd HerdResource { get; set; }

        /// <summary>
        /// Method to get the set herd filters
        /// </summary>
        public void InitialiseHerd(bool allowMultipleBreeds, bool allowMultipleHerds)
        {
            HerdResource = Resources.FindResourceGroup<RuminantHerd>();
            GetHerdFilters();
            this.allowMultipleBreeds = allowMultipleBreeds;
            this.allowMultipleHerds = allowMultipleHerds;
            DetermineHerdName();
        }

        /// <summary>
        /// Method to get the set herd filters
        /// </summary>
        private void GetHerdFilters()
        {
            HerdFilters = new List<RuminantActivityGroup>();
            IModel current = this;
            while (current.GetType() != typeof(ZoneCLEM))
            {
                var filtergroup = current.Children.OfType<RuminantActivityGroup>();
                if(filtergroup.Count() > 1)
                    Summary.WriteWarning(this, "Multiple [f=RuminantActivityGroups] have been supplied for [a=" + current.Name +"]"+ Environment.NewLine + ". Only the first [f=RuminantActivityGroup] will be used.");

                if (filtergroup.FirstOrDefault() != null)
                    HerdFilters.Insert(0, filtergroup.FirstOrDefault());

                current = current.Parent as IModel;
            }
        }

        /// <summary>
        /// Gets the current herd from all herd filters above
        /// </summary>
        public IEnumerable<Ruminant> CurrentHerd(bool includeCheckHerdMeetsCriteria)
        {
            if (HerdFilters == null)
                throw new ApsimXException(this, "Herd filters have not been defined for [a="+ this.Name +"]"+ Environment.NewLine + "You need to perform InitialiseHerd() in CLEMInitialiseActivity for this activity. Please report this issue to CLEM developers.");

            if(includeCheckHerdMeetsCriteria)
                CheckHerd();

            if(HerdResource == null)
                throw new ApsimXException(this, "No ruminant herd has been defined for [a=" + this.Name + "]" + Environment.NewLine + "You need to add Ruminants to the resources section of this simulation setup.");

            IEnumerable<Ruminant> herd = HerdResource.Herd;
            foreach (RuminantActivityGroup filter in HerdFilters)
                herd = herd.FilterRuminants(filter);

            return herd;
        }

        /// <summary>
        /// Determines the herd name from individuals available, filter details or resources
        /// </summary>
        private void DetermineHerdName()
        {
            PredictedHerdBreed = "N/A";
            PredictedHerdName = "N/A";

            // get herd name and breed for use if no individuals are available.

            var herd = CurrentHerd(false);
            // check for multiple breeds
            if (herd.Select(a => a.Breed).Distinct().Count() > 1)
            {
                if (!allowMultipleBreeds)
                    throw new ApsimXException(this, "Multiple breeds were detected in current herd for [a=" + this.Name + "]" + Environment.NewLine + "Use a Ruminant Filter Group to specify a single breed for this activity.");
                PredictedHerdBreed = "Multiple";
            }
            if (herd.Select(a => a.HerdName).Distinct().Count() > 1)
            {
                if (!allowMultipleHerds)
                    throw new ApsimXException(this, "Multiple herd names were detected in current herd for [a=" + this.Name + "]" + Environment.NewLine + "Use a Ruminant Filter Group to specify a single herd for this activity.");
                PredictedHerdName = "Multiple";
            }

            if (herd.Count() > 0)
            {
                PredictedHerdBreed = herd.FirstOrDefault().Breed;
                PredictedHerdName = herd.FirstOrDefault().HerdName;
            }
            else
            {
                var ruminantTypeChildren = HerdResource.FindAllChildren<RuminantType>();
                if (!ruminantTypeChildren.Any())
                    throw new ApsimXException(this, "No Ruminant Type exists for Activity [a=" + this.Name + "]"+Environment.NewLine+"Please supply a ruminant type in the Ruminant Group of the Resources");

                // try use the only herd in the model
                else if (ruminantTypeChildren.Count() == 1)
                {
                    PredictedHerdBreed = ruminantTypeChildren.FirstOrDefault().Breed;
                    PredictedHerdName = ruminantTypeChildren.FirstOrDefault().Name;
                }
                else
                // look through filters for a herd name
                {
                    foreach (var filtergroup in this.HerdFilters)
                        foreach (var filter in filtergroup.Children.Cast<RuminantFilter>())
                        {
                            if (filter.Parameter == RuminantFilterParameters.Breed)
                            {
                                if (PredictedHerdBreed != "N/A" && PredictedHerdBreed != filter.Value && !allowMultipleBreeds)
                                    // multiple breeds in filter.
                                    throw new ApsimXException(this, "Multiple breeds are used to filter the herd for Activity [a=" + this.Name + "]" + Environment.NewLine + "Ensure the herd comprises of a single breed for this activity.");

                                PredictedHerdBreed = filter.Value;
                            }
                            if (filter.Parameter == RuminantFilterParameters.HerdName)
                            {
                                if (PredictedHerdName != "N/A" && !allowMultipleHerds)
                                    // multiple breeds in filter.
                                    throw new ApsimXException(this, "Multiple herd names are used to filter the herd for Activity [a=" + this.Name + "]" + Environment.NewLine + "Ensure the herd comprises of a single herd for this activity.");

                                PredictedHerdName = filter.Value;
                            }
                        }
                }
            }
        }

        /// <summary>
        /// Method to check single breed status of herd for activities.
        /// </summary>
        private void CheckHerd()
        {
            IEnumerable<Ruminant> herd = CurrentHerd(false);
            if (!allowMultipleBreeds)
            {
                // check for multiple breeds
                if (herd.Select(a => a.Breed).Distinct().Count() > 1)
                    throw new ApsimXException(this, "Multiple breeds were detected in current herd for Manage Activity [a=" + this.Name + "]" + Environment.NewLine + "Use a Ruminant Filter Group to specify a single breed for this activity.");

                // check for filter limited herd and set warning
                IEnumerable<Ruminant> fullHerd = HerdResource.Herd.Where(a => a.Breed == PredictedHerdBreed);
                if (fullHerd.Count() != herd.Count() && reportedRestrictedBreed)
                {
                    Summary.WriteWarning(this, String.Format("The herd being used for management Activity [a=" + this.Name + "] is a subset of the available herd for the breed." + Environment.NewLine + "Check that [f=RuminantFilterGroup] is not restricting the herd as the activity is not considering all individuals."));
                    reportedRestrictedHerd = true;
                }
            }
            if (!allowMultipleHerds)
            {
                // check for multiple breeds
                if (herd.Select(a => a.HerdName).Distinct().Count() > 1)
                    throw new ApsimXException(this, "Multiple herd types were detected in current herd for Manage Activity [a=" + this.Name + "]" + Environment.NewLine + "Use a Ruminant Filter Group to specify a single herd for this activity.");

                // check for filter limited herd and set warning
                IEnumerable<Ruminant> fullHerd = HerdResource.Herd.Where(a => a.HerdName == PredictedHerdName);
                if (fullHerd.Count() != herd.Count() && !reportedRestrictedHerd)
                {
                    Summary.WriteWarning(this, String.Format("The herd being used for management Activity [a=" + this.Name + "] is a subset of the available herd for the herd name." + Environment.NewLine + "Check that [f=RuminantActivityGroup] above or [f=RuminantActivityGroup] are not restricting the herd as the activity is not considering all individuals."));
                    reportedRestrictedHerd = true;
                }
            }
        }
    }
}
