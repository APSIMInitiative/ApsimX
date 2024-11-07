using System.Collections.Generic;
using System.IO;
using System.Linq;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;
using Graph = Models.Graph;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocSimulations : DocGeneric
    {
        private static string PATH_REVIEW = "/Tests/UnderReview/";
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
            
            if (!string.IsNullOrEmpty(sims.FileName))
            {
                if (sims.FileName.Contains(PATH_REVIEW) || sims.FileName.Contains(PATH_REVIEW.Replace('/', '\\')) ||
                    sims.FileName.Contains(PATH_VALIDATION) || sims.FileName.Contains(PATH_VALIDATION.Replace('/', '\\')))
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
            }

            return tags;
        }

        private List<ITag> DocumentValidation(Model m)
        {
            string name = Path.GetFileNameWithoutExtension((m as Simulations).FileName);
            string title = "The APSIM " + name + " Model";

            if (name.ToLower() == "SpeciesTable".ToLower()) //This is a special case file, we may want to review this in the future to remove/merge it
                return (new DocSpeciesTable(m)).Document();

            List<ITag> tags = new List<ITag>();
            List<ITag> modelTags = new List<ITag>();

            List<Memo> memos = m.FindAllChildren<Memo>().ToList();
            List<ITag> memoTags = new List<ITag>();
            if (name.ToLower() != "wheat")          //Wheat has the memo in both the validation and resource, so don't do it for that.
                    foreach (IModel child in memos)
                        memoTags.AddRange(AutoDocumentation.DocumentModel(child));

            // Find a single instance of all unique Plant models.
            IModel modelToDocument = m.FindDescendant(name);
            if (modelToDocument != null)
            {
                modelTags.AddRange(AutoDocumentation.DocumentModel(modelToDocument));
            }

            //Sort out heading
            Section firstSection = new Section(title, memoTags);
            foreach(ITag tag in modelTags)
            {
                if (tag.GetType() == typeof(Section))
                {
                    foreach(ITag subtag in (tag as Section).Children)
                        firstSection.Add(subtag);
                }
                else if (tag.GetType() == typeof(Paragraph))
                {
                    firstSection.Add(tag);
                }
            }
            
            tags.Add(firstSection);

            //Then just document the folders that aren't replacements
            foreach (IModel child in m.FindAllChildren<Folder>())
            {
                if(child.Name != "Replacements")
                    tags.AddRange(AutoDocumentation.DocumentModel(child));
            }

            tags.AddRange(AddAdditionals(name));

            return tags;
        }

        private List<ITag> DocumentTutorial(Model m)
        {
            List<ITag> tags = new List<ITag>();

            if (m is not Simulation)
            {
                string name = Path.GetFileNameWithoutExtension((m as Simulations).FileName);
                string title = name + " Tutorial";
                //Sort out heading
                var firstSection = new Section(title, new List<ITag>() { new Paragraph("----") });
                tags.Add(firstSection);
            }

            foreach(IModel child in m.FindAllChildren())
            {
                if (child is Simulation)
                { 
                    tags.Add(new Section(child.Name, DocumentTutorial(child as Simulation)));
                } 
                else if(child is Memo || child is Graph || (child is Folder && child.Name != "Replacements"))
                {
                    tags.AddRange(AutoDocumentation.DocumentModel(child));
                }
            }

            return tags;
        }

        /// <summary>
        /// Adds extra documents or media for specific apsimx file documents.
        /// </summary>
        private static List<ITag> AddAdditionals(string modelName)
        {
            List<ITag> additionsTags = new();

            // TODO: Handle AgPasture file names 
            if (modelName == "AgPasture")
            {

            }

            Dictionary<string, DocAdditions> validationAdditions = new()
            {
                {"AGPRyegrass", new DocAdditions(
                    "https://apsimdev.apsim.info/ApsimX/Documents/AgPastureScience.pdf", 
                    "https://builds.apsim.info/api/nextgen/docs/SpeciesTable.pdf")},
                {"AGPWhiteClover", new DocAdditions("https://apsimdev.apsim.info/ApsimX/Documents/AgPastureScience.pdf")},
                {"Canola", new DocAdditions(videoLink: "https://www.youtube.com/watch?v=kz3w5nOtdqM")},
                {"MicroClimate", new DocAdditions("https://www.apsim.info/wp-content/uploads/2019/09/Micromet.pdf")},
                {"Mungbean", new DocAdditions(videoLink:"https://www.youtube.com/watch?v=nyDZkT1JTXw")},
                {"Stock", new DocAdditions("https://grazplan.csiro.au/wp-content/uploads/2007/08/TechPaperMay12.pdf")},
                {"SWIM", new DocAdditions("https://apsimdev.apsim.info/ApsimX/Documents/SWIMv21UserManual.pdf")},
            };

            if(validationAdditions.ContainsKey(modelName))
            {
                DocAdditions additions = validationAdditions[modelName];
                if(additions.ScienceDocLink != null)
                {
                    Section scienceSection = new Section("Science Documentation", new Paragraph($"![View science documentation here]({additions.ScienceDocLink})"));   
                    additionsTags.Add(scienceSection);
                }
            }

            return additionsTags;
        }

        /// <summary>
        /// Stores additional resources for model documentation.
        /// </summary>
        public class DocAdditions
        {
            /// <summary>
            /// Link to science documentation.
            /// </summary>
            public string ScienceDocLink {get; private set;}

            /// <summary>
            /// Link to a related video.
            /// </summary>
            public string VideoLink {get; private set;}

            /// <summary>
            /// Extra resource link.
            /// </summary>
            public string ExtraLink {get; private set;}

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="scienceDocLink"></param>
            /// <param name="videoLink"></param>
            /// <param name="extraLink"></param>
            public DocAdditions(string scienceDocLink = null, string videoLink = null, string extraLink = null)
            {
                ScienceDocLink = scienceDocLink;
                VideoLink = videoLink;
                ExtraLink = extraLink;
            }
        }
    }
}
