using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.PMF;

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
            List<ITag> tags = new List<ITag>();

            foreach (ITag tag in GetSummaryAndRemarksSection(model).Children)
                tags.Add(tag);
            
            if (model.FindAllChildren<Folder>("Validation").Any())
            {
                // Find a single instance of all unique Plant models.
                var plants = model.FindAllDescendants<Plant>().DistinctBy(p => p.Name.ToUpper());
                foreach(Plant plant in plants)
                {
                    tags.AddRange(AutoDocumentation.Document(plant));
                }
            }

            foreach (IModel child in model.FindAllChildren<Folder>())
            {
                if(child.Name != "Replacements")
                    tags.AddRange(AutoDocumentation.Document(child));
            }
            return tags;
        }
    }
}
