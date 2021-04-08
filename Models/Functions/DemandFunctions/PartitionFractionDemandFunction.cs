using Models.Core;
using Models.PMF;
using Models.PMF.Interfaces;
using System;
using APSIM.Services.Documentation;
using System.Collections.Generic;

namespace Models.Functions.DemandFunctions
{
    /// <summary>
    /// Returns the product of its PartitionFraction and the total DM supplied to the arbitrator by all organs.
    /// </summary>
    [Serializable]
    [Description("Demand is calculated as a fraction of the total plant supply term.")]
    public class PartitionFractionDemandFunction : Model, IFunction
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

        /// <summary>
        /// Document the model.
        /// </summary>
        /// <param name="indent">Indentation level.</param>
        /// <param name="headingLevel">Heading level.</param>
        protected override IEnumerable<ITag> Document(int indent, int headingLevel)
        {
            if (IncludeInDocumentation)
            {
                // add a heading
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write memos
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // add a description of the equation for this function
                tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " = PartitionFraction � [Arbitrator].DM.TotalFixationSupply</i>", indent));

                // write children
                tags.Add(new AutoDocumentation.Paragraph("Where:", indent));
                foreach (IModel child in this.FindAllChildren<IFunction>())
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent+1);
            }
        }
    }
}


