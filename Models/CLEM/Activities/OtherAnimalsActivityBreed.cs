using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>Other animals breed activity</summary>
    /// <summary>This activity handles breeding in other animals types</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages the breeding of a specified type of other animal.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/OtherAnimals/OtherAnimalsActivityBreed.htm")]
    public class OtherAnimalsActivityBreed : CLEMActivityBase
    {
        /// <summary>
        /// name of other animal type
        /// </summary>
        [Description("Other animal type")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of other animal type required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(OtherAnimals) } })]
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
        [JsonIgnore]
        public DateTime NextDueDate { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public OtherAnimalsActivityBreed()
        {
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get other animal type model
            SelectedOtherAnimalsType = Resources.GetResourceItem(this, AnimalType, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as OtherAnimalsType;
        }

        /// <summary>An event handler to perform herd breeding </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalBreeding")]
        private void OnCLEMAnimalBreeding(object sender, EventArgs e)
        {
            if(this.TimingOK)
            {
                double malebreeders = SelectedOtherAnimalsType.Cohorts.Where(a => a.Age >= this.BreedingAge && a.Gender == Sex.Male).Sum(b => b.Number);
                if (!UseLocalMales || malebreeders > 0)
                {
                    // get number of females
                    double breeders = SelectedOtherAnimalsType.Cohorts.Where(a => a.Age >= this.BreedingAge && a.Gender == Sex.Female).Sum(b => b.Number);
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
                        SelectedOtherAnimalsType.Add(newmales, this, SelectedOtherAnimalsType.NameWithParent, "Births");
                        OtherAnimalsTypeCohort newfemales = new OtherAnimalsTypeCohort()
                        {
                            Age = 0,
                            Weight = 0,
                            Gender = Sex.Female,
                            Number = newbysex,
                            SaleFlag = HerdChangeReason.Born
                        };
                        SelectedOtherAnimalsType.Add(newfemales, this, SelectedOtherAnimalsType.NameWithParent, "Births");
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
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
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
            ResourceShortfallOccurred?.Invoke(this, e);
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
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            double breeders = SelectedOtherAnimalsType.Cohorts.Where(a => a.Age >= this.BreedingAge).Sum(b => b.Number);
            if (breeders == 0)
            {
                return new GetDaysLabourRequiredReturnArgs(0, "Breed", SelectedOtherAnimalsType.NameWithParent);
            }

            double daysNeeded = 0;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHead:
                    daysNeeded = Math.Ceiling(breeders / requirement.UnitSize) * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, "Breed", SelectedOtherAnimalsType.NameWithParent);
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }
    }
}
