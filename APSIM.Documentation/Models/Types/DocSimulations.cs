using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;
using Models.PMF;
using Graph = Models.Graph;

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
            
            if (model.FindAllChildren<Folder>("Validation").Any())
            {
                foreach (ITag tag in GetSummaryAndRemarksSection(model).Children)
                    tags.Add(tag);

                // Find a single instance of all unique Plant models.
                var plants = model.FindAllDescendants<Plant>().DistinctBy(p => p.Name.ToUpper());
                foreach(Plant plant in plants)
                {
                    tags.AddRange(AutoDocumentation.Document(plant));
                }

                foreach (IModel child in model.FindAllChildren<Folder>())
                {
                    if(child.Name != "Replacements")
                        tags.AddRange(AutoDocumentation.Document(child));
                }
            }
            else
            {
                foreach(IModel child in model.FindAllChildren())
                {
                    if(child is Memo)
                    {
                        tags.AddRange(AutoDocumentation.Document(child));
                    }
                    else if (child is Simulation)
                    {
                        foreach(IModel simChild in child.FindAllChildren())
                        {
                                if (simChild is Memo)
                                    tags.AddRange(AutoDocumentation.Document(simChild));
                                else if (simChild is Graph)
                                {
                                    List<ITag> graphTags = AutoDocumentation.Document(simChild);
                                    (graphTags.First() as Section)?.Children?.RemoveAt(0);
                                    tags.AddRange(graphTags);
                                }
                        }
                    }
                    else if (child is Folder && child.Name != "Replacements")
                    {
                        tags.AddRange(AutoDocumentation.Document(child));
                    }
                    else if(child is Graph)
                    {
                        tags.AddRange(AutoDocumentation.Document(child));
                    }

                }
            }

            return tags;
        }
    }
}
