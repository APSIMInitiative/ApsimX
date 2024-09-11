using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using System.Linq;
using Models.Functions;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation for BoundFunction model
    /// </summary>
    public class DocBoundFunction : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocBoundFunction" /> class.
        /// </summary>
        public DocBoundFunction(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int heading = 0)
        {
            List<ITag> tags = base.Document(heading);
            
            List<ITag> subTags = new List<ITag>();
            List<IFunction> childFunctions = model.FindAllChildren<IFunction>().ToList();

            IFunction Upper = null;
            IFunction Lower = null;
            foreach (IFunction child in childFunctions)
            {
                if (child.Name.CompareTo("Upper") == 0)
                    Upper = child;
                if (child.Name.CompareTo("Lower") == 0)
                    Lower = child;
            }

            foreach (IFunction child in childFunctions)
            {
                if (child != Upper && child != Lower)
                {
                    subTags.Add(new Paragraph($"{model.Name} is the value of {child.Name} bound between a lower and upper bound where:"));
                    subTags = AutoDocumentation.Document(child, heading + 1).ToList();
                }
            }
            if (Lower != null)
                subTags = AutoDocumentation.Document(Lower, heading + 1).ToList();
            if (Upper != null)
                subTags = AutoDocumentation.Document(Upper, heading + 1).ToList();

            tags.Add(new Section(model.Name, subTags));

            return tags;
        }
    }
}
