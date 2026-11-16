using System.Collections.Generic;
using APSIM.Documentation.Tag;
using Models.Core;
using Models.PMF.Interfaces;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for StorageNDemandFunction
    /// </summary>
    public class DocStorageNDemandFunction : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocStorageNDemandFunction" /> class.
        /// </summary>
        public DocStorageNDemandFunction(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            IOrgan parentOrgan = model.Node.FindParent<IOrgan>(recurse: true);
            if (parentOrgan != null)
            {
                string organName = parentOrgan.Name;
                section.Add(new Paragraph($"*{model.Name} = [{organName}].maximumNConc × ([{organName}].Live.Wt + potentialAllocationWt) - [{organName}].Live.N*", model));
                section.Add(new Paragraph($"The demand for storage N is further reduced by a factor specified by the [{organName}].NitrogenDemandSwitch.", model));
            }

            return new List<ITag>() {section};
        }
    }
}
