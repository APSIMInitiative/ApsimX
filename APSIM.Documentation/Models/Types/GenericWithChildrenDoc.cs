using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class GenericWithChildrenDoc : GenericDoc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlantDoc" /> class.
        /// </summary>
        public GenericWithChildrenDoc(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            if (tags == null)
                tags = new List<ITag>();
            
            List<ITag> subTags = new List<ITag>();
            subTags.Add(new Paragraph(CodeDocumentation.GetSummary(GetType())));
            subTags.Add(new Paragraph(CodeDocumentation.GetRemarks(GetType())));

            foreach (IModel child in model.FindAllChildren())
                subTags.Add(new Section(child.Name, child.Document()));

            tags.Add(new Section(model.Name, subTags));

            return tags;
        }
    }
}
