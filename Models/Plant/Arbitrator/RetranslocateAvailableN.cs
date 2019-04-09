using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
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
    [ValidParent(ParentType = typeof(GenericOrgan))]
    public class RetranslocateAvailableN : Model, IRetranslocateMethod, ICustomDocumentation
    {
        /// <summary>The calculation for N retranslocation function</summary>
        [ChildLinkByName]
        [Units("/d")]
        public IFunction RetranslocateFunction = null;

        /// <summary>The calculation for DM retranslocation function</summary>
        [ChildLinkByName]
        [Units("/d")]
        public IFunction RetranslocateDMFunction = null;

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        public double CalculateN(GenericOrgan organ)
        {
            return RetranslocateFunction.Value();
        }

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        public double CalculateBiomass(GenericOrgan organ)
        {
            return RetranslocateDMFunction.Value();
        }

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        /// <param name="biomass"></param>
        public void AllocateBiomass(GenericOrgan organ, BiomassAllocationType biomass)
        {
            //doing all non-structural allocation here for sorghum, as well as retranslocation
            //TODO JB refactor metabolic and storage allocation
            organ.Live.StorageWt += biomass.Storage;
            organ.Live.MetabolicWt += biomass.Metabolic;

            organ.Allocated.StorageWt += biomass.Storage;
            organ.Allocated.MetabolicWt += biomass.Metabolic;

            double remainingBiomass = biomass.Retranslocation;
            double storageRetranslocation = Math.Min(organ.Live.StorageWt, remainingBiomass);
            organ.Live.StorageWt -= storageRetranslocation;
            organ.Allocated.StorageWt -= storageRetranslocation;
            remainingBiomass -= storageRetranslocation;

            double metabolicRetranslocation = Math.Min(organ.Live.MetabolicWt, remainingBiomass);
            organ.Live.MetabolicWt -= metabolicRetranslocation;
            organ.Allocated.MetabolicWt -= metabolicRetranslocation;
            remainingBiomass -= metabolicRetranslocation;

            double structuralRetranslocation = Math.Min(organ.Live.StructuralWt, remainingBiomass);
            organ.Live.StructuralWt -= structuralRetranslocation;
            organ.Allocated.StructuralWt -= structuralRetranslocation;
            remainingBiomass -= structuralRetranslocation;
        }

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        /// <param name="nitrogen"></param>
        public void AllocateN(GenericOrgan organ, BiomassAllocationType nitrogen)
        {
            // Retranslocation
            if (MathUtilities.IsGreaterThan(nitrogen.Retranslocation, organ.Live.StructuralN + organ.Live.StorageN + organ.Live.MetabolicN - organ.NSupply.Retranslocation))
                throw new Exception("N retranslocation exceeds storage + metabolic nitrogen in organ: " + Name);

            double remainingN = nitrogen.Retranslocation;
            double storageRetranslocation = Math.Min(organ.Live.StorageN, remainingN);
            organ.Live.StorageN -= storageRetranslocation;
            organ.Allocated.StorageN -= storageRetranslocation;
            remainingN -= storageRetranslocation;

            double metabolicRetranslocation = Math.Min(organ.Live.MetabolicN, remainingN);
            organ.Live.MetabolicN -= metabolicRetranslocation;
            organ.Allocated.MetabolicN -= metabolicRetranslocation;
            remainingN -= metabolicRetranslocation;

            double structuralRetranslocation = Math.Min(organ.Live.StructuralN, remainingN);
            organ.Live.StructuralN -= structuralRetranslocation;
            organ.Allocated.StructuralN -= structuralRetranslocation;
            remainingN -= structuralRetranslocation;
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