using Models.Core;
using Models.PMF;
using Models.PMF.Interfaces;
using System;
using System.Collections.Generic;

namespace Models.Functions.DemandFunctions
{
    /// <summary>
    /// Returns the product of its PartitionFraction and the total DM supplied to the arbitrator by all organs.
    /// </summary>
    [Serializable]
    [Description("Demand is calculated as a fraction of the total plant supply term.")]
    public class PartitionFractionDemandFunction : Model, IFunction, ICustomDocumentation
    {
        /// <summary>The partition fraction</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction PartitionFraction = null;

        /// <summary>The arbitrator</summary>
        [Link]
        IArbitrator arbitrator = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arbitrator.DM != null)
                return arbitrator.DM.TotalFixationSupply * PartitionFraction.Value(arrayIndex);
            else
                return 0;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write memos
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // add a description of the equation for this function
                tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " = PartitionFraction � [Arbitrator].DM.TotalFixationSupply</i>", indent));

                // write children
                tags.Add(new AutoDocumentation.Paragraph("Where:", indent));
                foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent+1);
            }
        }
    }
}


