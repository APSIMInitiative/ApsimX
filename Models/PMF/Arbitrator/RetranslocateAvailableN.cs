using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.PMF
{

    /// <summary>
    /// Process Retranslocation of BiomassType using Storage First and then Metabolic
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class RetranslocateAvailableN : Model, IRetranslocateMethod, ICustomDocumentation
    {
        /// <summary>The calculation for N retranslocation function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        public IFunction RetranslocateFunction = null;

        /// <summary>The calculation for DM retranslocation function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        public IFunction RetranslocateDMFunction = null;

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        public double Calculate(IOrgan organ)
        {
            return RetranslocateFunction.Value();
        }

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        public double CalculateBiomass(IOrgan organ)
        {
            return RetranslocateDMFunction.Value();
        }

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        /// <param name="biomass"></param>
        public void AllocateBiomass(IOrgan organ, BiomassAllocationType biomass)
        {
            //doing all non-structural allocation here for sorghum, as well as retranslocation
            //TODO JB refactor metabolic and storage allocation
            var genOrgan = organ as GenericOrgan;
            genOrgan.Live.StorageWt += biomass.Storage;
            genOrgan.Live.MetabolicWt += biomass.Metabolic;

            genOrgan.Allocated.StorageWt += biomass.Storage;
            genOrgan.Allocated.MetabolicWt += biomass.Metabolic;

            var remainingBiomass = biomass.Retranslocation;
            double storageRetranslocation = Math.Min(genOrgan.Live.StorageWt, remainingBiomass);
            genOrgan.Live.StorageWt -= storageRetranslocation;
            genOrgan.Allocated.StorageWt -= storageRetranslocation;
            remainingBiomass -= storageRetranslocation;

            double metabolicRetranslocation = Math.Min(genOrgan.Live.MetabolicWt, remainingBiomass);
            genOrgan.Live.MetabolicWt -= metabolicRetranslocation;
            genOrgan.Allocated.MetabolicWt -= metabolicRetranslocation;
            remainingBiomass -= metabolicRetranslocation;

            double structuralRetranslocation = Math.Min(genOrgan.Live.StructuralWt, remainingBiomass);
            genOrgan.Live.StructuralWt -= structuralRetranslocation;
            genOrgan.Allocated.StructuralWt -= structuralRetranslocation;
            remainingBiomass -= structuralRetranslocation;
        }

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        /// <param name="nitrogen"></param>
        public void Allocate(IOrgan organ, BiomassAllocationType nitrogen)
        {
            var genOrgan = organ as GenericOrgan;

            // Retranslocation
            if (MathUtilities.IsGreaterThan(nitrogen.Retranslocation, genOrgan.Live.StructuralN + genOrgan.Live.StorageN + genOrgan.Live.MetabolicN - genOrgan.NSupply.Retranslocation))
                throw new Exception("N retranslocation exceeds storage + metabolic nitrogen in organ: " + Name);

            var remainingN = nitrogen.Retranslocation;
            double storageRetranslocation = Math.Min(genOrgan.Live.StorageN, remainingN);
            genOrgan.Live.StorageN -= storageRetranslocation;
            genOrgan.Allocated.StorageN -= storageRetranslocation;
            remainingN -= storageRetranslocation;

            double metabolicRetranslocation = Math.Min(genOrgan.Live.MetabolicN, remainingN);
            genOrgan.Live.MetabolicN -= metabolicRetranslocation;
            genOrgan.Allocated.MetabolicN -= metabolicRetranslocation;
            remainingN -= metabolicRetranslocation;

            double structuralRetranslocation = Math.Min(genOrgan.Live.StructuralN, remainingN);
            genOrgan.Live.StructuralN -= structuralRetranslocation;
            genOrgan.Allocated.StructuralN -= structuralRetranslocation;
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
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // write description of this class.
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                string RelativeDocString = "Arbitration is performed in two passes for each of the supply sources.  On the first pass, biomass or nutrient supply is allocated to structural and metabolic pools of each organ based on their demand relative to the demand from all organs.  On the second pass any remaining supply is allocated to non-structural pool based on the organ's relative demand.";

                tags.Add(new AutoDocumentation.Paragraph(RelativeDocString, indent));
            }
        }
    }
}