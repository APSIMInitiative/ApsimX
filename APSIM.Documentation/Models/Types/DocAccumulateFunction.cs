using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocAccumulateFunction : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocAccumulateFunction" /> class.
        /// </summary>
        public DocAccumulateFunction(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            List<ITag> newTags = base.Document(tags, headingLevel, indent).ToList();
            
            string list = ChildFunctionList(model);

            newTags.Add(new Paragraph($"*{model.Name}* = Accumulated {list} and  between {(model as AccumulateFunction).StartStageName.ToLower()} and {(model as AccumulateFunction).EndStageName.ToLower()}"));

            foreach (var child in model.Children)
                AutoDocumentation.Document(child, newTags, headingLevel+1, indent+1);

            return newTags;
        }

        /// <summary> creates a list of child function names </summary>
        private static string ChildFunctionList(IModel model)
        {
            List<IFunction> childFunctions = model.FindAllChildren<IFunction>().ToList();

            string output = "";
            int total = childFunctions.Count;
            for(int i = 0; i < childFunctions.Count; i++)
            {
                output += "*" + childFunctions[i].Name + "*";                    
                if (i < total - 1)
                    output += ", ";
            }

            return output;
        }
    }
}
