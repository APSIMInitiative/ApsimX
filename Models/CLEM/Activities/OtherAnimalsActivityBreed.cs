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
    /// <summary>Other animals breed activity</summary>
    /// <summary>This activity handles breeding in other animals types</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages the breeding of a specified type of other animal.")]
    public class OtherAnimalsActivityBreed : CLEMActivityBase, IValidatableObject
    {
        /// <summary>
        /// name of other animal type
        /// </summary>
        [Description("Name of other animal type")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of other animal type required")]
        public string AnimalType { get; set; }

        /// <summary>
        /// Offspring per female breeder
        /// </summary>
        [Description("Offspring per female breeder")]
        [Required, GreaterThanValue(0)]
        public double OffspringPerBreeder { get; set; }

        /// <summary>
        /// Cost per female breeder
        /// </summary>
        [Description("Cost per female breeder")]
        [Required, GreaterThanEqualValue(0)]
        public int CostPerBreeder { get; set; }

        /// <summary>
        /// Breeding female age
        /// </summary>
        [Description("Breeding age (months)")]
        [Required, GreaterThanEqualValue(1)]
        public int BreedingAge { get; set; }

        /// <summary>
        /// Use local males for breeding
        /// </summary>
        [Description("Use local males for breeding")]
        [Required]
        public bool UseLocalMales { get; set; }

        /// <summary>
        /// The Other animal type this group points to
        /// </summary>
        public OtherAnimalsType SelectedOtherAnimalsType;

        /// <summary>
        /// Month this overhead is next due.
        /// </summary>
        [XmlIgnore]
        public DateTime NextDueDate { get; set; }

        /// <summary>
        /// Labour settings
        /// </summary>
        private List<LabourFilterGroupSpecified> labour { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public OtherAnimalsActivityBreed()
        {
            this.SetDefaults();
        }

        /// <summary>
        /// Object validation
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            SelectedOtherAnimalsType = Resources.OtherAnimalsStore().GetByName(AnimalType) as OtherAnimalsType;
            if (SelectedOtherAnimalsType == null)
            {
                string[] memberNames = new string[] { "AnimalType" };
                results.Add(new ValidationResult("Unknown other animal type: " + AnimalType, memberNames));
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
            if (labour == null) labour = new List<LabourFilterGroupSpecified>();
        }

        /// <summary>An event handler to perform herd breeding </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalBreeding")]
        private void OnCLEMAnimalBreeding(object sender, EventArgs e)
        {
            if(this.TimingOK)
            {
                double malebreeders = SelectedOtherAnimalsType.Cohorts.Where(a => a.Age >= this.BreedingAge & a.Gender == Sex.Male).Sum(b => b.Number);
                if (!UseLocalMales || malebreeders > 0)
                {
                    // get number of females
                    double breeders = SelectedOtherAnimalsType.Cohorts.Where(a => a.Age >= this.BreedingAge & a.Gender == Sex.Female).Sum(b => b.Number);
                    // create new cohorts (male and female)
                    if (breeders > 0)
                    {
                        double newbysex = breeders * this.OffspringPerBreeder / 2.0;
                        OtherAnimalsTypeCohort newmales = new OtherAnimalsTypeCohort()
                        {
                            Age = 0,
                            Weight = 0,
                            Gender = Sex.Male,
                            Number = newbysex,
                            SaleFlag = HerdChangeReason.Born
                        };
                        SelectedOtherAnimalsType.Add(newmales, this.Name, SelectedOtherAnimalsType.Name);
                        OtherAnimalsTypeCohort newfemales = new OtherAnimalsTypeCohort()
                        {
                            Age = 0,
                            Weight = 0,
                            Gender = Sex.Female,
                            Number = newbysex,
                            SaleFlag = HerdChangeReason.Born
                        };
                        SelectedOtherAnimalsType.Add(newfemales, this.Name, SelectedOtherAnimalsType.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            // this activity is performed in CLEMAnimalBreeding event
            throw new NotImplementedException();
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            ResourceRequestList = null;
            if (this.TimingOK)
            {
                double breeders = SelectedOtherAnimalsType.Cohorts.Where(a => a.Age >= this.BreedingAge).Sum(b => b.Number);
                if (breeders == 0) return null;

                // for each labour item specified
                foreach (var item in labour)
                {
                    double daysNeeded = 0;
                    switch (item.UnitType)
                    {
                        case LabourUnitType.Fixed:
                            daysNeeded = item.LabourPerUnit;
                            break;
                        case LabourUnitType.perHead:
                            daysNeeded = Math.Ceiling(breeders / item.UnitSize) * item.LabourPerUnit;
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
