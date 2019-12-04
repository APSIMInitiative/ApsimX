using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.Functions
{
    /// <summary>Value returned is determined according to given criteria</summary>
    [Serializable]
    [Description("Tests if value of the first child is less than value of second child. Returns third child if true and forth if false")]
    public class LessThanFunction : Model, IFunction, ICustomDocumentation
    {
        /// <summary>The child functions</summary>
        private List<IModel> ChildFunctions;
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            ChildFunctions = Apsim.Children(this, typeof(IFunction));

            double TestVariable = 0.0;
            double LessThanCriteria = 0.0;
            double IfTrue = 0.0;
            double IfFalse = 0.0;

            IFunction F = null;

            for (int i = 0; i < ChildFunctions.Count; i++)
            {
                F = ChildFunctions[i] as IFunction;
                if (i == 0)
                    TestVariable = F.Value(arrayIndex);
                if (i == 1)
                    LessThanCriteria = F.Value(arrayIndex);
                if (i == 2)
                    IfTrue = F.Value(arrayIndex);
                if (i == 3)
                    IfFalse = F.Value(arrayIndex);
            }

            if (TestVariable < LessThanCriteria)
                return IfTrue;
            else
                return IfFalse;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(IFunction));

                if (ChildFunctions == null || ChildFunctions.Count < 1)
                    return;

                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                string lhs;
                if (ChildFunctions[0] is VariableReference)
                    lhs = (ChildFunctions[0] as VariableReference).VariableName;
                else
                    lhs = ChildFunctions[0].Name;
                string rhs;
                if (ChildFunctions[1] is VariableReference)
                    rhs = (ChildFunctions[1] as VariableReference).VariableName;
                else
                    rhs = ChildFunctions[1].Name;

                tags.Add(new AutoDocumentation.Paragraph("IF " + lhs + " < " + rhs + " THEN", indent));
                AutoDocumentation.DocumentModel(ChildFunctions[2], tags, headingLevel, indent + 1);
                tags.Add(new AutoDocumentation.Paragraph("ELSE", indent));
                AutoDocumentation.DocumentModel(ChildFunctions[3], tags, headingLevel, indent + 1);
            }
        }
    }
}