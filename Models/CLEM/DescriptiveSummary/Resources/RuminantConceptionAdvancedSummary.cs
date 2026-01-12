using Models.CLEM.Resources;
using System.Text;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for RuminantConceptionAdvanced
    /// </summary>
    public class RuminantConceptionAdvancedSummary : DescriptiveSummaryProviderBase<RuminantConceptionAdvanced>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            // Most advanced conception implementations provide their own ModelSummary override.

            Generator.AddBlockWithText("activityentry", $"Conception rates are being calculated for first pregnancy before 12 months, between 12-24 months and after 24 months as well as 2nd calf and 3rd or later calf using the following values.");

            StringBuilder sb = new StringBuilder();
            string[] names = new string[]
            {
                "First calf < 12 months",
                "First calf 12-24 months",
                "Second calf",
                "Third or later calf"
            };

            sb.AppendLine($"{generator.GetIndentTabs}<table><tr><th>Status</th><th>Asymptote</th><th>Coefficient</th><th>Intercept</th></tr>");

            for (int i = 0; i < names.Length; i++)
            {
                sb.Append($"{generator.GetIndentTabs}<tr><td>{names[i]}</td>");
                for (int j = 0; j < 4; j++)
                {
                    double value = j switch
                    {
                        0 => model.ConceptionRateAsymptote[i],
                        1 => model.ConceptionRateCoefficent[i],
                        2 => model.ConceptionRateIntercept[i],
                        _ => 0
                    };
                    sb.Append($"<td>{CLEMModel.DisplaySummaryValueSnippet(value, warnZero: true)}</td>");
                }
                sb.AppendLine("</tr>");
            }
            sb.AppendLine($"{generator.GetIndentTabs}</table>");
            Generator.Append(sb.ToString());
        }
    }
}