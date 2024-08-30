using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using System.Linq;
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
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            List<ITag> newTags = base.Document(tags, headingLevel, indent).ToList();
            
            List<ITag> subTags = new List<ITag>();

            IOrgan parentOrgan = model.FindAncestor<IOrgan>();
            if (parentOrgan != null)
            {
                string organName = parentOrgan.Name;
                subTags.Add(new Paragraph($"*{model.Name} = [{organName}].maximumNconc × ([{organName}].Live.Wt + potentialAllocationWt) - [{organName}].Live.N*"));
                subTags.Add(new Paragraph($"The demand for storage N is further reduced by a factor specified by the [{organName}].NitrogenDemandSwitch."));
            }

            newTags.Add(new Section(model.Name, subTags));

            return newTags;
        }
    }
}
