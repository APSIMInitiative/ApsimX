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
    public class DocBiomassArbitrationFunction : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocBiomassArbitrationFunction" /> class.
        /// </summary>
        public DocBiomassArbitrationFunction(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            List<ITag> newTags = base.Document(tags, headingLevel, indent).ToList();

            Organ parentOrgan = FindParentOrgan(model.Parent);

            // add a description of the equation for this function
            newTags.Add(new Paragraph("<i>" + model.Name + " = [" + parentOrgan.Name + "].maximumNconc × (["
                + parentOrgan.Name + "].Live.Wt + potentialAllocationWt) - [" + parentOrgan.Name + "].Live.N</i>", indent));
            newTags.Add(new Paragraph("The demand for storage N is further reduced by a factor specified by the ["
                + parentOrgan.Name + "].NitrogenDemandSwitch.", indent));

            return newTags;
        }

        private Organ FindParentOrgan(IModel model)
        {
            if (model is Organ) return model as Organ;

            if (model is IPlant)
                throw new Exception(model.Name + "cannot find parent organ to get Structural and Storage DM status");

            return FindParentOrgan(model.Parent);
        }
    }
}
