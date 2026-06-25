using DocumentFormat.OpenXml.Drawing.Charts;
using Models.CLEM.Activities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Other Animals Activity Breed
/// </summary>
public class OtherAnimalsActivityBreedSummary : DescriptiveSummaryProviderBase<OtherAnimalsActivityBreed>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText($"{generator.DisplaySummaryValueSnippet(ModelTyped.AnimalTypeName, "No Other Animal Type", HTMLSummaryStyle.Resource)}" +
            $" individuals must be {generator.DisplaySummaryValueSnippet(ModelTyped.BreedingAge, "Mature age not set", HTMLSummaryStyle.Default)} months of age to breed.");

        string output = $"Breeding will occur regardless of whether adult males are present in the local population";
        if (ModelTyped.UseLocalMales)
        {
            output = "Breeding will only occur when adult males are present in the local population.";
        }
        generator.AddBlockWithText(output);
        generator.AddBlockWithText($"Each breeding female will produce {generator.DisplaySummaryValueSnippet(ModelTyped.OffspringPerBreeder, "Offspring not set", HTMLSummaryStyle.Default)} offspring with an equal sex ratio and rounded to whole individuals");
    }
}
