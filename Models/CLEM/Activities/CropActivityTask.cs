using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>Crop activity task</summary>
    /// <summary>This activity will perform costs and labour for a crop activity</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CropActivityManageProduct))]
    [Description("This is a crop task (e.g. sowing) with associated costs and labour requirements.")]
    public class CropActivityTask: CLEMActivityBase, IValidatableObject
    {
        private List<LabourFilterGroupSpecified> labour;

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if(this.Parent.GetType() != typeof(CropActivityManageProduct))
            {
                string[] memberNames = new string[] { "Parent model" };
                results.Add(new ValidationResult("A crop activity task must be placed immediately below a CropActivityManageProduct model component", memberNames));
            }

            CropActivityManageProduct ProductParent = Parent as CropActivityManageProduct;
            foreach (CropActivityFee item in Apsim.Children(this, typeof(CropActivityFee)))
            {
                if (!ProductParent.IsTreeCrop)
                {
                    if (item.PaymentStyle == CropPaymentStyleType.perTree)
                    {
                        string[] memberNames = new string[] { item.Name + ".PaymentStyle" };
                        results.Add(new ValidationResult("The payment style " + item.PaymentStyle.ToString() + " is not supported for crops defined non tree crops", memberNames));
                    }
                }
            }
            return results;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get labour specifications
            labour = Apsim.Children(this, typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList(); //  this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
            if (labour.Count() == 0) labour = new List<LabourFilterGroupSpecified>();
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            ResourceRequestList = null;

            if (this.TimingOK)
            {
                // get all crop fees for task
                foreach (CropActivityFee item in Apsim.Children(this, typeof(CropActivityFee)))
                {
                    if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
                    double sumneeded = 0;
                    switch (item.PaymentStyle)
                    {
                        case CropPaymentStyleType.Fixed:
                            sumneeded = item.Amount;
                            break;
                        case CropPaymentStyleType.perHa:
                            CropActivityManageCrop CropParent = Parent.Parent as CropActivityManageCrop;
                            CropActivityManageProduct ProductParent = Parent as CropActivityManageProduct;
                            sumneeded = CropParent.Area * ProductParent.UnitsToHaConverter * item.Amount;
                            break;
                        case CropPaymentStyleType.perTree:
                            CropParent = Parent.Parent as CropActivityManageCrop;
                            ProductParent = Parent as CropActivityManageProduct;
                            sumneeded = ProductParent.TreesPerHa * CropParent.Area * ProductParent.UnitsToHaConverter * item.Amount;
                            break;
                        default:
                            throw new Exception(String.Format("PaymentStyle ({0}) is not supported for ({1}) in ({2})", item.PaymentStyle, item.Name, this.Name));
                    }
                    ResourceRequestList.Add(new ResourceRequest()
                    {
                        AllowTransmutation = false,
                        Required = sumneeded,
                        ResourceType = typeof(Finance),
                        ResourceTypeName = "General account",
                        ActivityModel = this,
                        FilterDetails = null,
                        Reason = item.Name
                    }
                    );
                }

                // for each labour item specified
                foreach (var item in labour)
                {
                    double daysNeeded = 0;
                    switch (item.UnitType)
                    {
                        case LabourUnitType.Fixed:
                            daysNeeded = item.LabourPerUnit;
                            break;
                        case LabourUnitType.perHa:
                            CropActivityManageCrop CropParent = Parent.Parent as CropActivityManageCrop;
                            CropActivityManageProduct ProductParent = Parent as CropActivityManageProduct;
                            daysNeeded = Math.Ceiling(CropParent.Area * ProductParent.UnitsToHaConverter / item.UnitSize) * item.LabourPerUnit;
                            break;
                        case LabourUnitType.perTree:
                            CropParent = Parent.Parent as CropActivityManageCrop;
                            ProductParent = Parent as CropActivityManageProduct;
                            daysNeeded = Math.Ceiling(ProductParent.TreesPerHa * CropParent.Area * ProductParent.UnitsToHaConverter / item.UnitSize) * item.LabourPerUnit;
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
            }
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
