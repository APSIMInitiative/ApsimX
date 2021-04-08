using System;
using APSIM.Services.Documentation;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using System.Linq;

namespace Models.Functions
{
    /// <summary>
    /// Bounds the child function between lower and upper bounds
    /// </summary>
    [Serializable]
    [Description("Bounds the child function between lower and upper bounds")]
    public class BoundFunction : Model, IFunction
    {
        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Lower = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Upper = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();
            foreach (IFunction child in ChildFunctions)
                if (child != Lower && child != Upper)
                    return Math.Max(Math.Min(Upper.Value(arrayIndex), child.Value(arrayIndex)),Lower.Value(arrayIndex));
            throw new Exception("Cannot find function value to apply in bound");
        }
        /// <summary>
        /// Document the model.
        /// </summary>
        /// <param name="indent">Indentation level.</param>
        /// <param name="headingLevel">Heading level.</param>
        protected override IEnumerable<ITag> Document(int indent, int headingLevel)
        {
            if (IncludeInDocumentation)
            {
                // write memos.
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);
                if (ChildFunctions == null)
                    ChildFunctions = this.FindAllChildren<IFunction>();
                foreach (IFunction child in ChildFunctions)
                    if (child != Lower && child != Upper)
                    {
                        tags.Add(new AutoDocumentation.Paragraph(Name + " is the value of " + (child as IModel).Name + " bound between a lower and upper bound where:", indent));
                        AutoDocumentation.DocumentModel(child as IModel, tags, headingLevel + 1, indent + 1);
                    }
                if (Lower != null)
                    AutoDocumentation.DocumentModel(Lower as IModel, tags, headingLevel + 1, indent + 1);
                if (Upper != null)
                    AutoDocumentation.DocumentModel(Upper as IModel, tags, headingLevel + 1, indent + 1);
            }
        }
    }
}