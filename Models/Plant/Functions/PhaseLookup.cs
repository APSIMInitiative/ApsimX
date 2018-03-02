// ----------------------------------------------------------------------
// <copyright file="PhaseLookup.cs" company="APSIM Initiative">
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

    /// <summary>
    /// Look up a value based upon the current growth phase.
    /// </summary>
    [Serializable]
    [Description("A value is chosen according to the current growth phase.")]
    public class PhaseLookup : BaseFunction, ICustomDocumentation
    {
        /// <summary>The value being returned</summary>
        private double[] zero = new double[1] { 0 };

        /// <summary>All child functions</summary>
        [ChildLink]
        private List<IFunction> childFunctions = null;

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            foreach (IFunction F in childFunctions)
            {
                PhaseLookupValue P = F as PhaseLookupValue;
                if (P.InPhase)
                {
                    double[] returnValues = P.Values();
                    Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:" + StringUtilities.BuildString(returnValues, "F3"));
                    return returnValues;
                }
            }
            Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:0");
            return zero;  // Default value is zero
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

                // write children.
                foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
                    AutoDocumentation.DocumentModel(child, tags, -1, indent + 1);

                tags.Add(new AutoDocumentation.Paragraph(this.Name + " has a value of zero for phases not specified above ", indent + 1));
            }
        }
    }
}


