using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.Core;
using Models.PMF.Phen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for RuminantTypeCohort
    /// </summary>
    public class RuminantTypeCohortSummary : DescriptiveSummaryProviderBase<RuminantTypeCohort>
    {
        /// <inheritdoc/>
        public override void BuildSummary(RuminantTypeCohort model)
        {
            RuminantType rumType;
            bool specifyRuminantParent = false;

            if (!FormatForParentControl)
            {
                rumType = model.Structure.FindParent<RuminantType>(recurse: true);
                if (rumType is null)
                {
                    // look for rum type in SpecifyRuminant
                    var specParent = model.Structure.FindParents<SpecifyRuminant>().FirstOrDefault();
                    if (specParent != null)
                    {
                        var zoneCLEM = model.Structure.FindParent<ZoneCLEM>(recurse: true);
                        var resHolder = model.Structure.FindChild<ResourcesHolder>(relativeTo: zoneCLEM);
                        rumType = resHolder.FindResourceType<RuminantHerd, RuminantType>(model, specParent.RuminantTypeName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
                        specifyRuminantParent = true;
                    }
                }

                using (StringWriter textWriter = new())
                {
                    if (!specifyRuminantParent)
                        textWriter.Write(CLEMModel.DisplaySummaryValueSnippet(model.Number, warnZero: true));
                    else
                        textWriter.Write("A");

                    if (model.AgeSD == 0)
                    {
                        if (model.AgeDetails.InDays > 0)
                            textWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet(model.AgeDetails.ToDescriptionString())} old");
                        else
                            textWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet(model.AgeDetails.InDays, warnZero:true)} days old");
                    }
                    textWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(model.Sex)}");
                    generator.AddBlockWithText("activityentry", textWriter.ToString());
                }

                if (model.AgeDetails.InDays > 0 & model.AgeSD > 0)
                {
                    generator.AddBlockWithText("activityentry", $"Individuals will be randomly assigned an age based on a mean of {CLEMModel.DisplaySummaryValueSnippet(model.AgeDetails.ToDescriptionString())} with a standard deviation of {CLEMModel.DisplaySummaryValueSnippet(model.AgeSD)}");
                }

                string plural = ((model.Number > 1) ? "These individuals are" : "This individual is a");

                if (model.Suckling)
                    generator.AddBlockWithText("activityentry", $"{plural} suckling");
                if (model.Sire)
                    generator.AddBlockWithText("activityentry", $"{plural} breeding sire");


                Ruminant newInd = null;
                string normWtString = "Unavailable";

                if (rumType != null)
                {
                    if (rumType.Parameters.General is not null)
                    {
                        newInd = Ruminant.Create(model.Sex, new(2000, 1, 1), rumType.Parameters, model.AgeDetails.InDays, rumType.Parameters.General.BirthScalar[0]);
                        normWtString = newInd?.Weight.NormalisedForAge.ToString("#,##0");
                    }
                }

                using (StringWriter textWriter = new())
                {
                    if (model.WeightSD > 0)
                    {
                        textWriter.Write($"Individuals will be randomly assigned a weight based on a mean ");
                        if (model.Weight == 0)
                            textWriter.Write($"(using the normalised weight) of {CLEMModel.DisplaySummaryValueSnippet(newInd?.Weight.NormalisedForAge??0, warnZero:true)} kg");
                        else
                            textWriter.Write($"of {CLEMModel.DisplaySummaryValueSnippet(model.Weight, warnZero: true)} kg");
                        textWriter.Write($" with a standard deviation of {CLEMModel.DisplaySummaryValueSnippet(model.WeightSD, warnZero: true)} kg");
                    }
                    else
                    {
                        textWriter.Write($"{((model.Number > 1) ? "These individuals " : "This individual ")} weigh{((model.Number > 1) ? "" : "s")}");
                        if (model.Weight == 0)
                            textWriter.Write($" the normalised weight of {CLEMModel.DisplaySummaryValueSnippet(newInd?.Weight.NormalisedForAge ?? 0, warnZero: true)} kg");
                        else
                            textWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(model.Weight, warnZero: true)} kg");
                    }
                    generator.AddBlockWithText("activityentry", textWriter.ToString());
                }

                if (model.Weight > 0 && (newInd is null || (model.Weight > 0 && Math.Abs(model.Weight - newInd.Weight.NormalisedForAge) / newInd.Weight.NormalisedForAge > 0.2)))
                    generator.AddBlockWithText("warningbanner", $"Individuals should weigh close to the normalised weight of {CLEMModel.DisplaySummaryValueSnippet(newInd?.Weight.NormalisedForAge ?? 0, warnZero: true)} kg for their age.");

                if (model.ManagedPastureName != "Not specified")
                {
                    generator.AddBlockWithText("activityentry", $"These individuals will be placed on the pasture {CLEMModel.DisplaySummaryValueSnippet(model.ManagedPastureName, entryStyle: HTMLSummaryStyle.Resource)}");
                }
            }
            else
            {
                switch (model.Parent.GetType().Name)
                {
                    case "CLEMActivityBase":
                    case "SpecifyRuminant":
                        bool parentIsSpecify = (model.Parent is SpecifyRuminant);
                        using (StringWriter textWriter = new())
                        {
                            if (!parentIsSpecify)
                            {
                                textWriter.Write($"Buy {CLEMModel.DisplaySummaryValueSnippet(model.Number, warnZero: true)} x ");
                            }
                            if (model.AgeDetails.InDays >= 0)
                            {
                                textWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet(model.AgeDetails.ToDescriptionString())} old");
                            }
                            else
                            {
                                textWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet(model.AgeDetails.InDays, warnZero:true)} day old");
                            }
                            textWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet(model.Sex)}{((model.Number > 1 | parentIsSpecify) ? "s" : "")}");
                            textWriter.Write($" weighing");
                            if (model.Weight > 0)
                            {
                                textWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet(model.Weight, warnZero: true)} kg");
                                if (model.WeightSD > 0)
                                {
                                    textWriter.Write($" with a standard deviation of {CLEMModel.DisplaySummaryValueSnippet(model.WeightSD, warnZero: true)} kg");
                                }
                            }
                            else
                            {
                                textWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet("Normalised Weight for age")}");
                            }
                            if (model.Sire || model.Suckling)
                            {
                                textWriter.Write(" and ");
                                textWriter.Write(model.Sire ? CLEMModel.DisplaySummaryValueSnippet("IsSire") : "");
                                if (model.Suckling)
                                    textWriter.Write($"<span class=\"{(model.Sire ? "errorlink" : "setvalue")}\">Suckling</span>");
                            }
                            generator.AddBlockWithText("resourcebanneralone clearfix", textWriter.ToString());
                        }
                        break;
                    case "RuminantInitialCohorts":
                        RuminantType rumtype = model.Structure.FindParent<RuminantType>(recurse: true);
                        if (rumtype != null)
                        {
                            Ruminant newInd = null;
                            if (rumtype.Parameters.General is not null)
                                newInd = Ruminant.Create(model.Sex, new(2000, 1, 1), rumtype.Parameters, model.AgeDetails.InDays, rumtype.Parameters.General.BirthScalar[0]);

                            string normWtString = newInd?.Weight.NormalisedForAge.ToString("#,##0") ?? "Unavailable";
                            if (newInd is null || (model.Weight != 0 && Math.Abs(model.Weight - newInd.Weight.NormalisedForAge) / newInd.Weight.NormalisedForAge > 0.2))
                            {
                                normWtString = "<span class=\"errorlink\">" + normWtString + "</span>";
                                (model.Parent as RuminantInitialCohorts).WeightWarningOccurred = true;
                            }
                            string weightstring = "";
                            if (model.Weight > 0)
                                weightstring = $"<span class=\"setvalue\">{model.Weight.ToString() + ((model.WeightSD > 0) ? " (" + model.WeightSD.ToString() + ")" : "")}</span>";


                            List<(string label, bool fill)> cellValues = new List<(string, bool)>() {
                                (model.Name, false),
                                (CLEMModel.DisplaySummaryValueSnippet(model.Sex.ToString()), false),
                                (CLEMModel.DisplaySummaryValueSnippet(string.Join(',', model.AgeDetails.Parts)), false),
                                (weightstring, false),
                                (normWtString, false),
                                (CLEMModel.DisplaySummaryValueSnippet(Convert.ToInt32(model.Number), warnZero:true), false),
                                ("", model.Suckling),
                                ("", model.Sire)
                            };

                            if ((model.Parent as RuminantInitialCohorts).ConceptionsFound)
                            {
                                string conceptionDetails = "";
                                var setConceptionFound = model.Structure.FindChild<SetPreviousConception>();
                                if (setConceptionFound != null)
                                    conceptionDetails = $"{CLEMModel.DisplaySummaryValueSnippet(setConceptionFound.NumberDaysPregnant)} days";
                                cellValues.Add((conceptionDetails, true));
                            }

                            if ((model.Parent as RuminantInitialCohorts).AttributesFound)
                            {
                                var setAttributesFound = model.Structure.FindChildren<SetAttributeWithValue>();
                                string attributes = "";
                                foreach (var attribute in setAttributesFound)
                                {
                                    attributes += CLEMModel.DisplaySummaryValueSnippet(attribute.AttributeName);
                                }
                                cellValues.Add((attributes, setAttributesFound.Any()));
                            }

                            generator.AddTableRow(cellValues, model.Enabled);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        /// <inheritdoc/>
        public override void CreateSummaryClosingBlocks()
        {
            if (!FormatForParentControl)
                base.CreateSummaryClosingBlocks();
        }

        /// <inheritdoc/>
        public override void CreateSummaryOpeningBlocks(CLEMModel model)
        {
            if (!FormatForParentControl)
                base.CreateSummaryOpeningBlocks(model);
        }

    }
}