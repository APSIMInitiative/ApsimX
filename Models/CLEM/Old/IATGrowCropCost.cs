using Models.Core;
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
    /// <summary>manage enterprise activity</summary>
    /// <summary>This activity undertakes the overheads of running the enterprise.</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IATGrowCrop))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    public class IATGrowCropCost : CLEMActivityBase
    {
        /// <summary>
        /// Get the Clock.
        /// </summary>
        [XmlIgnore]
        [Link]
        Clock Clock = null;

        [XmlIgnore]
        [Link]
        ISummary Summary = null;


        /// <summary>
        /// Months before harvest to sow crop
        /// </summary>
        [Description("Months before harvest to apply cost")]
        [Required]
        public int MthsBeforeHarvest { get; set; }

        /// <summary>
        /// Crop payment style
        /// </summary>
        [Description("Payment style")]
        [Required]
        public CropPaymentStyleType  PaymentStyle { get; set; }


        /// <summary>
        /// Units Per Hectare 
        /// </summary>
        [Description("Units [eg. tonnes, kgs, bags] perHa or perTree")]
        [Required]
        public double UnitsPerHaOrTree { get; set; }

        /// <summary>
        /// Cost Per Unit
        /// </summary>
        [Description("Cost [per unit or fixed] ($)")]
        [Required]
        public double CostPerUnit { get; set; }

        /// <summary>
        /// name of account to use
        /// </summary>
        [Description("Name of account to use")]
        [Required]
        public string AccountName { get; set; }


        /// <summary>
        /// Date to apply the cost on.
        /// Has to be stored as a global variable because of a race condition that occurs if user sets  MthsBeforeHarvest=0
        /// Then because ParentGrowCrop and children are all executing on the harvest month and 
        /// the ParentGrowCrop executes first and it removes the harvest from the harvest list, 
        /// then the chidren such as these never get the Clock.Today == harvest date (aka costdate).
        /// So instead we store the next harvest date (aka costdate) in this variable and don't update its value
        /// until after we have done the Clock.Today == harvest date (aka costdate) comparison.
        /// </summary>
        private DateTime costDate;

        /// <summary>
        /// Store finance type to use
        /// </summary>
        private FinanceType bankAccount;

        /// <summary>
        /// Parent somewhere above this model.
        /// </summary>
        private IATGrowCrop parentGrowCrop;

        /// <summary>
        /// Parent above ParentGrowCrop.
        /// </summary>
        private IATCropLand grandParentCropLand;





        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            bool foundGrowCrop = FindParentGrowCrop();
            if (!foundGrowCrop)
            {
                Summary.WriteWarning(this, String.Format("Unable to find a parent IATGrowCrop anywhere above ({0}).", this.Name));
                throw new ApsimXException(this, String.Format("Unable to find a parent IATGrowCrop anywhere above ({0}).", this.Name));
            }

            grandParentCropLand = (IATCropLand)parentGrowCrop.Parent;

            Finance finance = Resources.FinanceResource();
            if (finance != null)
            {
                bankAccount = Resources.GetResourceItem(this, typeof(Finance), AccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as FinanceType;
            }

            costDate = CostDateFromHarvestDate();
        }


        /// <summary>
        /// Find a parent of type IATGrowCrop somewhere above this model in the simulation tree.
        /// </summary>
        /// <returns>true or false whether this found it</returns>
        private bool FindParentGrowCrop()
        {

            IModel temp = this.Parent;

            //stop when you hit the top level Activity folder if you have not found it yet.
            while ((temp is ActivitiesHolder) == false)
            {
                //if you have found it.
                if (temp is IATGrowCrop)
                {
                    parentGrowCrop = (IATGrowCrop)temp;  //set the global variable to it.
                    return true;
                }
                //else go up one more folder level
                temp = temp.Parent;
            }

            return false;
        }




        /// <summary>
        /// Get the cost date from the harvest date.
        /// This will happen every month in case the harvest has occured and there is a new harvest date.
        /// </summary>
        /// <returns></returns>
        private DateTime CostDateFromHarvestDate()
        {
            DateTime nextdate;
            CropDataType nextharvest = parentGrowCrop.HarvestData.FirstOrDefault();
            if (nextharvest != null)
            {
                nextdate = nextharvest.HarvestDate;
                return nextdate.AddMonths(-1 * MthsBeforeHarvest);
            }
            else
            {
                return new DateTime();
            }
        }



        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            ResourceRequestList = null;

            if ((costDate.Year == Clock.Today.Year) && (costDate.Month == Clock.Today.Month))
            {

                string cropName = parentGrowCrop.CropName;
                double totalcost;
                string reason;

                switch (PaymentStyle)
                {
                    case CropPaymentStyleType.Fixed:
                        totalcost = CostPerUnit;
                        reason = "Crop cost (fixed) - " + cropName;
                        break;
                    case CropPaymentStyleType.perHa:
                        totalcost = CostPerUnit * UnitsPerHaOrTree * grandParentCropLand.Area;
                        reason = "Crop cost (perHa) - " + cropName;
                        break;
                    case CropPaymentStyleType.perTree:
                        if (parentGrowCrop.IsTreeCrop)
                        {
                            totalcost = CostPerUnit * UnitsPerHaOrTree * parentGrowCrop.TreesPerHa * grandParentCropLand.Area;
                            reason = "Crop cost (perTree) - " + cropName;
                        }
                        else
                        {
                            throw new Exception(String.Format("{0} is not a Tree Crop, so CropPaymentStyleType {1} is not supported for {2}", parentGrowCrop.Name, PaymentStyle, this.Name));
                        }
                        break;
                    default:
                        throw new Exception(String.Format("CropPaymentStyleType {0} is not supported for {1}", PaymentStyle, this.Name));
                }

                if (totalcost > 0)
                {
                    ResourceRequestList = new List<ResourceRequest>();

                    ResourceRequestList.Add(new ResourceRequest()
                    {
                        Resource = bankAccount,
                        ResourceType = typeof(Finance),
                        AllowTransmutation = false,
                        Required = totalcost,
                        ResourceTypeName = this.AccountName,
                        ActivityModel = this,
                        Reason = reason
                    }
                    );
                }

            }

            costDate = CostDateFromHarvestDate();

            return ResourceRequestList;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            return;
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            if (ResourceShortfallOccurred != null)
                ResourceShortfallOccurred(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
        }


    }
}
