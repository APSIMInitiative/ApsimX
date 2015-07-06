using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// A function that adds values from child functions
    /// </summary>
    [Serializable]
    [Description("Add the values of all child functions")]
    public class AddFunction : Model, IFunction
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

                foreach (IFunction F in ChildFunctions)
                {
                    returnValue = returnValue + F.Value;
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

            // create a string to display 'child1 + child2 + child3...'
            string msg = string.Empty;
            foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
            {
                if (msg != string.Empty)
                    msg += " + ";
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