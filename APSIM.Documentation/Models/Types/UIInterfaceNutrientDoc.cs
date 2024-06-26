using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;
using Models.Functions;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class UIInterfaceNutrientDoc : GenericDoc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UIInterfaceNutrientDoc" /> class.
        /// </summary>
        public UIInterfaceNutrientDoc(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            if (tags == null)
                tags = new List<ITag>();
            
            // add a heading
            tags.Add(new Heading(model.Name, headingLevel));

            // get description of this class.
            tags.Add(new Paragraph("This is the collection of functions for calculating the demands for each of the biomass pools (Structural, Metabolic, and Storage).", indent));

            // write memos.
            foreach (IModel memo in model.FindAllChildren<Memo>())
                AutoDocumentation.Document(memo, tags, headingLevel + 1, indent);

            // write children.
            foreach (IModel child in model.FindAllChildren<IFunction>())
                AutoDocumentation.Document(child, tags, headingLevel + 1, indent);

            return tags;
        }
    }
}
