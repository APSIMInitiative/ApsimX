using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class ClockDoc : GenericDoc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericDoc" /> class.
        /// </summary>
        public ClockDoc(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            if (tags == null)
                tags = new List<ITag>();

            List<ITag> subTags = new List<ITag>();
            subTags.Add(new Paragraph(CodeDocumentation.GetSummary(model.GetType())));
            subTags.Add(new Paragraph(CodeDocumentation.GetRemarks(model.GetType())));
            tags.Add(new Section(model.Name, subTags));

            tags.Add(new Section(model.Name, GetModelEventsInvoked(typeof(Clock), "OnDoCommence(object _, CommenceArgs e)", "CLEM", true)));
            
            return tags;
        }
    }
}
