using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using M = Models;
using Models.Core;
using Models;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocGeneric
    {
        /// <summary>
        /// The model that the documentation should be generated for
        /// </summary>
        protected IModel model = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocGeneric" /> class.
        /// </summary>
        public DocGeneric(IModel model)
        {
            this.model = model;
        }

        /// <summary>
        /// Document the model
        /// </summary>
        public virtual List<ITag> Document(int none = 0)
        {
            return new List<ITag>() {GetSummaryAndRemarksSection(model)};
        }

        /// <summary>
        /// Get a section with the Summary, Remarks and Memo text included.
        /// </summary>
        public static Section GetSectionTitle(IModel model) {

            List<ITag> tags = new List<ITag>();
            foreach (IModel child in model.Node.FindChildren<Memo>())
                tags = AutoDocumentation.DocumentModel(child).ToList();

            return new Section(model.Name, tags);
        }

        /// <summary>
        /// Get a section with the Summary, Remarks and Memo text included.
        /// </summary>
        public static Section GetSummaryAndRemarksSection(IModel model) {

            List<ITag> tags = new List<ITag>();
            tags.Add(new Paragraph(CodeDocumentation.GetSummary(model.GetType())));
            tags.Add(new Paragraph(CodeDocumentation.GetRemarks(model.GetType())));

            if (model.Node != null)
            {
                bool hasDocumentation = false;
                if (model.Node.FindParent<Simulations>(recurse:true).Node.FindChild<M.Documentation>(recurse:true) != null)
                    hasDocumentation = true;

                if (!hasDocumentation)
                {
                    foreach (IModel child in model.Node.FindChildren<Memo>(recurse:true))
                        tags.AddRange(AutoDocumentation.DocumentModel(child).ToList());
                }
                else
                {
                    foreach (IModel child in model.Node.FindChildren<M.Documentation>(recurse:true))
                        tags.AddRange(AutoDocumentation.DocumentModel(child).ToList());
                }
            }

            return new Section(model.Name, tags);
        }
    }
}
