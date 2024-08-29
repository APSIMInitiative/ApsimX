using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using Models;
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
        public DocPhenology(IModel model) : base(model) { }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
             if (tags == null)
                tags = new List<ITag>();
                
            List<ITag> memoDocs = new();
            foreach (Memo memo in model.FindAllChildren<Memo>().ToList())
                 memoDocs.AddRange(AutoDocumentation.Document(memo));

            List<ITag> newTags = new()
            {
                new Section(memoDocs),
                // Required to get sections properly aligned.
                new Paragraph("."),
                new Section("List of Plant Model Components",
                    new Table((model as Phenology).GetPhaseTable())),
            };

            tags.Add(new Section("Phenology", newTags));
            return tags;
        }
    }
}