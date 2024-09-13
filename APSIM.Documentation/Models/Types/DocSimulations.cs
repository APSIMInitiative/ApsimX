using System.Collections.Generic;
using APSIM.Shared.Documentation;
using DocumentFormat.OpenXml.Bibliography;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocSimulations : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocSimulations" /> class.
        /// </summary>
        public DocSimulations(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            foreach (ITag tag in section.Children)
                section.Add(tag);
            foreach (IModel child in model.FindAllChildren<Folder>())
                section.Add(AutoDocumentation.Document(child));

            return new List<ITag>() {section};
        }
    }
}
