using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using System.Linq;
using Models.Functions;
using Models.Interfaces;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for SubDailyInterpolation
    /// </summary>
    public class DocSubDailyInterpolation : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocSubDailyInterpolation" /> class.
        /// </summary>
        public DocSubDailyInterpolation(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            List<ITag> newTags = base.Document(tags, headingLevel, indent).ToList();
            
            List<ITag> subTags = new List<ITag>();

            SubDailyInterpolation sub = model as SubDailyInterpolation;
            IIndexedFunction Response = sub.FindChild<IIndexedFunction>("Response");
            IInterpolationMethod InterpolationMethod = sub.FindChild<IInterpolationMethod>("InterpolationMethod");

            if (Response != null)
            {
                subTags.Add(new Paragraph($"{model.Name} is the {sub.agregationMethod.ToString().ToLower()} of sub-daily values from a {Response.GetType().Name}."));
                subTags.Add(new Paragraph($"Each of the interpolated {InterpolationMethod.OutputValueType}s are then passed into the following Response and the {sub.agregationMethod} taken to give daily {sub.Name}"));
                subTags.AddRange(AutoDocumentation.Document(Response, subTags, headingLevel+1, indent+1));
                subTags.AddRange(AutoDocumentation.Document(InterpolationMethod as IModel, subTags, headingLevel+1, indent+1));
            }
            newTags.Add(new Section(model.Name, subTags));

            return newTags;
        }
    }
}
