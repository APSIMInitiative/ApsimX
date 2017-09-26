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
        /// Method to get the set herd filters
        /// </summary>
        public void GetHerdFilters()
        {
            HerdFilters = new List<RuminantFilterGroup>();
            IModel current = this;
            while (current.GetType() != typeof(WholeFarm))
            {
                var filtergroup = current.Children.Where(a => a.GetType() == typeof(RuminantFilterGroup)).Cast<RuminantFilterGroup>();
                if(filtergroup.Count() > 1)
                {
                    Summary.WriteWarning(this, "Multiple ruminant filter groups have been supplied for " + current.Name + Environment.NewLine + "Only the first filer group will be used.");
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
                throw new ApsimXException(this, "Herd filters have not been defined for "+ this.Name + Environment.NewLine + "You need to perfrom GetHerdFilters()  in OnSimulationStart for this activity.");
            }
            List<Ruminant> herd = Resources.RuminantHerd().Herd;
            foreach (RuminantFilterGroup filter in HerdFilters)
            {
                herd = herd.Filter(filter).ToList();
            }
            return herd;
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
