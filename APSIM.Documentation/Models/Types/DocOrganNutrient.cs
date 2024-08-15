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
    public class DocOrganNutrient : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocOrganNutrient" /> class.
        /// </summary>
        public DocOrganNutrient(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            if (tags == null)
                tags = new List<ITag>();
            
            // add a heading, the name of this organ
            tags.Add(new Heading(model.Name, headingLevel));

            // write the basic description of this class, given in the <summary>
            AutoDocumentation.DocumentModelSummary(model, tags, headingLevel, indent, false);

            // write the memos
            foreach (IModel memo in model.FindAllChildren<Memo>())
                AutoDocumentation.Document(memo, tags, headingLevel + 1, indent);

            return tags;
        }
    }
}
