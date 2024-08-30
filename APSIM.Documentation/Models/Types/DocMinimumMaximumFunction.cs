using System.Collections.Generic;
using System.Linq;
using System.Text;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;
using Models.Functions;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocMinimumMaximumFunction : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocMinimumMaximumFunction" /> class.
        /// </summary>
        public DocMinimumMaximumFunction(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            List<ITag> newTags = base.Document(tags, headingLevel, indent).ToList();

            foreach (IModel child in model.FindAllChildren())
                newTags.AddRange(AutoDocumentation.Document(child, newTags, headingLevel+1, indent+1).ToList());

            string type = "Max";
            if (model is MinimumFunction)
                type = "Min";

            newTags.AddRange(DocumentMinMaxFunction(type, model.Name, model.Children));

            return newTags;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        private static List<ITag> DocumentMinMaxFunction(string functionName, string name, IEnumerable<IModel> children)
        {
            List<ITag> newTags = new List<ITag>();

            var writer = new StringBuilder();
            writer.Append($"*{name}* = {functionName}(");

            bool addComma = false;
            foreach (IModel child in children)
            {
                if (children as Memo == null)
                {
                    if (addComma)
                        writer.Append($", ");
                    writer.Append($"*" + child.Name + "*");
                    addComma = true;
                }
            }
            writer.Append(')');
            
            newTags.Add(new Paragraph(writer.ToString()));

            newTags.Add(new Paragraph("Where:"));

            return newTags;
        }
    }
}
