using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
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
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            List<ITag> newTags = base.Document(tags, headingLevel, indent).ToList();

            string organs = "";
            foreach (string name in (model as CompositeBiomass).OrganNames)
                organs += $"- {name}" + Environment.NewLine;

            List<ITag> subTags = new List<ITag>();
            subTags.Add(new Paragraph($"{model.Name} summarises the following biomass objects:"));
            subTags.Add(new Paragraph(organs));

            newTags.Add(new Section("Organs", subTags));

            return newTags;
        }
    }
}
