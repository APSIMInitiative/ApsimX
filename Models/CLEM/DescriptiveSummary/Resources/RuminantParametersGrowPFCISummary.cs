using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

internal class RuminantParametersGrowPFCISummary : RuminantParametersSummaryBase<RuminantParametersGrowPFCI>
{
    /// <inheritdoc/>
    public override List<(string componentName, string propertyName, string category, string description, string value)> GetCustomSummaryParameters()
    {
        //if (FormatForParentControl)
        //{
        //    if (RelativeConditionEffect_CI20 == 1.0)
        //    {
        //        htmlWriter.Write("\r\n<div class=\"warninglink\">");
        //        htmlWriter.Write($"Ruminant intake reduction based on high condition is disabled<br />To allow this functionality set [GrowPF CI].RelativeConditionEffect_CI20 to a value <span class=\"setvalue\">> 1</span> (default 1.5)");
        //        htmlWriter.Write("</div>");
        //        if (IgnoreFeedQualityIntakeAdustment)
        //            htmlWriter.Write("</br>");
        //    }
        //    if (IgnoreFeedQualityIntakeAdustment)
        //    {
        //        htmlWriter.Write("\r\n<div class=\"warninglink\">");
        //        htmlWriter.Write($"Ruminant intake reduction based on intake quality is disabled<br />To allow this functionality set [GrowPF CI].IgnoreFeedQualityIntakeAdustment to <span class=\"setvalue\">False</span>");
        //        htmlWriter.Write(" </div>");
        //    }
        //}
    }
}
