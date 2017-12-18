using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.CLEM.Resources;
using Models.CLEM.Groupings;

namespace Models.CLEM.Activities
{
    ///<summary>
    /// CLEM ruminant specific activity base model
    /// This has the ability of identify herd to be used.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("This is the Ruminant specific version of the CLEM Activity Base Class and should not be used directly.")]
    public class CLEMRuminantActivityBase : CLEMActivityBase
    {
        [Link]
        ISummary Summary = null;

        /// <summary>
        /// List of filters that define the herd
        /// </summary>
        public List<RuminantFilterGroup> HerdFilters { get; set; }

        /// <summary>
        /// Herd name determined for this activity
        /// </summary>
        public string PredictedHerdName { get; set; }

        /// <summary>
        /// Breed determined for this activity
        /// </summary>
        public string PredictedHerdBreed { get; set; }

        private bool reportedRestrictedBreed = false;
        private bool reportedRestrictedHerd = false;
        private bool allowMultipleBreeds;
        private bool allowMultipleHerds;

        /// <summary>
        /// Method to get the set herd filters
        /// </summary>
        public void InitialiseHerd(bool AllowMultipleBreeds, bool AllowMultipleHerds)
        {
            GetHerdFilters();
            allowMultipleBreeds = AllowMultipleBreeds;
            allowMultipleHerds = AllowMultipleHerds;
            DetermineHerdName();
        }

        /// <summary>
        /// Method to get the set herd filters
        /// </summary>
        private void GetHerdFilters()
        {
            HerdFilters = new List<RuminantFilterGroup>();
            IModel current = this;
            while (current.GetType() != typeof(ZoneCLEM))
            {
                var filtergroup = current.Children.Where(a => a.GetType() == typeof(RuminantFilterGroup)).Cast<RuminantFilterGroup>();
                if(filtergroup.Count() > 1)
                {
                    Summary.WriteWarning(this, "Multiple ruminant filter groups have been supplied for [" + current.Name +"]"+ Environment.NewLine + "Only the first filer group will be used.");
                }
                if (filtergroup.FirstOrDefault() != null)
                {
                    HerdFilters.Insert(0, filtergroup.FirstOrDefault());
                }
                current = current.Parent as IModel;
            }
        }

        /// <summary>
        /// Gets the current herd from all herd filters above
        /// </summary>
        public List<Ruminant> CurrentHerd(bool IncludeCheckHerdMeetsCriteria)
        {
            if (HerdFilters == null)
            {
                throw new ApsimXException(this, "Herd filters have not been defined for ["+ this.Name +"]"+ Environment.NewLine + "You need to perfrom InitialiseHerd() in CLEMInitialiseActivity for this activity.");
            }
            if(IncludeCheckHerdMeetsCriteria)
            {
                CheckHerd();
            }
            List<Ruminant> herd = Resources.RuminantHerd().Herd;
            foreach (RuminantFilterGroup filter in HerdFilters)
            {
                herd = herd.Filter(filter).ToList();
            }
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
                {
                    throw new ApsimXException(this, "Multiple breeds were detected in current herd for [" + this.Name + "]" + Environment.NewLine + "Use a Ruminant Filter Group to specify a single breed for this activity.");
                }
                PredictedHerdBreed = "Multiple";
            }
            if (herd.Select(a => a.HerdName).Distinct().Count() > 1)
            {
                if (!allowMultipleHerds)
                {
                    throw new ApsimXException(this, "Multiple herd names were detected in current herd for [" + this.Name + "]" + Environment.NewLine + "Use a Ruminant Filter Group to specify a single herd for this activity.");
                }
                PredictedHerdName = "Multiple";
            }

            if (herd.Count() > 0)
            {
                PredictedHerdBreed = herd.FirstOrDefault().Breed;
                PredictedHerdName = herd.FirstOrDefault().HerdName;
            }
            else
            {
                if (Resources.RuminantHerd().Children.Count == 0)
                {
                    throw new ApsimXException(this, "No Ruminant Type exists for Activity [" + this.Name + "]"+Environment.NewLine+"Please supply a ruminant type in the Ruminant Group of the Resources");
                }
                // try use the only herd in the model
                else if (Resources.RuminantHerd().Children.Count == 1)
                {
                    PredictedHerdBreed = (Resources.RuminantHerd().Children[0] as RuminantType).Breed;
                    PredictedHerdName = (Resources.RuminantHerd().Children[0] as RuminantType).Name;
                }
                else
                // look through filters for a herd name
                {
                    foreach (var filtergroup in this.HerdFilters)
                    {
                        foreach (var filter in filtergroup.Children.Cast<RuminantFilter>())
                        {
                            if (filter.Parameter == RuminantFilterParameters.Breed)
                            {
                                if (PredictedHerdBreed != "N/A" & !allowMultipleBreeds)
                                {
                                    // multiple breeds in filter.
                                    throw new ApsimXException(this, "Multiple breeds are used to filter the herd for Activity [" + this.Name + "]" + Environment.NewLine + "Ensure the herd comprises of a single breed for this activity.");
                                }
                                PredictedHerdBreed = filter.Value;
                            }
                            if (filter.Parameter == RuminantFilterParameters.HerdName)
                            {
                                if (PredictedHerdName != "N/A" & !allowMultipleHerds)
                                {
                                    // multiple breeds in filter.
                                    throw new ApsimXException(this, "Multiple herd names are used to filter the herd for Activity [" + this.Name + "]" + Environment.NewLine + "Ensure the herd comprises of a single herd for this activity.");
                                }
                                PredictedHerdName = filter.Value;
                            }
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
            List<Ruminant> herd = CurrentHerd(false);
            if (!allowMultipleBreeds)
            {
                // check for multiple breeds
                if (herd.Select(a => a.Breed).Distinct().Count() > 1)
                {
                    throw new ApsimXException(this, "Multiple breeds were detected in current herd for Manage Activity [" + this.Name + "]" + Environment.NewLine + "Use a Ruminant Filter Group to specify a single breed for this activity.");
                }
                // check for filter limited herd and set warning
                List<Ruminant> fullHerd = Resources.RuminantHerd().Herd.Where(a => a.Breed == PredictedHerdBreed).ToList();
                if (fullHerd.Count() != herd.Count() & !reportedRestrictedBreed)
                {
                    Summary.WriteWarning(this, String.Format("Warning! The herd being used for Manage Activity [" + this.Name + "] is a subset of the available herd for the breed." + Environment.NewLine + "Check that Ruminant Group Filtering is not restricting the herd as the activity is not considering all individuals."));
                    reportedRestrictedHerd = true;
                }
            }
            if (!allowMultipleHerds)
            {
                // check for multiple breeds
                if (herd.Select(a => a.HerdName).Distinct().Count() > 1)
                {
                    throw new ApsimXException(this, "Multiple herd types were detected in current herd for Manage Activity [" + this.Name + "]" + Environment.NewLine + "Use a Ruminant Filter Group to specify a single herd for this activity.");
                }
                // check for filter limited herd and set warning
                List<Ruminant> fullHerd = Resources.RuminantHerd().Herd.Where(a => a.HerdName == PredictedHerdName).ToList();
                if (fullHerd.Count() != herd.Count() & !reportedRestrictedHerd)
                {
                    Summary.WriteWarning(this, String.Format("Warning! The herd being used for Manage Activity [" + this.Name + "] is a subset of the available herd for the herd name." + Environment.NewLine + "Check that Ruminant Group Filtering is not restricting the herd as the activity is not considering all individuals."));
                    reportedRestrictedHerd = true;
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public override void DoActivity()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            throw new NotImplementedException();
        }
    }
}
