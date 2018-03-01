// -----------------------------------------------------------------------
// <copyright file="LessThanFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using Models.Core;
    using System;
    using System.Collections.Generic;

    /// <summary>Value returned is determined according to given criteria</summary>
    [Serializable]
    public class LessThanFunction : BaseFunction, ICustomDocumentation
    {
        /// <summary>The child functions</summary>
        [ChildLink]
        private List<IFunction> childFunctions = null;
        
        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            double testVariable = 0.0;
            double lessThanCriteria = 0.0;
            double ifTrue = 0.0;
            double ifFalse = 0.0;

            IFunction f = null;

            for (int i = 0; i < childFunctions.Count; i++)
            {
                f = childFunctions[i] as IFunction;
                if (i == 0)
                    testVariable = f.Value();
                if (i == 1)
                    lessThanCriteria = f.Value();
                if (i == 2)
                    ifTrue = f.Value();
                if (i == 3)
                    ifFalse = f.Value();
            }

            if (testVariable < lessThanCriteria)
                return new double[] { ifTrue };
            else
                return new double[] { ifFalse };
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

                string lhs;
                if (childFunctions[0] is VariableReference)
                    lhs = (childFunctions[0] as VariableReference).VariableName;
                else
                    lhs = (childFunctions[0] as IModel).Name;
                string rhs;
                if (childFunctions[1] is VariableReference)
                    rhs = (childFunctions[1] as VariableReference).VariableName;
                else
                    rhs = (childFunctions[1] as IModel).Name;

                tags.Add(new AutoDocumentation.Paragraph("IF " + lhs + " < " + rhs + " THEN", indent));
                AutoDocumentation.DocumentModel((childFunctions[2] as IModel), tags, headingLevel, indent + 1);
                tags.Add(new AutoDocumentation.Paragraph("ELSE", indent));
                AutoDocumentation.DocumentModel((childFunctions[3] as IModel), tags, headingLevel, indent + 1);
            }
        }
    }
}