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

            foreach (ITag tag in model.Children.SelectMany(c => c.Document()))
                newTags.Add(tag);

            string type = "Max";
            if (model is MinimumFunction)
                type = "Min";

            foreach (ITag tag in DocumentMinMaxFunction(type, model.Name, model.Children))
                newTags.Add(tag);

            return newTags;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        private static IEnumerable<ITag> DocumentMinMaxFunction(string functionName, string name, IEnumerable<IModel> children)
        {
            foreach (var child in children.Where(c => c is Memo))
                foreach (var tag in child.Document())
                    yield return tag;

            var writer = new StringBuilder();
            writer.Append($"*{name}* = {functionName}(");

            bool addComma = false;
            foreach (var child in children.Where(c => !(c is Memo)))
            {
                if (addComma)
                    writer.Append($", ");
                writer.Append($"*" + child.Name + "*");
                addComma = true;
            }
            writer.Append(')');
            yield return new Paragraph(writer.ToString());

            yield return new Paragraph("Where:");

            foreach (var child in children.Where(c => !(c is Memo)))
                foreach (var tag in child.Document())
                    yield return tag;
        }
    }
}
