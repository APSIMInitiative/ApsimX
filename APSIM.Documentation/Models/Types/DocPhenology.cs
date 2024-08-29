using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.PMF.Phen;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for Phenology
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
            List<ITag> newTags = base.Document(tags, headingLevel, indent).ToList();

            List<ITag> subTags = new()
            {
                // Write Phenology stage table.
                new Paragraph("**List of Plant Model Components.**"),
                new Table((model as Phenology).GetPhaseTable()),
            };

            newTags.Add(new Section("Phenology", subTags));

            return newTags;
        }
    }
}