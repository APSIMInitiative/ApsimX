using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using APSIM.Shared.Utilities;
using System.Globalization;

namespace Models.Functions
{
    /// <summary>
    /// From the value of the first child function, subtract the values of the subsequent children functions
    /// </summary>
    [Serializable]
    [Description("From the value of the first child function, subtract the values of the subsequent children functions")]
    public class SubtractFunction : Model, IFunction, ICustomDocumentation
    {
        /// <summary>The child functions</summary>
        private List<IModel> ChildFunctions;
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = Apsim.Children(this, typeof(IFunction));

            double returnValue = 0.0;
            if (ChildFunctions.Count > 0)
            {
                IFunction F = ChildFunctions[0] as IFunction;
                returnValue = F.Value(arrayIndex);

                if (ChildFunctions.Count > 1)
                {
                    for (int i = 1; i < ChildFunctions.Count; i++)
                    {
                        F = ChildFunctions[i] as IFunction;
                        returnValue = returnValue - F.Value(arrayIndex);
                    }
                }
            }
            return returnValue;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
                DocumentMathFunction(this, '-', tags, headingLevel, indent);
        }

        /// <summary>
        /// Document the mathematical function.
        /// </summary>
        /// <param name="function">The IModel function.</param>
        /// <param name="op">The operator</param>
        /// <param name="tags">The tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public static void DocumentMathFunction(IModel function, char op,
                                                List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        { 
            // add a heading
            tags.Add(new AutoDocumentation.Heading(function.Name, headingLevel));

            // write memos
            foreach (IModel memo in Apsim.Children(function, typeof(Memo)))
                AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

            // create a string to display 'child1 - child2 - child3...'
            string msg = string.Empty;
            List<IModel> childrenToDocument = new List<IModel>();
            foreach (IModel child in Apsim.Children(function, typeof(IFunction)))
            {
                if (msg != string.Empty)
                    msg += " " + op + " ";

                if (!AddChildToMsg(child, ref msg))
                    childrenToDocument.Add(child);
            }

            tags.Add(new AutoDocumentation.Paragraph("<i>" + function.Name + " = " + msg + "</i>", indent));

            // write children
            if (childrenToDocument.Count > 0)
            {
                tags.Add(new AutoDocumentation.Paragraph("Where:", indent));

                foreach (IModel child in childrenToDocument)
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent + 1);
            }
        }

        /// <summary>
        /// Return the name of the child or it's value if the name of the child is equal to 
        /// the written value of the child. i.e. if the value is 1 and the name is 'one' then
        /// return the value, instead of the name.
        /// </summary>
        /// <param name="child">The child model.</param>
        /// <param name="msg">The message to add to.</param>
        /// <returns>True if child's value was added to msg.</returns>
        private static bool AddChildToMsg(IModel child, ref string msg)
        {
            if (child is Constant)
            {
                double doubleValue = (child as Constant).FixedValue;
                if (Math.IEEERemainder(doubleValue, doubleValue) == 0)
                {
                    int intValue = Convert.ToInt32(doubleValue, CultureInfo.InvariantCulture);
                    string writtenInteger = Integer.ToWritten(intValue);
                    writtenInteger = writtenInteger.Replace(" ", "");  // don't want spaces.
                    if (writtenInteger.Equals(child.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        msg += intValue.ToString();
                        return true;
                    }
                }
            }
            else if (child is VariableReference)
            {
                msg += StringUtilities.RemoveTrailingString((child as VariableReference).VariableName, ".Value()");
                return true;
            }

            msg += child.Name;
            return false;
        }
    }
}