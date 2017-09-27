using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.WholeFarm.Resources;
using Models.WholeFarm.Groupings;

namespace Models.WholeFarm.Activities
{
    ///<summary>
    /// WholeFarm ruminant specific activity base model
    /// This has the ability of identify herd to be used.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class WFRuminantActivityBase : WFActivityBase
    {
        [Link]
        private ResourcesHolder Resources = null;
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

        private string herdName = "N/A";
        private bool reportedRestrictedHerd = false;

        /// <summary>
        /// Method to get the set herd filters
        /// </summary>
        public void InitialiseHerd(bool AllowMultipleBreeds)
        {
            GetHerdFilters();
            PredictedHerdName = DetermineHerdName(AllowMultipleBreeds);
        }

        /// <summary>
        /// Method to get the set herd filters
        /// </summary>
        private void GetHerdFilters()
        {
            HerdFilters = new List<RuminantFilterGroup>();
            IModel current = this;
            while (current.GetType() != typeof(WholeFarm))
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
        public List<Ruminant> CurrentHerd()
        {
            if (HerdFilters == null)
            {
                throw new ApsimXException(this, "Herd filters have not been defined for ["+ this.Name +"]"+ Environment.NewLine + "You need to perfrom InitialiseHerd() in WFInitialiseActivity for this activity.");
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
        private string DetermineHerdName(bool AllowMultipleBreeds)
        {
            //string herdName = "";
            // get herd name for use if no individuals are available.
            var herd = CurrentHerd();
            // check for multiple breeds
            if (herd.Select(a => a.Breed).Distinct().Count() > 1 & !AllowMultipleBreeds)
            {
                if (!AllowMultipleBreeds)
                {
                    throw new ApsimXException(this, "Multiple breeds or herd types were detected in current herd for [" + this.Name + "]" + Environment.NewLine + "Use a Ruminant Filter Group to specify a single herd for this activity.");
                }
                herdName = "Multiple";
            }

            if (herd.Count() > 0)
            {
                herdName = herd.FirstOrDefault().Breed;
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
                    herdName = (Resources.RuminantHerd().Children[0] as RuminantType).Breed;
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
                                if (herdName != "N/A" & !AllowMultipleBreeds)
                                {
                                    // multiple breeds in filter.
                                    throw new ApsimXException(this, "Multiple breed names are used to filter the herd for Activity [" + this.Name + "]" + Environment.NewLine + "Ensure the herd comprises of a single breed or herd type for this activity.");
                                }
                                herdName = filter.Value;
                            }
                        }
                    }
                }
            }
            return herdName;
        }

        /// <summary>
        /// Method to check single breed status of herd for activities.
        /// </summary>
        public void CheckHerdIsSingleBreed()
        {
            // check for multiple breeds
            if (this.CurrentHerd().Select(a => a.Breed).Distinct().Count() > 1)
            {
                throw new ApsimXException(this, "Multiple breeds or herd types were detected in current herd for Manage Activity [" + this.Name + "]" + Environment.NewLine + "Use a Ruminant Filter Group to specify a single herd for this activity.");
            }
            // check for filter limited herd and set warning
            List<Ruminant> fullHerd = Resources.RuminantHerd().Herd.Where(a => a.Breed == herdName).ToList();
            if (fullHerd.Count() != this.CurrentHerd().Count() & !reportedRestrictedHerd)
            {
                Summary.WriteWarning(this, String.Format("Warning! The herd being used for Manage Activity [" + this.Name + "] is a subset of the available herd for the breed." + Environment.NewLine + "Check that Ruminant Group Filtering is not restricting the herd as the activity is not considering all individuals."));
                reportedRestrictedHerd = true;
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
        public override void DoInitialisation()
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
