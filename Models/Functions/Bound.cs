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
        public override IEnumerable<ITag> Document(int indent, int headingLevel)
        {
            yield return new Heading(Name, indent, headingLevel);

            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>();
            foreach (IFunction child in ChildFunctions)
            {
                if (child != Lower && child != Upper)
                {
                    yield return new Paragraph($"{Name} is the value of {child.Name} bound between a lower and upper bound where:", indent);
                    foreach (ITag tag in child.Document(indent + 1, headingLevel + 1))
                        yield return tag;
                    break;
                }
            }
            if (Lower != null)
                foreach (ITag tag in Lower.Document(indent + 1, headingLevel + 1))
                    yield return tag;
            if (Upper != null)
                foreach (ITag tag in Upper.Document(indent + 1, headingLevel + 1))
                    yield return tag;
        }
    }
}
