using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.CLEM.Extras;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Activities;

/// <summary>
/// Descriptive summary provider for Ruminant Activity Purchase
/// </summary>
public class RuminantActivityPurchaseSummary : RuminantActivitySummaryBase<RuminantActivityPurchase>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        generator.AddBlockWithText($"Purchased individuals will be placed in {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.GrazeFoodStoreName, "Not specified - general yards", HTMLSummaryStyle.Resource)}");

        Relationship numberRelationship = ModelTyped.Structure.FindChildren<Relationship>().Where(a => a.Identifier == "Number to stock vs pasture").FirstOrDefault();
        if (string.IsNullOrWhiteSpace(ModelTyped.GrazeFoodStoreName) == false && ModelTyped.GrazeFoodStoreName.StartsWith("Not specified") == false && numberRelationship != null)
        {
            generator.AddBlockWithText( $"The relationship {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(numberRelationship.Name)} will be used to calculate numbers purchased based on destination pasture biomass (t\\ha)");
        }
        else
        {
            generator.AddBlockWithText($"{DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.NumberToPurchase, warnZero: true)} individuals will be purchased with cohorts able to determine proportional breakdown.");
        }

        if (!string.IsNullOrWhiteSpace(model.TagLabel))
            generator.AddBlockWithText($"Purchased individuals will be assigned the tag {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.TagLabel)} for identification.");
    }
}