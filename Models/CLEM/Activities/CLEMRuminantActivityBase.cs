using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Models.CLEM.Resources;
using Models.CLEM.Groupings;
using Newtonsoft.Json;
using Models.CLEM.Interfaces;

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
        private bool multipleHerds = false;

        /// <summary>
        /// List of filters that define the herd
        /// </summary>
        [JsonIgnore]
        private List<RuminantActivityGroup> HerdFilters { get; set; }

        /// <summary>
        /// Herd name determined for this activity
        /// </summary>
        [JsonIgnore]
        public string PredictedHerdName { get; private set; }

        /// <summary>
        /// Herd name determined for this activity to be used in display
        /// Returns empty string if only one herd in use
        /// </summary>
        [JsonIgnore]
        public string PredictedHerdNameToDisplay { get {return (multipleHerds?PredictedHerdName:""); }  }

        /// <summary>
        /// Breed determined for this activity
        /// </summary>
        [JsonIgnore]
        public string PredictedHerdBreed { get; private set; }

        /// <summary>
        /// The herd resource for this simulation zone
        /// </summary>
        [JsonIgnore]
        protected private RuminantHerd HerdResource { get; set; }

        /// <summary>
        /// Method to get the set herd filters and perform checks
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
                    Summary.WriteMessage(this, "Multiple [f=RuminantActivityGroups] have been supplied for [a=" + current.Name +"]"+ Environment.NewLine + ". Only the first [f=RuminantActivityGroup] will be used.", MessageType.Warning);

                if (filtergroup.FirstOrDefault() != null)
                    HerdFilters.Insert(0, filtergroup.FirstOrDefault());

                current = current.Parent as IModel;
            }
        }

        /// <summary>
        /// Get individuals of specified type in current herd
        /// </summary>
        /// <typeparam name="T">The type of individuals to return</typeparam>
        /// <param name="herdStyle">Overall style of individuals selected. Default NotForSale</param>
        /// <param name="excludeFlags">A list of HerdChangeReasons to exclude individuals matching flag. Default null</param>
        /// <param name="predictedBreedOnly">Flag to only return the single predicted breed for this activity. Default is true</param>
        /// <param name="includeCheckHerdMeetsCriteria">Perform check and report issues. Only expected once per activity or if herd changing. Default false</param>
        /// <returns>A list of individuals in the herd</returns>
        protected private IEnumerable<T> GetIndividuals<T>(GetRuminantHerdSelectionStyle herdStyle = GetRuminantHerdSelectionStyle.NotMarkedForSale, List<HerdChangeReason> excludeFlags = null, bool predictedBreedOnly = true, bool includeCheckHerdMeetsCriteria = false) where T: Ruminant
        {
            if(herdStyle == GetRuminantHerdSelectionStyle.ForPurchase)
                return HerdResource.PurchaseIndividuals.OfType<T>().Where(a => !predictedBreedOnly || a.Breed == PredictedHerdBreed);
            else
            {
                bool readyForSale = herdStyle == GetRuminantHerdSelectionStyle.MarkedForSale;
                return CurrentHerd(includeCheckHerdMeetsCriteria).OfType<T>().Where(a => (!predictedBreedOnly || a.Breed == PredictedHerdBreed) && (herdStyle == GetRuminantHerdSelectionStyle.AllOnFarm || a.ReadyForSale == readyForSale) && (excludeFlags is null || !excludeFlags.Contains(a.SaleFlag)));
            }
        }

        /// <summary>
        /// A method to return the unique individuals from a list and multiple potentially overlapping filter groups
        /// </summary>
        /// <param name="filters">The filter groups to include</param>
        /// <param name="herd">the individuals to filter</param>
        /// <returns>A list of unique individuals</returns>
        public static IEnumerable<T> GetUniqueIndividuals<T>(IEnumerable<RuminantGroup> filters, IEnumerable<T> herd) where T: Ruminant 
        {
            // no filters provided
            if (!filters.Any())
            {
                return herd;
            }
            // check that no filters will filter all groups otherwise return all 
            // account for any sorting or reduced takes
            var emptyfilters = filters.Where(a => a.FindAllChildren<Filter>().Any() == false);
            if (emptyfilters.Any())
            {
                foreach (var empty in emptyfilters.Where(a => a.FindAllChildren<ISort>().Any() || a.FindAllChildren<TakeFromFiltered>().Any()))
                    herd = empty.Filter(herd);
                return herd;
            }
            else
            {
                // get unique individuals across all filters
                if (filters.Count() > 1)
                {
                    IEnumerable<T> unique = new List<T>();
                    foreach (var selectFilter in filters)
                        unique = unique.Union(selectFilter.Filter(herd)).DistinctBy(a => a.ID);
                    return unique;
                }
                else
                {
                    return filters.FirstOrDefault().Filter(herd);
                }
            }
        }


        /// <summary>
        /// Gets the current herd from all herd filters above
        /// </summary>
        /// <param name="includeCheckHerdMeetsCriteria">Perfrom check and report issues. Only once per activity. Default is false.</param>
        public IEnumerable<Ruminant> CurrentHerd(bool includeCheckHerdMeetsCriteria = false)
        {
            if (HerdFilters == null)
                throw new ApsimXException(this, $"Herd filters have not been defined for [a={this.Name}{Environment.NewLine}You need to perform InitialiseHerd() in CLEMInitialiseActivity for this activity. Please report this issue to CLEM developers.");

            if(includeCheckHerdMeetsCriteria && (!allowMultipleBreeds | !allowMultipleHerds))
                CheckHerd();

            if(HerdResource == null)
                throw new ApsimXException(this, $"No ruminant herd has been defined for [a={this.Name}]{Environment.NewLine}You need to add Ruminants to the resources section of this simulation setup.");

            IEnumerable<Ruminant> herd = HerdResource.Herd;
            foreach (RuminantActivityGroup group in HerdFilters)
                herd = group.Filter(herd);
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
            if (herd.Select(a => a.Breed).Distinct().Skip(1).Any())
            {
                multipleHerds = true;
                if (!allowMultipleBreeds)
                    throw new ApsimXException(this, $"Multiple breeds were detected in current herd for [a={this.Name}]{Environment.NewLine}Use a Ruminant Filter Group to specify a single breed for this activity.");
                PredictedHerdBreed = "Multiple";
            }
            if (herd.Select(a => a.HerdName).Distinct().Skip(1).Any())
            {
                if (!allowMultipleHerds)
                    throw new ApsimXException(this, $"Multiple herd names were detected in current herd for [a={this.Name}]{Environment.NewLine}Use a Ruminant Filter Group to specify a single herd for this activity.");
                PredictedHerdName = "Multiple";
            }

            if (herd.Any())
            {
                PredictedHerdBreed = herd.FirstOrDefault().Breed;
                PredictedHerdName = herd.FirstOrDefault().HerdName;
            }
            else
            {
                var ruminantTypeChildren = HerdResource.FindAllChildren<RuminantType>();
                if (!ruminantTypeChildren.Any())
                    throw new ApsimXException(this, $"No Ruminant Type exists for Activity [a={this.Name}]{Environment.NewLine}Please supply a ruminant type in the Ruminant Group of the Resources");

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
                        foreach (var filter in filtergroup.Children.OfType<FilterByProperty>())
                        {
                            if (filter.PropertyOfIndividual == "Breed")
                            {
                                if (PredictedHerdBreed != "N/A" && PredictedHerdBreed != filter.Value.ToString() && !allowMultipleBreeds)
                                    // multiple breeds in filter.
                                    throw new ApsimXException(this, $"Multiple breeds are used to filter the herd for Activity [a={this.Name}]{Environment.NewLine}Ensure the herd comprises of a single breed for this activity.");
                                PredictedHerdBreed = filter.Value.ToString();
                            }
                            if (filter.PropertyOfIndividual == "HerdName")
                            {
                                if (PredictedHerdName != "N/A" && !allowMultipleHerds)
                                    // multiple breeds in filter.
                                    throw new ApsimXException(this, $"Multiple herd names are used to filter the herd for Activity [a={this.Name}]{Environment.NewLine}Ensure the herd comprises of a single herd for this activity.");
                                PredictedHerdName = filter.Value.ToString();
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
                if (herd.Select(a => a.Breed).Distinct().Skip(1).Any())
                    throw new ApsimXException(this, $"Multiple breeds were detected in current herd for Manage Activity [a={this.Name}]{Environment.NewLine}Use a Ruminant Filter Group to specify a single breed for this activity.");

                // check for filter limited herd and set warning
                IEnumerable<Ruminant> fullHerd = HerdResource.Herd.Where(a => a.Breed == PredictedHerdBreed);
                if (fullHerd.Count() != herd.Count() && reportedRestrictedBreed)
                {
                    Summary.WriteMessage(this, $"The herd being used for management Activity [a={this.Name}] is a subset of the available herd for the breed." + Environment.NewLine + "Check that [f=RuminantFilterGroup] is not restricting the herd as the activity is not considering all individuals.", MessageType.Warning);
                    reportedRestrictedHerd = true;
                }
            }
            if (!allowMultipleHerds)
            {
                // check for multiple breeds
                if (herd.Select(a => a.HerdName).Distinct().Skip(1).Any())
                    throw new ApsimXException(this, $"Multiple herd types were detected in current herd for Manage Activity [a={this.Name}]{Environment.NewLine}Use a Ruminant Filter Group to specify a single herd for this activity.");

                // check for filter limited herd and set warning
                IEnumerable<Ruminant> fullHerd = HerdResource.Herd.Where(a => a.HerdName == PredictedHerdName);
                if (fullHerd.Count() != herd.Count() && !reportedRestrictedHerd)
                {
                    Summary.WriteMessage(this, $"The herd being used for management Activity [a={this.Name}] is a subset of the available herd for the herd name." + Environment.NewLine + "Check that [f=RuminantActivityGroup] above or [f=RuminantActivityGroup] are not restricting the herd as the activity is not considering all individuals.", MessageType.Warning);
                    reportedRestrictedHerd = true;
                }
            }
        }

        /// <inheritdoc/>
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            return new List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)>
            {
                (FindAllChildren<RuminantGroup>(), true, "childgroupfilterborder", "Individuals will be selected from the following:", "")
            };
        }

    }
}
