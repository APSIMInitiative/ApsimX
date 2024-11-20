using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace Models.CLEM.Resources
{

    /// <summary>
    /// This stores the initialisation parameters for a Cohort of a specific Ruminant Type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantInitialCohorts))]
    [ValidParent(ParentType = typeof(SpecifyRuminant))]
    [Description("Cohort component for specifying an individual during simulation or initalising the herd at the start")]
    [Version(1, 0, 3, "Includes set previous conception specification")]
    [Version(1, 0, 2, "Includes attribute specification")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantInitialCohort.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGeneral) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType })]
    public class RuminantTypeCohort : CLEMModel, IValidatableObject
    {
        private SetPreviousConception setPreviousConception = null;
        [Link]
        private RuminantHerd ruminantHerd = null;

        /// <summary>
        /// Sex
        /// </summary>
        [Description("Sex")]
        [Required]
        public Sex Sex { get; set; }

        /// <summary>
        /// Provides the age in a user friendly format of "years (optional), months (optional), days"
        /// </summary>
        [Description("Age")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        public AgeSpecifier AgeDetails { get; set; } = new int[] { 0, 12, 0 };

        /// <summary>
        /// Age in days
        /// </summary>
        [JsonIgnore]
        public int Age 
        {
            get
            {
                if ((AgeDetails?.Parts??null) is not null)
                    return AgeDetails.InDays;
                return 0;
            }
        }

        /// <summary>
        /// Starting Number
        /// </summary>
        [Description("Number of individuals")]
        [Required, GreaterThanEqualValue(0)]
        [Core.Display(VisibleCallback = "DisplayNumber")]
        public double Number { get; set; } = 1;

        /// <summary>
        /// Starting Weight
        /// </summary>
        [Description("Live weight (kg)")]
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
        /// Style of calculating the intial fat and protein mass of the indivdiual
        /// </summary>
        [Description("Style of assigning initial fat and protein")]
        [Required, GreaterThanEqualValue(0)]
        public InitialiseFatProteinAssignmentStyle InitialFatProteinStyle { get; set; } = InitialiseFatProteinAssignmentStyle.ProvideMassKg;

        /// <summary>
        /// Values to use in initialising initial fat and protein mass (fat, muscle protein, visceral protein (optional))
        /// </summary>
        [Description("Values for assigning initial fat and protein")]
        public double[] InitialFatProteinValues { get; set; }

        /// <summary>
        /// Is suckling?
        /// </summary>
        [Description("Still suckling")]
        [Required]
        public bool Suckling { get; set; }

        /// <summary>
        /// Breeding sire?
        /// </summary>
        [Description("Breeding sire?")]
        [Required]
        public bool Sire { get; set; }

        /// <summary>
        /// Display nuber of individuals
        /// </summary>
        public bool DisplayNumber { get { return Parent is RuminantInitialCohorts; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantTypeCohort()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            setPreviousConception = this.FindChild<SetPreviousConception>();
        }

        /// <summary>
        /// Create the individual ruminant animals using the Cohort parameterisations.
        /// </summary>
        /// <param name="initialAttributes">The initial attributes found from parent</param>
        /// <param name="date">The date these individuals are created</param>
        /// <param name="ruminantType">The breed parameters if overwritten</param>
        /// <returns>List of ruminants</returns>
        public List<Ruminant> CreateIndividuals(List<ISetAttribute> initialAttributes, DateTime date, RuminantType ruminantType = null)
        {
            List<ISetAttribute> localAttributes = new ();
            // add any whole herd attributes
            if (initialAttributes != null)
                localAttributes.AddRange(initialAttributes);
            // Add any attributes defined at the cohort level
            localAttributes.AddRange(this.FindAllChildren<ISetAttribute>().ToList());

            return CreateIndividuals(Convert.ToInt32(this.Number, CultureInfo.InvariantCulture), localAttributes, date, ruminantType);
        }

        /// <summary>
        /// Create the individual ruminant animals using the Cohort parameterisations.
        /// </summary>
        /// <param name="number">The number of individuals to create</param>
        /// <param name="initialAttributes">The initial attributes found from parent and this cohort</param>
        /// <param name="date">The date these individuals are created</param>
        /// <param name="ruminantType">The breed parameters if overwritten</param>
        /// <param name="getUniqueID">Switch to determine if unique id is assigned. Not needed when added to purchase list</param>
        /// <returns>List of ruminants</returns>
        public List<Ruminant> CreateIndividuals(int number, List<ISetAttribute> initialAttributes, DateTime date, RuminantType ruminantType = null, bool getUniqueID = true)
        {
            List<Ruminant> individuals = new ();
            initialAttributes ??= new List<ISetAttribute>();
            setPreviousConception = this.FindChild<SetPreviousConception>();

            if (number > 0)
            {
                RuminantType parent = ruminantType;
                if(parent is null)
                    parent = FindAncestor<RuminantType>();

                // get Ruminant Herd resource for unique ids
                RuminantHerd ruminantHerd = parent.Parent as RuminantHerd; 

                for (int i = 1; i <= number; i++)
                {
                    double weight = 0;
                    if (MathUtilities.IsPositive(Weight))
                    {
                        // avoid accidental small weight if SD provided but weight is 0
                        // if weight is 0 then the normalised weight will be applied in Ruminant constructor.
                        double u1 = RandomNumberGenerator.Generator.NextDouble();
                        double u2 = RandomNumberGenerator.Generator.NextDouble();
                        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                     Math.Sin(2.0 * Math.PI * u2);
                        weight = Weight + WeightSD * randStdNormal;
                    }

                    // estimate birth scalar based on probability of multiple births. Uses 0.07 if parameters are not provided (but validation error will also be thrown)
                    double birthScalar = parent.Parameters.General?.BirthScalar[RuminantFemale.PredictNumberOfSiblingsFromBirthOfIndividual((parent.Parameters.General?.MultipleBirthRate ?? null))-1] ?? 0.07;
                           
                    Ruminant ruminant = Ruminant.Create(Sex, parent.Parameters, date, Age, birthScalar, weight);

                    if (getUniqueID)
                        ruminant.ID = ruminantHerd.NextUniqueID;
                    ruminant.Parameters = new RuminantParameters(parent.Parameters);
                    ruminant.SaleFlag = HerdChangeReason.None;

                    if (Suckling)
                    {
                        if (Age >= ((parent.Parameters.General.NaturalWeaningAge.InDays == 0) ? parent.Parameters.General.GestationLength.InDays : parent.Parameters.General.NaturalWeaningAge.InDays))
                        {
                            string limitstring = (parent.Parameters.General.NaturalWeaningAge.InDays == 0) ? $"gestation length [{parent.Parameters.General?.GestationLength ?? "Unknown"}]" : $"natural weaning age [{parent.Parameters.General.NaturalWeaningAge.InDays}]";
                            string warn = $"Individuals older than {limitstring} cannot be assigned as suckling [r={parent.Name}][r={this.Parent.Name}][r={this.Name}]{Environment.NewLine}These individuals have not been assigned suckling.";
                            Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
                        }
                    }
                    else
                    {
                        // the user has specified that this individual is not suckling, but it is younger than the weaning age, so wean today with no reporting.
                        if(!ruminant.IsWeaned)
                            ruminant.Wean(false, string.Empty, date);
                    }

                    if (Sire)
                    {
                        if (ruminant is RuminantMale ruminantMale)
                        {
                            ruminantMale.Attributes.Add("Sire");
                        }
                        else
                        {
                            string warn = $"Breeding sire switch is not valid for individual females [r={parent.Name}][r={this.Parent.Name}][r={this.Name}]{Environment.NewLine}These individuals have not been assigned sires. Change Sex to Male to create sires in initial herd.";
                            Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
                        }
                    }

                    // if weight not provided use normalised weight
                    // ruminant.PreviousWeight = ruminant.Weight;

                    if (ruminant is RuminantFemale ruminantFemale)
                    {
                        ruminantFemale.WeightAtConception = ruminant.Weight.Live;
                        ruminantFemale.NumberOfBirths = 0;

                        setPreviousConception?.SetConceptionDetails(ruminantFemale);
                    }

                    // initialise attributes
                    foreach (ISetAttribute item in initialAttributes)
                        ruminant.AddNewAttribute(item);

                    RuminantInfoWeight.SetInitialFatProtein(ruminant, InitialFatProteinStyle, ruminantHerd.RuminantGrowActivity, InitialFatProteinValues);

                    individuals.Add(ruminant);
                }

                // add any mandatory attributes to the list on the ruminant type
                foreach (var mattrib in initialAttributes.Where(a => a.Mandatory))
                    parent.AddMandatoryAttribute(mattrib.AttributeName);
            }

            return individuals;
        }

        #region descriptive summary 

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            RuminantType rumType;
            bool specifyRuminantParent = false;

            using StringWriter htmlWriter = new();
            if (!FormatForParentControl)
            {
                rumType = FindAncestor<RuminantType>();
                if (rumType is null)
                {
                    // look for rum type in SpecifyRuminant
                    var specParent = this.FindAllAncestors<SpecifyRuminant>().FirstOrDefault();
                    if (specParent != null)
                    {
                        var resHolder = this.FindAncestor<ZoneCLEM>().FindDescendant<ResourcesHolder>();
                        rumType = resHolder.FindResourceType<RuminantHerd, RuminantType>(this, specParent.RuminantTypeName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
                        specifyRuminantParent = true;
                    }
                }

                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if (!specifyRuminantParent & Number <= 0)
                    htmlWriter.Write($"<span class=\"errorlink\">{Number}</span> x ");
                else if (!specifyRuminantParent & Number > 1)
                    htmlWriter.Write($"<span class=\"setvalue\">{Number}</span> x ");
                else
                    htmlWriter.Write("A ");

                if(AgeDetails.InDays > 0)
                    htmlWriter.Write($"<span class=\"setvalue\">{AgeDetails.ToDescriptionString()}</span> old ");
                else
                    htmlWriter.Write($"<span class=\"errorlink\">0</span> days old ");

                htmlWriter.Write($"<span class=\"setvalue\">{Sex}</span></div>");
                if (Suckling)
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">{((Number > 1) ? "These individuals are suckling" : "This individual is a suckling")}</div>");

                if (Sire)
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">{((Number > 1) ? "These individuals are breeding sires" : "This individual is a breeding sire")}</div>");

                Ruminant newInd = null;
                string normWtString = "Unavailable";

                if (rumType != null)
                {
                    if (rumType.Parameters.General is not null)
                    {
                        newInd = Ruminant.Create(Sex, rumType.Parameters, new(2000, 1, 1), Age, rumType.Parameters.General.BirthScalar[0]);
                        normWtString = newInd.Weight.NormalisedForAge.ToString("#,##0");
                    }
                }

                if (WeightSD > 0)
                {
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">Individuals will be randomly assigned a weight based on a mean ");
                    if (Weight == 0)
                        htmlWriter.Write($"(using the normalised weight) of {((newInd is null) ? "<span class=\"errorlink\">" : "<span class=\"setvalue\">")}{normWtString}</span> kg");
                    else
                        htmlWriter.Write($"of {DisplaySummaryValueSnippet(Weight)} kg");
                    htmlWriter.Write($" with a standard deviation of {DisplaySummaryValueSnippet(WeightSD)}</div>");
                }
                else
                {
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">{((Number > 1) ? "These individuals " : "This individual ")}weigh{((Number > 1) ? "" : "s")}");
                    if (Weight == 0)
                        htmlWriter.Write($" the normalised weight of {((newInd is null) ? "<span class=\"errorlink\">" : "<span class=\"setvalue\">")}{normWtString}</span> kg");
                    else
                        htmlWriter.Write($" {DisplaySummaryValueSnippet(Weight)} kg");
                }
                if (Weight>0 && (newInd is null || (Weight>0 && Math.Abs(Weight - newInd.Weight.NormalisedForAge) / newInd.Weight.NormalisedForAge > 0.2)))
                    htmlWriter.Write($"<div class=\"warningbanner\">Individuals should weigh close to the normalised weight of <span class=\"errorlink\">{normWtString}</span> kg for their age.</div>");
            }
            else
            {
                if (this.Parent is CLEMActivityBase | this.Parent is SpecifyRuminant)
                {
                    bool parentIsSpecify = (Parent is SpecifyRuminant);

                    // when formatted for parent control. i.e. child fo trade 
                    htmlWriter.Write("\r\n<div class=\"resourcebanneralone clearfix\">");
                    if (!parentIsSpecify)
                    {
                        htmlWriter.Write("Buy ");
                        if (Number > 0)
                        {
                            htmlWriter.Write("<span class=\"setvalue\">");
                            htmlWriter.Write(Number.ToString());
                        }
                        else
                        {
                            htmlWriter.Write("<span class=\"errorlink\">");
                            htmlWriter.Write("NOT SET");
                        }
                        htmlWriter.Write("</span> x ");
                    }
                    if (Age > 0)
                    {
                        htmlWriter.Write("<span class=\"setvalue\">");
                        htmlWriter.Write(AgeDetails.ToString());
                    }
                    else
                    {
                        htmlWriter.Write("<span class=\"errorlink\">");
                        htmlWriter.Write("NOT SET");
                    }
                    htmlWriter.Write("</span> month old ");
                    htmlWriter.Write("<span class=\"setvalue\">");
                    htmlWriter.Write(Sex.ToString() + ((Number > 1 | parentIsSpecify) ? "s" : ""));
                    htmlWriter.Write("</span> weighing ");
                    if (Weight > 0)
                    {
                        htmlWriter.Write("<span class=\"setvalue\">");
                        htmlWriter.Write(Weight.ToString());
                        htmlWriter.Write("</span> kg ");
                        if (WeightSD > 0)
                        {
                            htmlWriter.Write("with a standard deviation of <span class=\"setvalue\">");
                            htmlWriter.Write(WeightSD.ToString());
                            htmlWriter.Write("</span>");
                        }
                    }
                    else
                    {
                        htmlWriter.Write("<span class=\"setvalue\">");
                        htmlWriter.Write("Normalised weight");
                        htmlWriter.Write("</span>");
                    }
                    if (Sire || Suckling)
                    {
                        htmlWriter.Write(" and ");
                        htmlWriter.Write(Sire ? "<span class=\"setvalue\">Sires</span>" : "");
                        if (Suckling)
                            htmlWriter.Write($"<span class=\"{(Sire ? "errorlink" : "setvalue")}\">Suckling</span>");
                    }
                    htmlWriter.Write("\r\n</div>");
                }
            }
            return htmlWriter.ToString();
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            using StringWriter htmlWriter = new();
            if (FormatForParentControl)
            {
                if (!(CurrentAncestorList.Count >= 3 && CurrentAncestorList[CurrentAncestorList.Count - 1] == typeof(RuminantInitialCohorts).Name))
                {
                    RuminantType rumtype = FindAncestor<RuminantType>();
                    if (rumtype != null)
                    {
                        Ruminant newInd = null;
                        if (rumtype.Parameters.General is not null)
                            newInd = Ruminant.Create(Sex, rumtype.Parameters, new(2000, 1, 1), Age, rumtype.Parameters.General.BirthScalar[0]);

                        string normWtString = newInd?.Weight.NormalisedForAge.ToString("#,##0")??"Unavailable";
                        if (newInd is null || (this.Weight != 0 && Math.Abs(this.Weight - newInd.Weight.NormalisedForAge) / newInd.Weight.NormalisedForAge > 0.2))
                        {
                            normWtString = "<span class=\"errorlink\">" + normWtString + "</span>";
                            (this.Parent as RuminantInitialCohorts).WeightWarningOccurred = true;
                        }
                        string weightstring = "";
                        if (this.Weight > 0)
                            weightstring = $"<span class=\"setvalue\">{this.Weight.ToString() + ((this.WeightSD > 0) ? " (" + this.WeightSD.ToString() + ")" : "")}</span>";

                        htmlWriter.Write($"\r\n<tr{(this.Enabled ? "" : " class=\"disabled\"")}><td>{this.Name}</td><td><span class=\"setvalue\">{this.Sex}</span></td><td><span class=\"setvalue\">{this.Age.ToString()}</span></td><td>{weightstring}</td><td>{normWtString}</td><td><span class=\"setvalue\">{this.Number.ToString()}</span></td><td{((this.Suckling) ? " class=\"fill\"" : "")}></td><td{((this.Sire) ? " class=\"fill\"" : "")}></td>");

                        if ((Parent as RuminantInitialCohorts).ConceptionsFound)
                        {
                            var setConceptionFound = this.FindChild<SetPreviousConception>();
                            if (setConceptionFound != null)
                                htmlWriter.Write($"<td class=\"fill\"><span class=\"setvalue\">{setConceptionFound.NumberDaysPregnant}</span> days</td>");
                            else
                                htmlWriter.Write("<td></td>");
                        }

                        if ((Parent as RuminantInitialCohorts).AttributesFound)
                        {
                            var setAttributesFound = this.FindAllChildren<SetAttributeWithValue>();
                            if (setAttributesFound.Any())
                            {
                                htmlWriter.Write($"<td class=\"fill\">");
                                foreach (var attribute in setAttributesFound)
                                {
                                    htmlWriter.Write($"<span class=\"setvalue\">{attribute.AttributeName}</span> ");
                                }
                                htmlWriter.Write($"</td>");
                            }
                            else
                                htmlWriter.Write("<td></td>");
                        }

                        htmlWriter.Write("</tr>");
                    }
                }
            }
            else
                htmlWriter.Write("\r\n</div>");

            return htmlWriter.ToString();
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return !FormatForParentControl ? base.ModelSummaryClosingTags() : "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            return !FormatForParentControl ? base.ModelSummaryOpeningTags() : "";
        }

        #endregion

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            string[] valueLabels = new string[] { "Fat", "Muscle protein", "Visceral protein" };
            if (ruminantHerd.RuminantGrowActivity.IncludeFatAndProtein == false)
                yield break; 

            if(InitialFatProteinValues is null && InitialFatProteinStyle != InitialiseFatProteinAssignmentStyle.EstimateFromRelativeCondition)
            {
                yield return new ValidationResult("Initial fat and protein values are required in all [r=RuminantTypeCohort] for the specified ruminant growth model", new string[] { "InitialFatProteinValues" });
            }

            int entries;
            if (ruminantHerd.RuminantGrowActivity.IncludeVisceralProteinMass)
            {
                entries = 3;
            }
            else
            {
                entries = 2;
                if (InitialFatProteinValues.Length == 2)
                {
                    valueLabels[1] = "Protein";
                }
            }

            if (InitialFatProteinValues is not null && InitialFatProteinValues.Length < entries)
            {
                yield return new ValidationResult($"Insufficient values provided for initial fat and protein mass. {entries} values are required for specified ruminant growth model", new string[] { "InitialFatProteinValues" });
            }

            // if proportion check all values are 0-1
            if (InitialFatProteinStyle == InitialiseFatProteinAssignmentStyle.ProportionOfEmptyBodyMass)
            {
                for(int i = 0; i < InitialFatProteinValues.Length; i++)
                {
                    if (InitialFatProteinValues[i] < 0 | InitialFatProteinValues[i] > 1)
                    {
                        yield return new ValidationResult($"Value for initial [{valueLabels[i]}] proportion of empty body weight must be between 0 and 1", new string[] { "InitialFatProteinValues" });
                    }
                }
            }
        }

        #endregion
    }
}



