// -----------------------------------------------------------------------
// <copyright file="MaximumFunction.cs" company="APSIM Initiative">
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

    /// <summary>Maximize the values of the children of this node</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval Maximum value of all children of this node. Return -999999999 if no child.
    [Serializable]
    [Description("Returns the maximum value of all childern functions")]
    public class MaximumFunction : BaseFunction, ICustomDocumentation
    {
        /// <summary>All child functions</summary>
        [ChildLink]
        private List<IFunction> childFunctions = null;

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            double[] returnValues = null;
            foreach (IFunction child in childFunctions)
            {
                double[] values = child.Values();
                if (returnValues == null)
                    returnValues = values;
                else
                {
                    if (returnValues.Length == 1 && values.Length > 1)
                        returnValues = MathUtilities.CreateArrayOfValues(returnValues[0], values.Length);
                    else if (values.Length == 1 && returnValues.Length > 1)
                        values = MathUtilities.CreateArrayOfValues(values[0], returnValues.Length);
                    else if (returnValues.Length > 1 && returnValues.Length != values.Length)
                        throw new Exception("In function: " + Name + " cannot perform a minimum on different length arrays.");

                    for (int i = 0; i < values.Length; i++)
                        returnValues[i] = Math.Max(returnValues[i], values[i]);
                }
            }

            Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:" + StringUtilities.BuildString(returnValues, "F3"));
            return returnValues;
        }

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
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, -1, indent);

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
                    AutoDocumentation.DocumentModel(child, tags, -1, indent + 1);
            }
        }
    }
}