using System.Collections.Generic;
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
        public DocPhenology(IModel model) : base(model) { }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int heading = 0)
        {
            List<ITag> tags = base.Document(heading);

            List<ITag> subTags = new()
            {
                new Paragraph("."),
                new Section("List of Plant Model Components",
                    new Table((model as Phenology).GetPhaseTable())),
            };

            tags.Add(new Section("Phenology", subTags));
            return tags;
        }
    }
}