using System.Linq;
using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for RuminantHerd
    /// </summary>
    public class RuminantHerdSummary : DescriptiveSummaryProviderBase<RuminantHerd>
    {
        /// <inheritdoc/>
        public override void BuildSummary(RuminantHerd model)
        {
            string text = "Activities reporting on herds will group individuals";
            switch (model.TransactionStyle)
            {
                case RuminantTransactionsGroupingStyle.Combined:
                    text += " into a single transaction per RuminantType.";
                    break;
                case RuminantTransactionsGroupingStyle.ByPriceGroup:
                    text += " by the pricing groups provided for the RuminantType.";
                    break;
                case RuminantTransactionsGroupingStyle.ByClass:
                    text += " by the class of individuals.";
                    break;
                case RuminantTransactionsGroupingStyle.BySexAndClass:
                    text += " by the sex and class of individuals.";
                    break;
                case RuminantTransactionsGroupingStyle.ByFullClass:
                    text += " by the full class of individuals.";
                    break;
                case RuminantTransactionsGroupingStyle.BySexAndFullClass:
                    text += " by the sex and full class of individuals.";
                    break;
                default:
                    text += " by [Unknown grouping style]";
                    break;
            }
            Generator.AddBlockWithText("activityentry", text);
        }
    }
}