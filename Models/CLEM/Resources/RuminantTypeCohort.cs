using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;
using System.ComponentModel.DataAnnotations;
using Models.CLEM.Activities;
using Models.Core.Attributes;

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
    [Version(1, 0, 1, "")]
    [HelpUri(@"content/features/resources/ruminant/ruminantcohort.htm")]
    public class RuminantTypeCohort : CLEMModel
    {
        [Link]
        private ResourcesHolder Resources = null;

        /// <summary>
        /// Gender
        /// </summary>
        [Description("Gender")]
        [Required]
        public Sex Gender { get; set; }

        /// <summary>
        /// Starting Age (Months)
        /// </summary>
        [Description("Age (months)")]
        [Required, GreaterThanEqualValue(0)]
        [Units("months")]
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
        [Units("kg")]
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
        /// Constructor
        /// </summary>
        public RuminantTypeCohort()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>
        /// Create the individual ruminant animals using the Cohort parameterisations.
        /// </summary>
        /// <returns></returns>
        public List<Ruminant> CreateIndividuals()
        {
            List<Ruminant> individuals = new List<Ruminant>();

            RuminantType parent = Apsim.Parent(this, typeof(RuminantType)) as RuminantType;

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
                    if (Suckling)
                    {
                        ruminant.SetUnweaned();
                    }

                    if (Sire)
                    {
                        if(this.Gender == Sex.Male)
                        {
                            RuminantMale ruminantMale = ruminantBase as RuminantMale;
                            ruminantMale.BreedingSire = true;
                        }
                        else
                        {
                            Summary.WriteWarning(this, "Breeding sire switch is not valid for individual females [r="+parent.Name+"].[r="+this.Parent.Name+"].[r="+this.Name+"]");
                        }
                    }

                    // if weight not provided use normalised weight
                    double weightToUse = (Weight == 0) ? Weight : ruminant.NormalisedAnimalWeight;

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

                    individuals.Add(ruminantBase as Ruminant);
                }
            }

            return individuals;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            if (!formatForParentControl)
            {
                html += "\n<div class=\"activityentry\">";
                if (Number <= 0)
                {
                    html += "<span class=\"errorlink\">"+Number.ToString()+"</span> x ";
                }
                else if(Number > 1)
                {
                    html += "<span class=\"setvalue\">" + Number.ToString() + "</span> x ";
                }
                else
                {
                    html += "A ";
                }
                html += "<span class=\"setvalue\">";
                html += Age.ToString("0")+ "</span> month old ";
                html += "<span class=\"setvalue\">" + Gender.ToString() + "</span></div>";
                if(Suckling)
                {
                    html += "\n<div class=\"activityentry\">"+((Number>1)?"These individuals are suckling":"This individual is a suckling")+"</div>";
                }
                if (Sire)
                {
                    html += "\n<div class=\"activityentry\">" + ((Number > 1) ? "These individuals are breeding sires" : "This individual is a breeding sire") + "</div>";
                }

                Ruminant newInd = new Ruminant()
                {
                    Age = this.Age,
                    BreedParams = Apsim.Parent(this, typeof(RuminantType)) as RuminantType,
                    Gender = this.Gender
                };
                double normalisedWt = newInd.NormalisedAnimalWeight;

                string normWtString = normalisedWt.ToString("#,##0");
                double weightToUse = ((Weight == 0) ? normalisedWt:Weight);

                if (WeightSD > 0)
                {
                    html += "\n<div class=\"activityentry\">Individuals will be randomally assigned a weight based on a mean "+ ((Weight == 0) ? "(using the normalised weight) " : "") + "of <span class=\"setvalue\">" + weightToUse.ToString("#,##0") + "</span> kg with a standard deviation of <span class=\"setvalue\">" + WeightSD.ToString() + "</span></div>";
                    if (Math.Abs(weightToUse - normalisedWt) / normalisedWt > 0.2)
                    {
                        html += "<div class=\"activityentry\">These individuals should weigh close to normalised weight of <span class=\"errorlink\">" + normWtString + "</span> kg</div>";
                    }
                }
                else
                {
                    html += "\n<div class=\"activityentry\">" + ((Number > 1) ? "These individuals " : "This individual ") + "weigh" + ((Number > 1) ? "" : "s") + ((Weight == 0)?" the normalised weight of ":"") + " <span class=\"setvalue\">" + weightToUse.ToString("#,##0") + "</span> kg";
                    if (Math.Abs(weightToUse - normalisedWt) / normalisedWt > 0.2)
                    {
                        html += ", but should weigh close to normalised weight of <span class=\"errorlink\">" + normWtString + "</span> kg";
                    }
                    html += "</div>";
                }
                html += "</div>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            string html = "";
            if (formatForParentControl)
            {
                Ruminant newInd = new Ruminant()
                {
                    Age = this.Age,
                    BreedParams = Apsim.Parent(this, typeof(RuminantType)) as RuminantType,
                    Gender = this.Gender
                };
                double normalisedWt = newInd.NormalisedAnimalWeight;

                string normWtString = normalisedWt.ToString("#,##0");
                if(Math.Abs(this.Weight - normalisedWt)/normalisedWt>0.2)
                {
                    normWtString = "<span class=\"errorlink\">" + normWtString + "</span>";
                }

                html += "\n<tr><td>" + this.Name + "</td><td><span class=\"setvalue\">" + this.Gender + "</span></td><td><span class=\"setvalue\">" + this.Age.ToString() + "</span></td><td><span class=\"setvalue\">" + this.Weight.ToString() + ((this.WeightSD > 0) ? " (" + this.WeightSD.ToString() + ")" : "") + "</spam></td><td>"+normWtString+"</td><td><span class=\"setvalue\">" + this.Number.ToString() + "</span></td><td" + ((this.Suckling) ? " class=\"fill\"" : "") + "></td><td" + ((this.Sire) ? " class=\"fill\"" : "") + "></td></tr>";
            }
            else
            {
                html += "\n</div>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            string html = "";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return !formatForParentControl ? base.ModelSummaryClosingTags(true) : "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            return !formatForParentControl ? base.ModelSummaryOpeningTags(true) : "";
        }

    }
}



