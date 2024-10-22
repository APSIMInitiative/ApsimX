using Models.Core;
using Models.CLEM.Resources;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using DocumentFormat.OpenXml.Drawing.Charts;
using SixLabors.ImageSharp;

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
    [Description("Manages the breeding of a specified type of other animal")]
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
        /// Month this timer is next due.
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
            SelectedOtherAnimalsType = Resources.FindResourceType<OtherAnimals, OtherAnimalsType>(this, AnimalType, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
        }

        /// <summary>An event handler to perform herd breeding </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalBreeding")]
        private void OnCLEMAnimalBreeding(object sender, EventArgs e)
        {
            if(this.TimingOK)
            {
                int malebreeders = SelectedOtherAnimalsType.Cohorts.Where(a => a.Age >= this.BreedingAge && a.Sex == Sex.Male).Sum(b => b.Number);
                if (!UseLocalMales || malebreeders > 0)
                {
                    // get number of females
                    int breeders = SelectedOtherAnimalsType.Cohorts.Where(a => a.Age >= this.BreedingAge && a.Sex == Sex.Female).Sum(b => b.Number);
                    // create new cohorts (male and female)
                    if (breeders > 0)
                    {
                        double newbysex = breeders * this.OffspringPerBreeder / 2.0;
                        int singlesex = 0;

                        // apply stochasticity to determine proportional numbers to integers
                        if(newbysex - Math.Truncate(newbysex) > RandomNumberGenerator.Generator.Next())
                            singlesex = Convert.ToInt32(Math.Ceiling(newbysex));
                        else
                            singlesex = Convert.ToInt32(Math.Floor(newbysex));

                        double newweight = SelectedOtherAnimalsType.AgeWeightRelationship?.SolveY(0.0) ?? 0.0;
                        if (singlesex > 1)
                        {
                            OtherAnimalsTypeCohort newmales = new OtherAnimalsTypeCohort()
                            {
                                Age = 0,
                                Weight = newweight,
                                Sex = Sex.Male,
                                Number = singlesex,
                                SaleFlag = HerdChangeReason.Born
                            };
                            SelectedOtherAnimalsType.Add(newmales, this, null, "Births");
                            Status = ActivityStatus.Success;
                        }

                        if (newbysex - Math.Truncate(newbysex) > RandomNumberGenerator.Generator.NextDouble())
                            singlesex = Convert.ToInt32(Math.Ceiling(newbysex));
                        else
                            singlesex = Convert.ToInt32(Math.Floor(newbysex));

                        if (singlesex > 1)
                        {
                            OtherAnimalsTypeCohort newfemales = new OtherAnimalsTypeCohort()
                            {
                                Age = 0,
                                Weight = newweight,
                                Sex = Sex.Female,
                                Number = singlesex,
                                SaleFlag = HerdChangeReason.Born
                            };
                            SelectedOtherAnimalsType.Add(newfemales, this, null, "Births");
                            Status = ActivityStatus.Success;
                        }
                    }
                }
            }
        }
    }
}
