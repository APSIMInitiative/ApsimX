using Models.CLEM.Groupings;
using System;
using System.IO;

namespace Models.CLEM.DescriptiveSummary.Groupings
{
    /// <summary>
    /// Descriptive summary provider for TakeFromFiltered
    /// </summary>
    public class TakeFromFilteredSummary : FilterSummaryBase<TakeFromFiltered>
    {
        /// <inheritdoc/>
        public override string FilterString(bool htmltags)
        {
            using StringWriter takeWriter = new();
            string cssSet = "";
            string cssError = "";
            string cssClose = "";
            if (htmltags)
            {
                cssSet = "<span class = \"filterset\">";
                cssError = "<span class = \"filtererror\">";
                cssClose = "</span>";
            }
            bool isTake = (ModelTyped.TakeStyle.ToString().Contains("Take"));
            bool isIndividuals = (ModelTyped.TakeStyle == TakeFromFilterStyle.TakeIndividuals || ModelTyped.TakeStyle == TakeFromFilterStyle.SkipIndividuals);
            takeWriter.Write((isTake) ? $"Take: " : "Skip: ");
            string errorString = "";
            if (ModelTyped.Value < 0 || (isIndividuals & ModelTyped.Value > 1))
            {
                errorString = "Invalid";
            }

            if (errorString != "")
            {
                takeWriter.Write($"{cssError}{errorString}{cssClose}");
            }
            else
            {
                takeWriter.Write(cssSet);
                takeWriter.Write((!isIndividuals ? ModelTyped.Value.ToString("P0") : $"{Convert.ToInt32(ModelTyped.Value)}"));
                takeWriter.Write(cssClose);
                takeWriter.WriteLine(((!isIndividuals) ? "" : " individuals"));
            }
            return takeWriter.ToString();
        }

    }
}