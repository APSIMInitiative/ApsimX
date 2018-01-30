using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;
using System.ComponentModel.DataAnnotations;
using Models.CLEM.Activities;

namespace Models.CLEM.Resources
{

    /// <summary>
    /// This stores the initialisation parameters for a Cohort of a specific Ruminant Type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantInitialCohorts))]
    [ValidParent(ParentType = typeof(RuminantActivityTrade))]
    [Description("This specifies a ruminant cohort used for identifying purchase individuals and initalising the herd at the start of the simulation.")]
    public class RuminantTypeCohort : CLEMModel
    {
        [Link]
        private ResourcesHolder Resources = null;
        [Link]
        ISummary Summary = null;

        /// <summary>
        /// Gender
        /// </summary>
        [Description("Gender")]
        [Required]
        public Sex Gender { get; set; }

        /// <summary>
        /// Starting Age (Months)
        /// </summary>
        [Description("Age")]
        [Required, GreaterThanEqualValue(0)]
        public int Age { get; set; }

        /// <summary>
        /// Starting Number
        /// </summary>
        [Description("Number of individuals")]
        [Required, GreaterThanEqualValue(0)]
        public double Number { get; set; }

        /// <summary>
        /// Starting Weight
        /// </summary>
        [Description("Weight (kg)")]
        [Required, GreaterThanEqualValue(0)]
        public double Weight { get; set; }

        /// <summary>
        /// Standard deviation of starting weight. Use 0 to use starting weight only
        /// </summary>
        [Description("Standard deviation of weight (0 weight only)")]
        [Required, GreaterThanEqualValue(0)]
        public double WeightSD { get; set; }

        /// <summary>
        /// Is suckling?
        /// </summary>
        [Description("Still suckling?")]
        [Required]
        public bool Suckling { get; set; }

        /// <summary>
        /// Breeding sire?
        /// </summary>
        [Description("Breeding sire?")]
        [Required]
        public bool Sire { get; set; }

        /// <summary>
        /// Create the individual ruminant animals using the Cohort parameterisations.
        /// </summary>
        /// <returns></returns>
        public List<Ruminant> CreateIndividuals()
        {
            List<Ruminant> Individuals = new List<Ruminant>();

            RuminantType parent = this.Parent.Parent as RuminantType;

            // get Ruminant Herd resource for unique ids
            RuminantHerd ruminantHerd = Resources.RuminantHerd();

            if (Number > 0)
            {
                for (int i = 1; i <= Number; i++)
                {
                    object ruminantBase = null;
                    if(this.Gender == Sex.Male)
                    {
                        ruminantBase = new RuminantMale();
                    }
                    else
                    {
                        ruminantBase = new RuminantFemale();
                    }

                    Ruminant ruminant = ruminantBase as Ruminant;

                    ruminant.ID = ruminantHerd.NextUniqueID;
                    ruminant.BreedParams = parent;
                    ruminant.Breed = parent.Breed;
                    ruminant.HerdName = parent.Name;
                    ruminant.Gender = Gender;
                    ruminant.Age = Age;
                    ruminant.SaleFlag = HerdChangeReason.None;
                    if (Suckling) ruminant.SetUnweaned();
                    if (Sire)
                    {
                        if(this.Gender == Sex.Male)
                        {
                            RuminantMale ruminantMale = ruminantBase as RuminantMale;
                            ruminantMale.BreedingSire = true;
                        }
                        else
                        {
                            Summary.WriteWarning(this, "Breeding sire switch is not valid for individual females");
                        }
                    }

                    double u1 = ZoneCLEM.RandomGenerator.NextDouble();
                    double u2 = ZoneCLEM.RandomGenerator.NextDouble();
                    double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                 Math.Sin(2.0 * Math.PI * u2);
                    ruminant.Weight = Weight + WeightSD * randStdNormal;
                    ruminant.PreviousWeight = ruminant.Weight;

                    if(this.Gender == Sex.Female)
                    {
                        RuminantFemale ruminantFemale = ruminantBase as RuminantFemale;
                        ruminantFemale.DryBreeder = true;
                        ruminantFemale.WeightAtConception = ruminant.Weight;
                        ruminantFemale.NumberOfBirths = 0;
                    }

                    Individuals.Add(ruminantBase as Ruminant);
                }
            }

            return Individuals;
        }


    }
}



