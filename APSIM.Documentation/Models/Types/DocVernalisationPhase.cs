using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using System.Linq;
using Models.PMF.Phen;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocVernalisationPhase : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocGenericWithChildren" /> class.
        /// </summary>
        public DocVernalisationPhase(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            if (tags == null)
                tags = new List<ITag>();

            var paragraph = new Paragraph(
                $"The {model.Name} phase goes from the {(model as VernalisationPhase).Start} stage to the {(model as VernalisationPhase).End}" +
                $" stage and reaches {(model as VernalisationPhase).End} when vernalisation saturation occurs.");

            tags.Add(new Section("VernalisationPhase", paragraph));
            return tags;
        }
    }
}


