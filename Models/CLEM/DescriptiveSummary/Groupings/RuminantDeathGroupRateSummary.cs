using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Ruminant Group filter
/// </summary>
public class RuminantDeathGroupRateSummary : GroupSummaryBase<RuminantDeathGroupRate>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        switch (ModelTyped.Style)
        {
            case ParameterStyle.GetFromParameters:
                htmlWriter.Write($"The annual mortality rates for the specified individuals each time-step are provided in the following breed parameter files: ");
                foreach (var rumtype in ModelTyped.Structure.FindAll<RuminantType>())
                {
                    htmlWriter.Write(rumtype.Name);
                    if (rumtype.Parameters.GrowPF is not null)
                    {
                        htmlWriter.Write($".Parameters.GrowPF.BasalMortalityRate_CD1 = {rumtype.Parameters.GrowPF_CD.BasalMortalityRate_CD1}");
                    }
                    else if (rumtype.Parameters.Grow is not null)
                    {
                        htmlWriter.Write($".Parameters.Grow.MortalityBase = {rumtype.Parameters.Grow.MortalityBase}");
                    }
                    else
                    {
                        htmlWriter.Write($"{generator.DisplayErrorSnippet("Missing Grow or GrowPF parameters")}");
                    }
                }
                break;
            case ParameterStyle.Specify:
                htmlWriter.Write($"The annual mortality rate of {generator.DisplaySummaryValueSnippet(ModelTyped.Rate, warnZero: true)} will be applied to the specified individuals each time step to determine if death occurs.");
                break;
            default:
                break;
        }
        generator.AddBlockWithText("activityentry", htmlWriter.ToString());
    }
}
