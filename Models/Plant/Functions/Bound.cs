// -----------------------------------------------------------------------
// <copyright file="BoundFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Bounds the child function between lower and upper bounds
    /// </summary>
    [Serializable]
    [Description("Bounds the child function between lower and upper bounds")]
    public class BoundFunction : BaseFunction, ICustomDocumentation
    {
        /// <summary>The child functions</summary>
        [ChildLink]
        private List<IFunction> childFunctions = null;

        [ChildLinkByName]
        private IFunction Lower = null;

        [ChildLinkByName]
        private IFunction Upper = null;

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            foreach (IFunction child in childFunctions)
            {
                if (child != Lower && child != Upper)
                {
                    double[] lowerValues = Lower.Values();
                    double[] upperValues = Upper.Values();
                    double[] values = child.Values();

                    if (lowerValues.Length == 1 && values.Length > 1)
                        lowerValues = MathUtilities.CreateArrayOfValues(lowerValues[0], values.Length);
                    if (upperValues.Length == 1 && values.Length > 1)
                        upperValues = MathUtilities.CreateArrayOfValues(upperValues[0], values.Length);
                    if (lowerValues.Length > 1 && lowerValues.Length != values.Length ||
                        upperValues.Length > 1 && upperValues.Length != values.Length)
                        throw new Exception("In function: " + Name + " cannot perform a bound on different length arrays.");

                    for (int i = 0; i < values.Length; i++)
                        values[i] = Math.Max(Math.Min(upperValues[i], values[i]), lowerValues[i]);
                    return values;
                }
            }
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
                foreach (IModel memo in childFunctions.OfType<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, -1, indent);
                foreach (IFunction child in childFunctions)
                    if (child != Lower && child != Upper)
                    {
                        tags.Add(new AutoDocumentation.Paragraph(Name + " is the value of " + (child as IModel).Name + " bound between a lower and upper bound where:", indent));
                        AutoDocumentation.DocumentModel(child as IModel, tags, -1, indent + 1);
                    }
                if (Lower != null)
                    AutoDocumentation.DocumentModel(Lower as IModel, tags, -1, indent + 1);
                if (Upper != null)
                    AutoDocumentation.DocumentModel(Upper as IModel, tags, -1, indent + 1);
            }
        }
    }
}