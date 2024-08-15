using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.PMF.Phen;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocPhenology : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocPhenology" /> class.
        /// </summary>
        public DocPhenology(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            if (tags == null)
                tags = new List<ITag>();

            List<ITag> subTags = new()
            {
                new Paragraph(CodeDocumentation.GetSummary(model.GetType())),
                new Paragraph(CodeDocumentation.GetRemarks(model.GetType())),
                // Write Phenology stage table.
                new Paragraph("**List of Plant Model Components.**"),
                new Table((model as Phenology).GetPhaseTable()),
            };

            tags.Add(new Section("Phenology", subTags));

            return tags;
        }
    }
}