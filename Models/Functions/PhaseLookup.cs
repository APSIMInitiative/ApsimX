using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.IO;
using System.Linq;

namespace Models.Functions
{
    /// <summary>
    /// Look up a value based upon the current growth phase.
    /// </summary>
    [Serializable]
    [Description("A value is chosen according to the current growth phase.")]
    public class PhaseLookup : Model, IFunction, ICustomDocumentation
    {
        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();

            foreach (IFunction F in ChildFunctions)
            {
                PhaseLookupValue P = F as PhaseLookupValue;
                if (P.InPhase)
                    return P.Value(arrayIndex);
            }
            return 0;  // Default value is zero
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
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel+1, indent);

                tags.Add(new AutoDocumentation.Paragraph(this.Name + " is calculated using specific values or functions for various growth phases.  The function will use a value of zero for phases not specified below.", indent));

                // write children.
                foreach (IModel child in this.FindAllChildren<IFunction>())
                    AutoDocumentation.DocumentModel(child, tags, headingLevel+1, indent + 1);
            }
        }
    }
}


