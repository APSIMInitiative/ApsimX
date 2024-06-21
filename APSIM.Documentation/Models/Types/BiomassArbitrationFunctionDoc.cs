using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;
using Models.PMF;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class BiomassArbitrationFunctionDoc : GenericDoc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BiomassArbitrationFunctionDoc" /> class.
        /// </summary>
        public BiomassArbitrationFunctionDoc(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            if (tags == null)
                tags = new List<ITag>();
            
            // add a heading
            tags.Add(new Heading(model.Name, headingLevel));

            // get description of this class
            AutoDocumentation.DocumentModelSummary(model, tags, headingLevel, indent, false);

            // write memos
            foreach (IModel memo in model.FindAllChildren<Memo>())
                AutoDocumentation.Document(memo, tags, headingLevel + 1, indent);

            Organ parentOrgan = FindParentOrgan(model.Parent);

            // add a description of the equation for this function
            tags.Add(new Paragraph("<i>" + model.Name + " = [" + parentOrgan.Name + "].maximumNconc × (["
                + parentOrgan.Name + "].Live.Wt + potentialAllocationWt) - [" + parentOrgan.Name + "].Live.N</i>", indent));
            tags.Add(new Paragraph("The demand for storage N is further reduced by a factor specified by the ["
                + parentOrgan.Name + "].NitrogenDemandSwitch.", indent));

            return tags;
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
