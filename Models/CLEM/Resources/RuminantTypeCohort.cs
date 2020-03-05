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
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantTypeCohort.htm")]
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
        /// <returns>List of ruminants</returns>
        public List<Ruminant> CreateIndividuals()
        {
            return CreateIndividuals(Convert.ToInt32(this.Number));
        }

        /// <summary>
        /// Create the individual ruminant animals using the Cohort parameterisations.
        /// </summary>
        /// <param name="number">The number of individuals to create</param>
        /// <returns>List of ruminants</returns>
        public List<Ruminant> CreateIndividuals(int number)
        {
            List<Ruminant> individuals = new List<Ruminant>();

            if (number > 0)
            {
                RuminantType parent = Apsim.Parent(this, typeof(RuminantType)) as RuminantType;

                // get Ruminant Herd resource for unique ids
                RuminantHerd ruminantHerd = Resources.RuminantHerd();

                for (int i = 1; i <= number; i++)
                {
                    double u1 = RandomNumberGenerator.Generator.NextDouble();
                    double u2 = RandomNumberGenerator.Generator.NextDouble();
                    double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                 Math.Sin(2.0 * Math.PI * u2);
                    double weight = Weight + WeightSD * randStdNormal;
                    object ruminantBase;
                    if (this.Gender == Sex.Male)
                    {
                        ruminantBase = new RuminantMale(Age, Gender, weight, parent);
                    }
                    else
                    {
                        ruminantBase = new RuminantFemale(Age, Gender, weight, parent);
                    }

                    Ruminant ruminant = ruminantBase as Ruminant;
                    ruminant.ID = ruminantHerd.NextUniqueID;
                    ruminant.Breed = parent.Breed;
                    ruminant.HerdName = parent.Name;
                    ruminant.SaleFlag = HerdChangeReason.None;
                    if (Suckling)
                    {
                        ruminant.SetUnweaned();
                    }

                    if (Sire)
                    {
                        if (this.Gender == Sex.Male)
                        {
                            RuminantMale ruminantMale = ruminantBase as RuminantMale;
                            ruminantMale.BreedingSire = true;
                        }
                        else
                        {
                            Summary.WriteWarning(this, "Breeding sire switch is not valid for individual females [r=" + parent.Name + "].[r=" + this.Parent.Name + "].[r=" + this.Name + "]");
                        }
                    }

                    // if weight not provided use normalised weight
                    ruminant.PreviousWeight = ruminant.Weight;

                    if (this.Gender == Sex.Female)
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

                RuminantType rumtype = Apsim.Parent(this, typeof(RuminantType)) as RuminantType;
                Ruminant newInd = null;
                string normWtString = "Unavailable";

                if (rumtype != null)
                {
                    newInd = new Ruminant(this.Age, this.Gender, 0, Apsim.Parent(this, typeof(RuminantType)) as RuminantType);
                    normWtString = newInd.NormalisedAnimalWeight.ToString("#,##0");
                }

                if (WeightSD > 0)
                {
                    html += "\n<div class=\"activityentry\">Individuals will be randomally assigned a weight based on a mean "+ ((Weight == 0) ? "(using the normalised weight) " : "") + "of <span class=\"setvalue\">" + Weight.ToString("#,##0") + "</span> kg with a standard deviation of <span class=\"setvalue\">" + WeightSD.ToString() + "</span></div>";
            
                    if (newInd != null && Math.Abs(Weight - newInd.NormalisedAnimalWeight) / newInd.NormalisedAnimalWeight > 0.2)
                    {
                        html += "<div class=\"activityentry\">These individuals should weigh close to the normalised weight of <span class=\"errorlink\">" + normWtString + "</span> kg for their age</div>";
                    }
                }
                else
                {
                    html += "\n<div class=\"activityentry\">" + ((Number > 1) ? "These individuals " : "This individual ") + "weigh" + ((Number > 1) ? "" : "s") + ((Weight == 0)?" the normalised weight of ":"") + " <span class=\"setvalue\">" + Weight.ToString("#,##0") + "</span> kg";
                    if (newInd != null && Math.Abs(Weight - newInd.NormalisedAnimalWeight) / newInd.NormalisedAnimalWeight > 0.2)
                    {
                        html += ", but should weigh close to the normalised weight of <span class=\"errorlink\">" + normWtString + "</span> kg for their age";
                    }
                    html += "</div>";
                }
                html += "</div>";
            }
            else
            {
                if (this.Parent is CLEMActivityBase)
                {
                    // when formatted for parent control. i.e. child fo trade 
                    html += "\n<div class=\"resourcebanneralone clearfix\">";
                    html += "Buy ";
                    if (Number > 0)
                    {
                        html += "<span class=\"setvalue\">";
                        html += Number.ToString();
                    }
                    else
                    {
                        html += "<span class=\"errorlink\">";
                        html += "NOT SET";
                    }
                    html += "</span> x ";
                    if (Age > 0)
                    {
                        html += "<span class=\"setvalue\">";
                        html += Number.ToString();
                    }
                    else
                    {
                        html += "<span class=\"errorlink\">";
                        html += "NOT SET";
                    }
                    html += "</span> month old ";
                    html += "<span class=\"setvalue\">";
                    html += Gender.ToString() + ((Number > 1) ? "s" : "");
                    html += "</span> weighing ";
                    if (Weight > 0)
                    {
                        html += "<span class=\"setvalue\">";
                        html += Weight.ToString();
                        html += "</span> kg ";
                        if (WeightSD > 0)
                        {
                            html += "with a standard deviation of <span class=\"setvalue\">";
                            html += WeightSD.ToString();
                            html += "</span>";
                        }
                    }
                    else
                    {
                        html += "<span class=\"setvalue\">";
                        html += "Normalised weight";
                        html += "</span>";
                    }
                    html += "\n</div>";
                }
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
                RuminantType rumtype = Apsim.Parent(this, typeof(RuminantType)) as RuminantType;
                Ruminant newInd = null;
                string normWtString = "Unavailable";
                double normalisedWt = 0;

                if (rumtype != null)
                {
                    newInd = new Ruminant(this.Age, this.Gender, 0, Apsim.Parent(this, typeof(RuminantType)) as RuminantType);
                    normWtString = newInd.NormalisedAnimalWeight.ToString("#,##0");
                    normalisedWt = newInd.NormalisedAnimalWeight;
                    if (Math.Abs(this.Weight - newInd.NormalisedAnimalWeight) / newInd.NormalisedAnimalWeight > 0.2)
                    {
                        normWtString = "<span class=\"errorlink\">" + normWtString + "</span>";
                        (this.Parent as RuminantInitialCohorts).WeightWarningOccurred = true;
                    }

                    html += "\n<tr><td>" + this.Name + "</td><td><span class=\"setvalue\">" + this.Gender + "</span></td><td><span class=\"setvalue\">" + this.Age.ToString() + "</span></td><td><span class=\"setvalue\">" + this.Weight.ToString() + ((this.WeightSD > 0) ? " (" + this.WeightSD.ToString() + ")" : "") + "</spam></td><td>" + normWtString + "</td><td><span class=\"setvalue\">" + this.Number.ToString() + "</span></td><td" + ((this.Suckling) ? " class=\"fill\"" : "") + "></td><td" + ((this.Sire) ? " class=\"fill\"" : "") + "></td></tr>";
                }
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



