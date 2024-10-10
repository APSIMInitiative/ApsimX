using System.Collections.Generic;
using System.IO;
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
        private static string PATH_VALIDATION = "/Tests/Validation/";
        private static string PATH_TUTORIAL = "/Examples/Tutorials/";

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
            Simulations sims = model as Simulations;
            
            if (sims.FileName.Contains(PATH_VALIDATION) || sims.FileName.Contains(PATH_VALIDATION.Replace('/', '\\')))
            {
                tags.AddRange(DocumentValidation(model as Simulations));
            }
            else if (sims.FileName.Contains(PATH_TUTORIAL) || sims.FileName.Contains(PATH_TUTORIAL.Replace('/', '\\')))
            {
                tags.AddRange(DocumentTutorial(model as Simulations));
            }
            else
            {
                foreach(IModel child in sims.FindAllChildren())
                {
                    tags.AddRange(AutoDocumentation.DocumentModel(child));
                }
            }

            return tags;
        }

        private List<ITag> DocumentValidation(Model m)
        {
            List<ITag> tags = new List<ITag>();

            // Find a single instance of all unique Plant models.
            var plants = model.FindAllDescendants<Plant>().DistinctBy(p => p.Name.ToUpper());
            List<ITag> plantTags = new List<ITag>();
            foreach(Plant plant in plants)
            {
                plantTags.AddRange(AutoDocumentation.DocumentModel(plant));
            }
            
            if (plantTags.Count > 0)
            {
                string title = Path.GetFileNameWithoutExtension((m as Simulations).FileName);
                plantTags = DocumentationUtilities.AddHeader(title, plantTags);
                tags.AddRange(plantTags);
            }
            

            //Then just document the folders that aren't replacements
            foreach (IModel child in model.FindAllChildren<Folder>())
            {
                if(child.Name != "Replacements")
                    tags.AddRange(AutoDocumentation.DocumentModel(child));
            }

            return tags;
        }

        private List<ITag> DocumentTutorial(Model m)
        {
            List<ITag> tags = new List<ITag>();
            foreach(IModel child in m.FindAllChildren())
            {
                if (child is Simulation)
                {
                    tags.AddRange(DocumentTutorial(child as Simulation));
                } 
                else if(child is Memo || child is Graph || (child is Folder && child.Name != "Replacements"))
                {
                    tags.AddRange(AutoDocumentation.DocumentModel(child));
                }
            }

            return tags;
        }
    }
}
