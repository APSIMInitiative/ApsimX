using DocumentFormat.OpenXml.Drawing;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Extras
{
    internal class RandomNumberGeneratorSummary : DescriptiveSummaryProviderBase<RandomNumberGenerator>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            string output = "Random numbers are provided for this simulation with ";
            if (ModelTyped.Seed == 0)
            {
                output += "every run using a different sequence.";
            }
            else
            {
                output += $"each run identical by using the seed {CLEMModel.DisplaySummaryValueSnippet(ModelTyped.Seed)}";
            }
            generator.AddBlockWithText("activityentry", output);
        }
    }
}
