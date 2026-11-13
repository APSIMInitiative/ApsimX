using System;
using System.Collections.Generic;
using APSIM.Documentation.Tag;
using Models.Core;
using Models.PMF;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocCompositeBiomass : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocCompositeBiomass" /> class.
        /// </summary>
        public DocCompositeBiomass(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            string organs = "";
            foreach (string name in (model as CompositeBiomass).OrganNames)
                organs += $"- {name}" + Environment.NewLine;

            section.Add(new Paragraph($"{model.Name} summarises the following biomass objects:", model));
            section.Add(new Paragraph(organs, model));

            return new List<ITag>() {section};
        }
    }
}
