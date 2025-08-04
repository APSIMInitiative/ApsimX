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
    public class RuminantTypeCohort : CLEMModel
    {
        private SetPreviousConception setPreviousConception = null;

        /// <summary>
        /// Sex
        /// </summary>
        [Description("Sex")]
        [Required]
        public Sex Sex { get; set; }

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
        [Core.Display(VisibleCallback = "DisplayNumber")]
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
            setPreviousConception = Structure.FindChild<SetPreviousConception>();
        }

        /// <summary>
        /// Create the individual ruminant animals using the Cohort parameterisations.
        /// </summary>
        /// <param name="initialAttributes">The initial attributes found from parent</param>
        /// <param name="ruminantType">The breed parameters if overwritten</param>
        /// <returns>List of ruminants</returns>
        public List<Ruminant> CreateIndividuals(List<ISetAttribute> initialAttributes, RuminantType ruminantType = null)
        {
            List<ISetAttribute> localAttributes = new List<ISetAttribute>();
            // add any whole herd attributes
            if (initialAttributes != null)
                localAttributes.AddRange(initialAttributes);
            // Add any attributes defined at the cohort level
            localAttributes.AddRange(Structure.FindChildren<ISetAttribute>().ToList());

            return CreateIndividuals(Convert.ToInt32(this.Number, CultureInfo.InvariantCulture), localAttributes, ruminantType);
        }

        /// <summary>
        /// Create the individual ruminant animals using the Cohort parameterisations.
        /// </summary>
        /// <param name="number">The number of individuals to create</param>
        /// <param name="initialAttributes">The initial attributes found from parent and this cohort</param>
        /// <param name="ruminantType">The breed parameters if overwritten</param>
        /// <param name="getUniqueID">Switch to determine if unique id is assigned. Not needed when added to purchase list</param>
        /// <returns>List of ruminants</returns>
        public List<Ruminant> CreateIndividuals(int number, List<ISetAttribute> initialAttributes, RuminantType ruminantType = null, bool getUniqueID = true)
        {
            List<Ruminant> individuals = new List<Ruminant>();
            if (initialAttributes is null)
                initialAttributes = new List<ISetAttribute>();

            if (number > 0)
            {
                RuminantType parent = ruminantType;
                if (parent is null)
                    parent = FindAncestor<RuminantType>();

                // get Ruminant Herd resource for unique ids
                RuminantHerd ruminantHerd = parent.Parent as RuminantHerd; // Resources.FindResourceGroup<RuminantHerd>();

                for (int i = 1; i <= number; i++)
                {
                    double weight = 0;
                    if (Weight > 0)
                    {
                        // avoid accidental small weight if SD provided but weight is 0
                        // if weight is 0 then the normalised weight will be applied in Ruminant constructor.
                        double u1 = RandomNumberGenerator.Generator.NextDouble();
                        double u2 = RandomNumberGenerator.Generator.NextDouble();
                        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                     Math.Sin(2.0 * Math.PI * u2);
                        weight = Weight + WeightSD * randStdNormal;
                    }

                    Ruminant ruminant = Ruminant.Create(Sex, parent, Age, weight);

                    if (getUniqueID)
                        ruminant.ID = ruminantHerd.NextUniqueID;
                    ruminant.Breed = parent.Breed;
                    ruminant.HerdName = parent.Name;
                    ruminant.SaleFlag = HerdChangeReason.None;

                    if (Suckling)
                    {
                        if (Age >= ((parent.NaturalWeaningAge == 0) ? parent.GestationLength : parent.NaturalWeaningAge))
                        {
                            string limitstring = (parent.NaturalWeaningAge == 0) ? $"gestation length [{parent.GestationLength}]" : $"natural weaning age [{parent.NaturalWeaningAge}]";
                            string warn = $"Individuals older than {limitstring} cannot be assigned as suckling [r={parent.Name}][r={this.Parent.Name}][r={this.Name}]{Environment.NewLine}These individuals have not been assigned suckling.";
                            Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
                        }
                    }
                    else
                        ruminant.Wean(false, "Initial state");

                    if (Sire)
                    {
                        if (this.Sex == Sex.Male)
                        {
                            RuminantMale ruminantMale = ruminant as RuminantMale;
                            ruminantMale.Attributes.Add("Sire");
                        }
                        else
                        {
                            string warn = $"Breeding sire switch is not valid for individual females [r={parent.Name}][r={this.Parent.Name}][r={this.Name}]{Environment.NewLine}These individuals have not been assigned sires. Change Sex to Male to create sires in initial herd.";
                            Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
                        }
                    }

                    // if weight not provided use normalised weight
                    ruminant.PreviousWeight = ruminant.Weight;

                    if (this.Sex == Sex.Female)
                    {
                        RuminantFemale ruminantFemale = ruminant as RuminantFemale;
                        ruminantFemale.WeightAtConception = ruminant.Weight;
                        ruminantFemale.NumberOfBirths = 0;

                        if (setPreviousConception != null)
                            setPreviousConception.SetConceptionDetails(ruminantFemale);
                    }

                    // initialise attributes
                    foreach (ISetAttribute item in initialAttributes)
                        ruminant.AddNewAttribute(item);

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

            using (StringWriter htmlWriter = new StringWriter())
            {
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
                        htmlWriter.Write("<span class=\"errorlink\">" + Number.ToString() + "</span> x ");
                    else if (!specifyRuminantParent & Number > 1)
                        htmlWriter.Write("<span class=\"setvalue\">" + Number.ToString() + "</span> x ");
                    else
                        htmlWriter.Write("A ");

                    htmlWriter.Write($"<span class=\"setvalue\">{Age}</span> month old ");
                    htmlWriter.Write("<span class=\"setvalue\">" + Sex.ToString() + "</span></div>");
                    if (Suckling)
                        htmlWriter.Write("\r\n<div class=\"activityentry\">" + ((Number > 1) ? "These individuals are suckling" : "This individual is a suckling") + "</div>");

                    if (Sire)
                        htmlWriter.Write("\r\n<div class=\"activityentry\">" + ((Number > 1) ? "These individuals are breeding sires" : "This individual is a breeding sire") + "</div>");

                    Ruminant newInd = null;
                    string normWtString = "Unavailable";

                    if (rumType != null)
                    {
                        newInd = Ruminant.Create(Sex, rumType, Age);
                        normWtString = newInd.NormalisedAnimalWeight.ToString("#,##0");
                    }

                    if (WeightSD > 0)
                    {
                        htmlWriter.Write("\r\n<div class=\"activityentry\">Individuals will be randomly assigned a weight based on a mean " + ((Weight == 0) ? "(using the normalised weight) " : "") + "of <span class=\"setvalue\">" + Weight.ToString("#,##0") + "</span> kg with a standard deviation of <span class=\"setvalue\">" + WeightSD.ToString() + "</span></div>");
                        if (newInd != null && Math.Abs(Weight - newInd.NormalisedAnimalWeight) / newInd.NormalisedAnimalWeight > 0.2)
                            htmlWriter.Write("<div class=\"activityentry\">These individuals should weigh close to the normalised weight of <span class=\"errorlink\">" + normWtString + "</span> kg for their age</div>");
                    }
                    else
                    {
                        htmlWriter.Write("\r\n<div class=\"activityentry\">" + ((Number > 1) ? "These individuals " : "This individual ") + "weigh" + ((Number > 1) ? "" : "s") + ((Weight == 0) ? " the normalised weight of " : "") + " <span class=\"setvalue\">" + Weight.ToString("#,##0") + "</span> kg");
                        if (newInd != null && Math.Abs(Weight - newInd.NormalisedAnimalWeight) / newInd.NormalisedAnimalWeight > 0.2)
                            htmlWriter.Write(", but should weigh close to the normalised weight of <span class=\"errorlink\">" + normWtString + "</span> kg for their age");
                        htmlWriter.Write("</div>");
                    }
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
                            htmlWriter.Write(Age.ToString());
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
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (FormatForParentControl)
                {
                    if (!(CurrentAncestorList.Count >= 3 && CurrentAncestorList[CurrentAncestorList.Count - 1] == typeof(RuminantInitialCohorts).Name))
                    {
                        RuminantType rumtype = FindAncestor<RuminantType>();
                        if (rumtype != null)
                        {
                            var newInd = Ruminant.Create(Sex, rumtype, Age);

                            string normWtString = newInd.NormalisedAnimalWeight.ToString("#,##0");
                            if (this.Weight != 0 && Math.Abs(this.Weight - newInd.NormalisedAnimalWeight) / newInd.NormalisedAnimalWeight > 0.2)
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
                                var setConceptionFound = Structure.FindChild<SetPreviousConception>();
                                if (setConceptionFound != null)
                                    htmlWriter.Write($"<td class=\"fill\"><span class=\"setvalue\">{setConceptionFound.NumberMonthsPregnant}</span> mths</td>");
                                else
                                    htmlWriter.Write("<td></td>");
                            }

                            if ((Parent as RuminantInitialCohorts).AttributesFound)
                            {
                                var setAttributesFound = Structure.FindChildren<SetAttributeWithValue>();
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
    }
}



