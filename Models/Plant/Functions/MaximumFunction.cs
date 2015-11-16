using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>Maximize the values of the children of this node</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval Maximum value of all children of this node. Return -999999999 if no child.
    [Serializable]
    [Description("Returns the maximum value of all childern functions")]
    public class MaximumFunction : Model, IFunction
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

                double ReturnValue = -999999999;
                foreach (IFunction F in ChildFunctions)
                {
                    ReturnValue = Math.Max(ReturnValue, F.Value);
                }
                return ReturnValue;
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

            // create a string to display 'child1 - child2 - child3...'
            string msg = "";

            foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
            {
                if (msg != string.Empty)
                    msg += ", ";
                msg += child.Name;
            }

            tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " = maximum (" + msg + ")</i>", indent));


            tags.Add(new AutoDocumentation.Paragraph("Where:", indent));

            // write children.
            foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
                child.Document(tags, -1, indent + 1);

        }
    }
}