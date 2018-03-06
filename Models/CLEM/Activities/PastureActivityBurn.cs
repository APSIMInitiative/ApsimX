using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM.Activities
{
    /// <summary>Activity to perform controlled burning of native pastures</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity applies controlled burning to a specified graze food store (i.e. native pasture paddock).")]
    public class PastureActivityBurn: CLEMActivityBase
    {
        /// <summary>
        /// Minimum proportion green for fire to carry
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0.5)]
        [Description("Minimum proportion green for fire to carry")]
        [Required, Proportion]
        public double MinimumProportionGreen { get; set; }

        /// <summary>
        /// Name of graze food store/paddock to burn
        /// </summary>
        [Description("Name of graze food store/paddock to burn")]
        [Required]
        public string PaddockName { get; set; }

        private GrazeFoodStoreType pasture { get; set; }
        private List<LabourFilterGroupSpecified> labour { get; set; }
        private GreenhouseGasesType methane { get; set; }
        private GreenhouseGasesType nox { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public PastureActivityBurn()
        {
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get pasture
            pasture = Resources.GetResourceItem(this, typeof(GrazeFoodStore), PaddockName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;

            // get labour specifications
            labour = Apsim.Children(this, typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList(); //  this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
            if (labour == null) labour = new List<LabourFilterGroupSpecified>();

            methane = Resources.GetResourceItem(this, typeof(GreenhouseGases), "Methane", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as GreenhouseGasesType;
            nox = Resources.GetResourceItem(this, typeof(GreenhouseGases), "NOx", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as GreenhouseGasesType;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            ResourceRequestList = null;
            // for each labour item specified
            foreach (var item in labour)
            {
                double daysNeeded = 0;
                switch (item.UnitType)
                {
                    case LabourUnitType.Fixed:
                        daysNeeded = item.LabourPerUnit;
                        break;
                    default:
                        throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", item.UnitType, item.Name, this.Name));
                }
                if (daysNeeded > 0)
                {
                    if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
                    ResourceRequestList.Add(new ResourceRequest()
                    {
                        AllowTransmutation = false,
                        Required = daysNeeded,
                        ResourceType = typeof(Labour),
                        ResourceTypeName = "",
                        ActivityModel = this,
                        FilterDetails = new List<object>() { item }
                    }
                    );
                }
            }
            return ResourceRequestList;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            // labour is consumed and shortfall has no impact at present
            // could lead to other paddocks burning in future.

            // calculate labour limit
//            double labourLimit = 1;
//            double labourNeeded = ResourceRequestList.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Required);
//            double labourProvided = ResourceRequestList.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Provided);
//            if (labourNeeded > 0)
//            {
//                labourLimit = labourProvided / labourNeeded;
//            }

            if(Status != ActivityStatus.Partial)
            {
                Status = ActivityStatus.NotNeeded;
            }
            // proportion green
            double green = pasture.Pools.Where(a => a.Age < 2).Sum(a => a.Amount);
            double total = pasture.Amount;
            if (total>0)
            {
                if(green / total <= MinimumProportionGreen)
                {
                    // TODO add weather to calculate fire intensity
                    // TODO calculate patchiness from intensity
                    // TODO influence trees and weeds

                    // burn
                    // remove biomass
                    pasture.Remove(new ResourceRequest()
                    {
                        ActivityModel = this,
                        Required = total,
                        AllowTransmutation = false,
                        Reason = "Burn",
                        ResourceTypeName = PaddockName,
                    }
                    );

                    // add emissions
                    double burnkg = total * 0.76 * 0.46; // burnkg * burning efficiency * carbon content
                    if (methane != null)
                    {
                        //TODO change emissions for green material
                        methane.Add(burnkg * 1.333 * 0.0035, "Burn", PaddockName); // * 21; // methane emissions from fire (CO2 eq)
                    }
                    if (nox != null)
                    {
                        nox.Add(burnkg * 1.571 * 0.0076 * 0.12, "Burn", PaddockName); // * 21; // methane emissions from fire (CO2 eq)
                    }

                    // TODO: add fertilisation to pasture for given period.

                    Status = ActivityStatus.Success;
                }
            }
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

    }
}
