using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using M = Models;
using Models.Core;
using Models;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// A generic documentation class that does nothing except document documentation classes.
    /// No summary and remarks or other inclusions.
    /// </summary>
    public class DocGenericBlank : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocGeneric" /> class.
        /// </summary>
        public DocGenericBlank(IModel model): base(model) {}

        /// <summary>
        /// Document the model
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            List<ITag> tags = new List<ITag>();
            tags.Add(new Paragraph($"Source: {DocumentationUtilities.GetGithubMarkdownLink(model)}"));

            foreach (IModel child in model.Node.FindChildren<M.Documentation>())
                tags.AddRange(AutoDocumentation.DocumentModel(child).ToList());

            return tags;
        }
    }
}
