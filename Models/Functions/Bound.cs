using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// Bounds the child function between lower and upper bounds
    /// </summary>
    [Serializable]
    [Description("Bounds the child function between lower and upper bounds")]
    public class BoundFunction : Model, IFunction, ICustomDocumentation
    {
        /// <summary>The child functions</summary>
        private List<IModel> ChildFunctions;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Lower = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Upper = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = Apsim.Children(this, typeof(IFunction));
            foreach (IFunction child in ChildFunctions)
                if (child != Lower && child != Upper)
                    return Math.Max(Math.Min(Upper.Value(arrayIndex), child.Value(arrayIndex)),Lower.Value(arrayIndex));
            throw new Exception("Cannot find function value to apply in bound");
        }
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(IFunction));
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