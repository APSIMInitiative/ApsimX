using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Starting with the first child function, recursively divide by the values of the subsequent child functions
    /// </summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval Starting with the first child function, recursively divide by the values of the subsequent child functions. Return 0 if no child. The value of first child if only one child.
    [Serializable]
    [Description("Starting with the first child function, recursively divide by the values of the subsequent child functions")]
    public class DivideFunction : Model, IFunction
    {
        /// <summary>The child functions</summary>
        private List<IModel> ChildFunctions;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(IFunction));

                double returnValue = 0.0;
                if (ChildFunctions.Count > 0)
                {
                    IFunction F = ChildFunctions[0] as IFunction;
                    returnValue = F.Value;

                    if (ChildFunctions.Count > 1)
                        for (int i = 1; i < ChildFunctions.Count; i++)
                        {
                            F = ChildFunctions[i] as IFunction;
                            returnValue = returnValue / F.Value;
                        }

                }
                return returnValue;
            }
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // add a heading.
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            // write memos.
            foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                memo.Document(tags, -1, indent);

            // create a string to display 'child1 / child2 / child3...'
            string msg = string.Empty;
            foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
            {
                if (msg != string.Empty)
                    msg += " / ";
                msg += child.Name;
            }

            tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " = " + msg + "</i>", indent));
            tags.Add(new AutoDocumentation.Paragraph("Where:", indent));

            // write children.
            foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
                child.Document(tags, -1, indent + 1);
        }

    }
}