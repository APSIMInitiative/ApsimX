using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using System;
using System.Collections.Generic;

namespace Models.PMF
{
    /// <summary>
    /// Process Retranslocation of BiomassType using Storage First and then Metabolic
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class RetranslocateNonStructural : Model, IRetranslocateMethod, ICustomDocumentation
    {

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        /// <param name="nitrogen"></param>
        public void Allocate(IOrgan organ, BiomassAllocationType nitrogen)
        {
            var genOrgan = organ as GenericOrgan;

            // Retranslocation
            if (MathUtilities.IsGreaterThan(nitrogen.Retranslocation, genOrgan.StartLive.StorageN + genOrgan.StartLive.MetabolicN - genOrgan.NSupply.Retranslocation))
                throw new Exception("N retranslocation exceeds storage + metabolic nitrogen in organ: " + Name);

            double storageRetranslocation = Math.Min(genOrgan.Live.StorageN, nitrogen.Retranslocation);
            genOrgan.Live.StorageN -= storageRetranslocation;
            genOrgan.Allocated.StorageN -= storageRetranslocation;

            double metabolicRetranslocation = nitrogen.Retranslocation - storageRetranslocation;
            genOrgan.Live.MetabolicN -= metabolicRetranslocation;
            genOrgan.Allocated.MetabolicN -= metabolicRetranslocation;

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
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // write description of this class.
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                string RelativeDocString = "Arbitration is performed in two passes for each of the supply sources.  On the first pass, biomass or nutrient supply is allocated to structural and metabolic pools of each organ based on their demand relative to the demand from all organs.  On the second pass any remaining supply is allocated to non-structural pool based on the organ's relative demand.";

                tags.Add(new AutoDocumentation.Paragraph(RelativeDocString, indent));
            }
        }
    }


}
