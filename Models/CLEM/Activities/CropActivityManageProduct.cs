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
    /// <summary>Manage crop product activity</summary>
    /// <summary>This activity sets aside land for the crop</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CropActivityManageCrop))]
    [Description("This activity is used within a crop management activity to obtain production values from the crop file.")]
    public class CropActivityManageProduct: CLEMActivityBase, IValidatableObject
    {
        [Link]
        Clock Clock = null;
        [Link]
        Simulation Simulation = null;

        /// <summary>
        /// Name of the model for the crop input file
        /// </summary>
        [Description("Name of model for crop growth file")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of crop growth file model required")]
        public string ModelNameFileCrop { get; set; }

        /// <summary>
        /// Name of crop in file
        /// </summary>
        [Description("Name of crop in file")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of crop in file required")]
        public string CropName { get; set; }

        /// <summary>
        /// Store to put crop growth into
        /// </summary>
        [Description("Store to put crop growth into")]
        [Required]
        public StoresForCrops Store { get; set; }

        /// <summary>
        /// Item name (in the store) to put crop growth into
        /// </summary>
        [Description("Item name [in the store] to put crop growth into")]
        [Required]
        public string StoreItemName { get; set; }

        /// <summary>
        /// Percentage of the crop growth that is kept
        /// </summary>
        [Description("Proportion of crop growth kept (%)")]
        [Required, Percentage]
        public double PercentKept { get; set; }

        /// <summary>
        /// Number of Trees per Hectare 
        /// </summary>
        [Description("Number of Trees (perHa) [0 if not a tree crop]")]
        [Required]
        public double TreesPerHa { get; set; }

        /// <summary>
        /// Is this a tree crop.
        /// </summary>
        [XmlIgnore]
        public bool IsTreeCrop;

        /// <summary>
        /// resource item
        /// </summary>
        [XmlIgnore]
        public IResourceType LinkedResourceItem { get; set; }

        /// <summary>
        /// Harvest Data retrieved from the Forage File.
        /// </summary>
        [XmlIgnore]
        public List<CropDataType> HarvestData { get; set; }

        /// <summary>
        /// Stores the next harvest details
        /// </summary>
        [XmlIgnore]
        public CropDataType NextHarvest { get; set; }

        /// <summary>
        /// Stores the next harvest details
        /// </summary>
        [XmlIgnore]
        public CropDataType PreviousHarvest { get; set; }

        /// <summary>
        /// Model for the crop input file
        /// </summary>
        private FileCrop fileCrop;

        /// <summary>
        /// Parent of this Model that gets the land for growing this crop.
        /// </summary>
        private CropActivityManageCrop parentManagementActivity;

        /// <summary>
        /// Units to Hectares converter from Land type
        /// </summary>
        public double UnitsToHaConverter { get; set; }

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (this.Parent.GetType() != typeof(CropActivityManageCrop))
            {
                string[] memberNames = new string[] { "Parent model" };
                results.Add(new ValidationResult("A crop activity manage product must be placed immediately below a CropActivityManageCrop model component", memberNames));
            }
            switch (Store)
            {
                case StoresForCrops.HumanFoodStore:
                case StoresForCrops.AnimalFoodStore:
                case StoresForCrops.ProductStore:
                case StoresForCrops.GrazeFoodStore:
                    break;
                default:
                    string[] memberNames = new string[] { "Store" };
                    results.Add(new ValidationResult(String.Format("Resource group [{0}] is not supported", Store.ToString()), memberNames));
                    break;
            }
            return results;
        }

        /// <summary>An event handler to allow us to initialise</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            fileCrop = Apsim.Child(Simulation, ModelNameFileCrop) as FileCrop;
            if (fileCrop == null)
            {
                throw new ApsimXException(this, String.Format("Unable to locate model for crop input file {0} (under Simulation) referred to in {1}", this.ModelNameFileCrop, this.Name));
            }

            switch (Store)
            {
                case StoresForCrops.HumanFoodStore:
                    LinkedResourceItem = Resources.GetResourceItem(this, typeof(HumanFoodStore), StoreItemName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IResourceType;
                    break;
                case StoresForCrops.AnimalFoodStore:
                    LinkedResourceItem = Resources.GetResourceItem(this, typeof(AnimalFoodStore), StoreItemName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IResourceType;
                    break;
                case StoresForCrops.GrazeFoodStore:
                    LinkedResourceItem = Resources.GetResourceItem(this, typeof(GrazeFoodStore), StoreItemName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IResourceType;
                    break;
                case StoresForCrops.ProductStore:
                    LinkedResourceItem = Resources.GetResourceItem(this, typeof(ProductStore), StoreItemName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IResourceType;
                    break;
                default:
                    throw new Exception(String.Format("Store {0} is not supported for {1}", Enum.GetName(typeof(StoresForCrops), Store), this.Name));
            }

            parentManagementActivity = (CropActivityManageCrop)this.Parent;

            // Retrieve harvest data from the forage file for the entire run. 
            HarvestData = fileCrop.GetCropDataForEntireRun(parentManagementActivity.LinkedLandItem.SoilType, CropName,
                                                               Clock.StartDate, Clock.EndDate).OrderBy(a => a.Year * 100 + a.Month).ToList<CropDataType>();
            if ((HarvestData == null) || (HarvestData.Count == 0))
            {
                throw new ApsimXException(this, String.Format("Unable to locate in crop file {0} any harvest data for SoilType {1}, CropName {2} between the dates {3} and {4}",
                    fileCrop.FileName, parentManagementActivity.LinkedLandItem.SoilType, CropName, Clock.StartDate, Clock.EndDate));
            }

            IsTreeCrop = (TreesPerHa == 0) ? false : true;  //using this boolean just makes things more readable.

            UnitsToHaConverter = (parentManagementActivity.LinkedLandItem.Parent as Land).UnitsOfAreaToHaConversion;
        }

        /// <summary>
        /// Function to get the next harvest date from data
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            PreviousHarvest = NextHarvest;
            NextHarvest = HarvestData.FirstOrDefault();
        }

        /// <summary>An event handler for a Cut and Carry</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMDoCutAndCarry")]
        private void OnCLEMDoCutAndCarry(object sender, EventArgs e)
        {
            int year = Clock.Today.Year;
            int month = Clock.Today.Month;

            if (NextHarvest != null)
            {
                //if this month is a harvest month for this crop
                if ((year == NextHarvest.HarvestDate.Year) && (month == NextHarvest.HarvestDate.Month))
                {
                    if (this.TimingOK)
                    {
                        double totalamount;
                        if (IsTreeCrop)
                            totalamount = NextHarvest.AmtKg * TreesPerHa * parentManagementActivity.Area * UnitsToHaConverter * (PercentKept / 100);
                        else
                            totalamount = NextHarvest.AmtKg * parentManagementActivity.Area * UnitsToHaConverter * (PercentKept / 100);

                        if (totalamount > 0)
                        {
                            //if Npct column was not in the file 
                            if (double.IsNaN(NextHarvest.Npct))
                            {
                                //Add without adding any new nitrogen.
                                //The nitrogen value for this feed item in the store remains the same.
                                LinkedResourceItem.Add(totalamount, this.Name, "Harvest");
                            }
                            else
                            {
                                FoodResourcePacket packet = new FoodResourcePacket()
                                {
                                    Amount = totalamount,
                                    PercentN = NextHarvest.Npct
                                };
                                LinkedResourceItem.Add(packet, this.Name, "Harvest");
                            }
                        }
                        SetStatusSuccess();
                    }
                    HarvestData.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>A list of resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
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
