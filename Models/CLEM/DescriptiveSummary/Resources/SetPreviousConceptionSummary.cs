using Models.CLEM.Resources;
using System.Globalization;
using System.IO;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for SpecifyRuminant
/// </summary>
public class SpecifyPreviousConceptionSummary : DescriptiveSummaryProviderBase<SetPreviousConception>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public SpecifyPreviousConceptionSummary()
    {
        SummaryStyle = HTMLSummaryStyle.SubResource;
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        if (FormatForParentControl)
        {
            generator.AddBlockWithText($"These individuals will be {generator.DisplaySummaryValueSnippet<int>(model.NumberDaysPregnant, warnZero: true)} days pregnant", "resourcebanneralone");
        }
        else
        {
            generator.AddBlockWithText($"Set last conception age to make these females {generator.DisplaySummaryValueSnippet<int>(model.NumberDaysPregnant, warnZero: true)} days pregnant");
        }
    }
}