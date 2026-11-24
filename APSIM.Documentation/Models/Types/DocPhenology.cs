using System.Collections.Generic;
using APSIM.Documentation.Tag;
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
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            List<ITag> subTags = new()
            {
                new Paragraph(".", model),
                new Section("List of Plant Model Components",
                    new Table(DocumentationUtilities.GetPhaseTable(model as Phenology), model),
                    model),
            };

            foreach(IPhase child in model.Node.FindChildren<IPhase>())
            {
                if (child as GenericPhase == null)
                {
                    Section S = GetSummaryAndRemarksSection(child);
                    subTags.Add(S);
                }
                else
                {
                    Section S = GetSummaryAndRemarksSection(child);
                    subTags.Add(S);                    
                }
            }

            section.Add(new Section("Phenology", subTags, model));
            return new List<ITag>() {section};
        }
    }
}