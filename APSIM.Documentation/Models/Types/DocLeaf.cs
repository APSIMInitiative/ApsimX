using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;
using Models.PMF.Organs;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocLeaf : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocGenericWithChildren" /> class.
        /// </summary>
        public DocLeaf(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document()
        {
            Section section = GetSummaryAndRemarksSection(model);

            List<ITag> dmfTags = new()
            {
                new Paragraph(
                        "The most important DM supply from leaf is the photosynthetic fixation supply.\n\n" +
                        "Radiation interception is calculated from LAI using an extinction coefficient of:\n\n")
            };

            IModel extinctionCoeff = (model as Leaf).FindChild("ExtinctionCoeff");
            IModel extinctionCoefficient = (model as Leaf).FindChild("ExtinctionCoefficient");

            if (extinctionCoefficient != null)
                dmfTags.AddRange(AutoDocumentation.Document(extinctionCoefficient));
            else if (extinctionCoeff != null)
                dmfTags.AddRange(AutoDocumentation.Document(extinctionCoeff));
            dmfTags.AddRange(AutoDocumentation.Document((model as Leaf).FindChild("Photosynthesis")));

            var dmfSection = new Section("Dry Matter Fixation", dmfTags);
            section.Add(dmfSection);

            return new List<ITag>() {section};
        }
    }
}
