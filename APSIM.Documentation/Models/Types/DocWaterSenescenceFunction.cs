using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocWaterSenescenceFunction : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocWaterSenescenceFunction" /> class.
        /// </summary>
        public DocWaterSenescenceFunction(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int heading = 0)
        {
            WaterSenescenceFunction waterSenFuncModel = model as WaterSenescenceFunction;
            List<ITag> tags = base.Document(heading);
            
            List<ITag> subTags = new List<ITag>();
            subTags.AddRange(AutoDocumentation.Document(waterSenFuncModel.FindChild<IFunction>("senWaterTimeConst")));
            subTags.AddRange(AutoDocumentation.Document(waterSenFuncModel.FindChild<IFunction>("senThreshold")));
            subTags.Add(new Paragraph("SDRatio is the Water Supply divided by the Water Demand (found in Arbitrator). It will return 1.0 unless there is less Supply than Demand"));
            (tags[0] as Section).Children.AddRange(subTags);

            return tags;
        }
    }
}
