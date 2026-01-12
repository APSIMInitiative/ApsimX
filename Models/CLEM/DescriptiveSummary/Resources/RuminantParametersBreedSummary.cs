using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    internal class RuminantParametersBreedSummary : DescriptiveSummaryProviderBase<RuminantParametersBreeding>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            Generator.AddBlockWithText("activityentry", $"Oestrus cycle is {CLEMModel.DisplaySummaryValueSnippet(model.OestrusCycleLength, warnZero: true)} days with {CLEMModel.DisplaySummaryValueSnippet(model.DaysInHeat, warnZero:true)} days in heat");
            if (model.ProportionOffspringMale != 0.5)
                Generator.AddBlockWithText("activityentry", $"Proportion of offspring male of {CLEMModel.DisplaySummaryValueSnippet(model.ProportionOffspringMale, warnZero: true)} is not 0.5");
            if (model.AllowFreemartins)
                Generator.AddBlockWithText("activityentry", $"Freemartins are produced");
            if (model.ConceptionDuringLactationProbability < 1.0)
                Generator.AddBlockWithText("activityentry", $"Conception rate is multiplied by {CLEMModel.DisplaySummaryValueSnippet(model.ConceptionDuringLactationProbability)} during lactation");
            if (model.DystociaCoefficients.Sum(a => a) > 0)
                Generator.AddBlockWithText("activityentry", $"Mortality from dystocia is included");
        }

        /// <inheritdoc/>
        public override void CreateSummaryInnerOpeningBlocksBeforeSummary()
        {
            if (!FormatForParentControl)
                Generator.AddBlockWithText("detailsnote", $"General breeding parameters used by multiple activities and growth components.");
        }

    }
}
