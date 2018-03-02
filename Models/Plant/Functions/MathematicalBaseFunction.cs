// -----------------------------------------------------------------------
// <copyright file="MathematicalBaseFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>An abstract base class for performing a mathematic operation (e.g. multiply, divide)</summary>
    [Serializable]
    public abstract class MathematicalBaseFunction : BaseFunction, ICustomDocumentation
    {
        /// <summary>All child functions</summary>
        [ChildLink]
        private List<IFunction> childFunctions = null;

        /// <summary>Gets the value, either a double or a double[]</summary>
        public override double[] Values()
        {
            double[] returnValues = null;
            for (int i = 0; i < childFunctions.Count(); i++)
            {
                double[] values = childFunctions[i].Values();
                if (returnValues == null)
                    returnValues = values;
                else
                {
                    if (returnValues.Length == 1 && values.Length > 1)
                        returnValues = MathUtilities.CreateArrayOfValues(returnValues[0], values.Length);
                    else if (values.Length == 1 && returnValues.Length > 1)
                        values = MathUtilities.CreateArrayOfValues(values[0], returnValues.Length);
                    else if (returnValues.Length > 1 && returnValues.Length != values.Length)
                        throw new Exception("In function: " + Name + " cannot perform a mathematical calculation on different length arrays.");

                    for (int j = 0; j < returnValues.Length; j++)
                        returnValues[j] = PerformOperation(returnValues[j], values[j]);
                }
            }

            Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:" + StringUtilities.BuildString(returnValues, "F3"));
            return returnValues;
        }


        /// <summary>Returns the character to insert into auto-generated documentation</summary>
        protected abstract char OperatorCharForDocumentation { get; }

        /// <summary>Perform the mathematical operation</summary>
        /// <param name="value1">The first value</param>
        /// <param name="value2">The second value</param>
        protected abstract double PerformOperation(double value1, double value2);

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write memos.
                foreach (IModel memo in childFunctions.OfType<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, -1, indent);

                // create a string to display 'child1 - child2 - child3...'
                string msg = string.Empty;
                List<IModel> childrenToDocument = new List<IModel>();
                foreach (IModel child in childFunctions.OfType<IFunction>())
                {
                    if (msg != string.Empty)
                        msg += " " + OperatorCharForDocumentation + " ";

                    if (!AddChildToMsg(child, ref msg))
                        childrenToDocument.Add(child);
                }

                tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " = " + msg + "</i>", indent));

                if (childrenToDocument.Count > 0)
                {
                    tags.Add(new AutoDocumentation.Paragraph("Where:", indent));

                    // write children.
                    foreach (IModel child in childrenToDocument)
                        AutoDocumentation.DocumentModel(child, tags, -1, indent + 1);
                }
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
                    int intValue = Convert.ToInt32(doubleValue);
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