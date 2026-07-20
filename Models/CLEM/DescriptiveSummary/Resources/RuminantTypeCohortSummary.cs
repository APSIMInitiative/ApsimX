using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Spreadsheet;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.PMF.Phen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for RuminantTypeCohort
/// </summary>
public class RuminantTypeCohortSummary : DescriptiveSummaryProviderBase<RuminantTypeCohort>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public RuminantTypeCohortSummary()
    {
        SummaryStyle = HTMLSummaryStyle.SubResource;
    }

    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        var model = ModelTyped;
        if (model is null) return [];

        if (FormatForParentControl)
        {
            return
            [
                new ChildComponentGroup(
                id: "ignoreall",
                models: model.Structure.FindChildren<IModel>(),
                include: false, 
                missing: ""
                )
            ];
        }
        return [];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

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
                    textWriter.Write(generator.DisplaySummaryValueSnippet(model.Number, warnZero: true));
                else
                    textWriter.Write("A");

                if (model.AgeSD == 0)
                {
                    if (model.AgeDetails.InDays > 0)
                        textWriter.Write($"{generator.DisplaySummaryValueSnippet(model.AgeDetails.ToDescriptionString())} old");
                    else
                        textWriter.Write($"{generator.DisplaySummaryValueSnippet(model.AgeDetails.InDays, warnZero:true)} days old");
                }
                textWriter.Write($" {generator.DisplaySummaryValueSnippet(model.Sex)}");
                generator.AddBlockWithText(textWriter.ToString());
            }

            if (model.AgeDetails.InDays > 0 & model.AgeSD > 0)
            {
                generator.AddBlockWithText($"Individuals will be randomly assigned an age based on a mean of {generator.DisplaySummaryValueSnippet(model.AgeDetails.ToDescriptionString())} with a standard deviation of {generator.DisplaySummaryValueSnippet(model.AgeSD)}");
            }

            string plural = ((model.Number > 1) ? "These individuals are" : "This individual is a");

            if (model.Suckling)
                generator.AddBlockWithText($"{plural} suckling");
            if (model.Sire)
                generator.AddBlockWithText($"{plural} breeding sire");


            Ruminant newInd = null;

            if (rumType != null)
            {
                if (rumType.Parameters.General is not null)
                {
                    newInd = Ruminant.Create(model.Sex, new(2000, 1, 1), rumType.Parameters, model.AgeDetails.InDays, rumType.Parameters.General.BirthScalar[0], cohortDetails: model);
                }
            }

            using (StringWriter textWriter = new())
            {
                if (model.WeightSD > 0)
                {
                    textWriter.Write($"Individuals will be randomly assigned a weight based on a mean ");
                    if (model.Weight == 0)
                        textWriter.Write($"(using the normalised weight, 8% gut fill) of {generator.DisplaySummaryValueSnippet(newInd?.Weight.NormalisedForAge??0, warnZero:true)} kg");
                    else
                        textWriter.Write($"of {generator.DisplaySummaryValueSnippet(model.Weight, warnZero: true)} kg");
                    textWriter.Write($" with a standard deviation of {generator.DisplaySummaryValueSnippet(model.WeightSD, warnZero: true)} kg");
                }
                else
                {
                    textWriter.Write($"{((model.Number > 1) ? "These individuals " : "This individual ")} weigh{((model.Number > 1) ? "" : "s")}");
                    if (model.Weight == 0)
                        textWriter.Write($" the normalised weight of {generator.DisplaySummaryValueSnippet(newInd?.Weight.NormalisedForAge ?? 0, warnZero: true)} kg");
                    else
                        textWriter.Write($" {generator.DisplaySummaryValueSnippet(model.Weight, warnZero: true)} kg");
                }
                generator.AddBlockWithText(textWriter.ToString());
            }

            if (model.Weight > 0 && (newInd is null || (model.Weight > 0 && Math.Abs(model.Weight - newInd.Weight.NormalisedForAge) / newInd.Weight.NormalisedForAge > 0.2)))
                generator.AddBlockWithText($"Individuals should weigh close to the normalised weight of {generator.DisplaySummaryValueSnippet(newInd?.Weight.NormalisedForAge ?? 0, warnZero: true)} kg for their age.", "infoBanner warning");

            if (model.ManagedPastureName != "Not specified")
            {
                generator.AddBlockWithText($"These individuals will be placed on the pasture {generator.DisplaySummaryValueSnippet(model.ManagedPastureName, entryStyle: HTMLSummaryStyle.Resource)}");
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
                            textWriter.Write($"Buy {generator.DisplaySummaryValueSnippet(model.Number, warnZero: true)} x ");
                        }
                        if (model.AgeDetails.InDays >= 0)
                        {
                            textWriter.Write($"{generator.DisplaySummaryValueSnippet(model.AgeDetails.ToDescriptionString())} old");
                        }
                        else
                        {
                            textWriter.Write($"{generator.DisplaySummaryValueSnippet(model.AgeDetails.InDays, warnZero:true)} day old");
                        }
                        textWriter.Write($"{generator.DisplaySummaryValueSnippet(model.Sex)}{((model.Number > 1 | parentIsSpecify) ? "s" : "")}");
                        textWriter.Write($" weighing");
                        if (model.Weight > 0)
                        {
                            textWriter.Write($"{generator.DisplaySummaryValueSnippet(model.Weight, warnZero: true)} kg");
                            if (model.WeightSD > 0)
                            {
                                textWriter.Write($" with a standard deviation of {generator.DisplaySummaryValueSnippet(model.WeightSD, warnZero: true)} kg");
                            }
                        }
                        else
                        {
                            textWriter.Write($"{generator.DisplaySummaryValueSnippet("Normalised Weight for age")}");
                        }
                        if (model.Sire || model.Suckling)
                        {
                            textWriter.Write(" and ");
                            textWriter.Write(model.Sire ? generator.DisplaySummaryValueSnippet("IsSire") : "");
                            if (model.Suckling)
                                textWriter.Write($"{generator.DisplaySummaryValueSnippet("Suckling", spanClass:$"entryValue {(model.Sire ? "errorValue" : "")}")}");
                        }
                        generator.AddBlockWithText(textWriter.ToString(), "componentContentNoBanner clearfix"); // "resourcebanneralone clearfix");
                    }
                    break;
                case "RuminantInitialCohorts":
                    RuminantType rumtype = model.Structure.FindParent<RuminantType>(recurse: true);
                    if (rumtype != null)
                    {
                        RandomNumberGenerator rng = model.Structure.Find<RandomNumberGenerator>();
                        if (rng is not null)
                        {
                            RandomNumberGenerator.SetForPreSimulation();
                        }
                        rumtype.Parameters.Initialise(rumtype);
//                            var generalParams = rumtype.Structure.FindChild<RuminantParametersGeneral>(recurse: true);

                        Ruminant newInd = null;
                        if (rumtype.Parameters.General is not null)
                            //Create(, int age, double weight = 0, int ? id = null, RuminantTypeCohort cohortDetails = null, IEnumerable < ISetAttribute > initialAttributes = null, SetPreviousConception previousConception = null)
                            newInd = Ruminant.Create(model.Sex, new(2000, 1, 1), rumtype.Parameters, model.AgeDetails.InDays, cohortDetails: model);

                        string normWtString = newInd?.Weight.NormalisedForAge.ToString("#,##0") ?? "Unavailable";
                        if (newInd is null || (model.Weight != 0 && Math.Abs(model.Weight - newInd.Weight.NormalisedForAge) / newInd.Weight.NormalisedForAge > 0.2))
                        {
                            normWtString = $"{generator.DisplaySummaryValueSnippet(normWtString, spanClass: "entryValue warningValue")}";
                            (model.Parent as RuminantInitialCohorts).WeightWarningOccurred = true;
                        }
                        string weightstring = "";
                        if (model.Weight > 0)
                            weightstring = $"{generator.DisplaySummaryValueSnippet($"{model.Weight}{((model.WeightSD > 0) ? $" \00B1 {model.WeightSD}" : "")}")}";


                        List<(string label, bool fill)> cellValues = [
                            (model.Name, false),
                            (generator.DisplaySummaryValueSnippet(model.Sex.ToString()), false),
                            (generator.DisplaySummaryValueSnippet(string.Join(',', model.AgeDetails.Parts)), false),
                            (weightstring, false),
                            (normWtString, false),
                            (generator.DisplaySummaryValueSnippet(Convert.ToInt32(model.Number), warnZero:true), false),
                            ("", model.Suckling),
                            ("", model.Sire)
                        ];

                        if ((model.Parent as RuminantInitialCohorts).ConceptionsFound)
                        {
                            string conceptionDetails = "";
                            var setConceptionFound = model.Structure.FindChild<SetPreviousConception>();
                            if (setConceptionFound != null)
                                conceptionDetails = $"{generator.DisplaySummaryValueSnippet(setConceptionFound.NumberDaysPregnant)} days";
                            cellValues.Add((conceptionDetails, true));
                        }

                        if ((model.Parent as RuminantInitialCohorts).AttributesFound)
                        {
                            var setAttributesFound = model.Structure.FindChildren<SetAttributeWithValue>();
                            string attributes = "";
                            foreach (var attribute in setAttributesFound)
                            {
                                attributes += generator.DisplaySummaryValueSnippet(attribute.AttributeName);
                            }
                            cellValues.Add((attributes, setAttributesFound.Any()));
                        }

                        generator.AddTableRow(cellValues, ModelTyped.Enabled);
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
    public override void CreateSummaryOpeningBlocks()
    {
        if (!FormatForParentControl)
            base.CreateSummaryOpeningBlocks();
    }

}