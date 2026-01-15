using Models.CLEM.Resources;
using Models.CLEM.Timers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Graze Food Store Type Fertility Limiter
/// </summary>
public class GrazeFoodStoreFertilityLimiterSummary : DescriptiveSummaryProviderBase<GrazeFoodStoreFertilityLimiter>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        bool timerpresent = model.Structure.FindChildren<ActivityTimerMonthRange>().Any();
        var parentPasture = model.Parent as GrazeFoodStoreType;

        generator.AddBlockWithText("activityentry", $"The nitrogen content of new pasture will be reduced by {generator.DisplaySummaryValueSnippet(model.NitrogenReduction, warnZero:true, errorString:"Not Set")} if:");

        string monthSet = "";
        if (model.AnnualYieldStartMonth == MonthsOfYear.NotSet)
            monthSet = "<span class=\"errorlink\">Month not set</span>";
        else
            monthSet = generator.DisplaySummaryValueSnippet(model.AnnualYieldStartMonth.ToString());

        generator.AddBlockWithText("activityentry", $"<b>(A)</b> An annual nitrogen supply of {generator.DisplaySummaryValueSnippet(model.AnnualNitrogenSupply, warnZero: true, errorString: "Not Set")} kg per hectare has been used since {monthSet}");
        if (model.AnnualNitrogenSupply > 0)
        {
            if (parentPasture.GreenNitrogenPercent > 0)
                generator.AddBlockWithText("activityentry", $"This equates to {generator.DisplaySummaryValueSnippet(model.AnnualNitrogenSupply / (parentPasture.GreenNitrogenPercent / 100))} kg per hectare of pasture production given the new growth nitrogen content of {generator.DisplaySummaryValueSnippet(parentPasture.GreenNitrogenPercent, warnZero:true)}.");
            else
                generator.AddBlockWithText("activityentry", $"This equates to <span class=\"errorlink\">Undefined</span> kg per hectare of pasture production given the green growth nitrogen content of <span class=\"errorlink\">Not set</span>.");
        }

        if (timerpresent)
        {
            generator.AddBlockWithText("activityentry", $"or <b>(B)</b> the growth month falls within the specified period below:");
        }
        else
        {
            generator.AddBlockWithText("activityentry", "or<b>(B) </ b > Add a ActivityMonthRangeTimer below to reduce nitrogen content in specified months");
        }
    }
}
