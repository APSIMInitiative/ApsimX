namespace Models.Soils.Nutrient
{
    using Interfaces;
    using Models.Core;
    using System;
    using APSIM.Shared.Utilities;
    using Models.SurfaceOM;
    using Models.Soils;

    /// <summary>
    /// Soil carbon model
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    public class Nutrient : Model, INutrient
    {

        /// <summary>The surface organic matter</summary>
        [Link]
        private SurfaceOrganicMatter SurfaceOrganicMatter = null;

        [ChildLinkByName]
        NutrientPool FOMCellulose = null;
        [ChildLinkByName]
        NutrientPool FOMCarbohydrate = null;
        [ChildLinkByName]
        NutrientPool FOMLignin = null;
        [ChildLinkByName]
        NutrientPool SurfaceResidue = null;
        [Link]
        private SoluteManager solutes = null;

        private SurfaceOrganicMatterDecompType PotentialSOMDecomp = null;

        /// <summary>
        /// 
        /// </summary>
        public double[] FOMCNR
        {
            get
            {
                double[] NH4 = solutes.GetSolute("NH4");
                double[] NO3 = solutes.GetSolute("NO3");

                double[] values = new double[FOMLignin.C.Length];
                for (int i = 0; i < FOMLignin.C.Length; i++)
                    values[i] = MathUtilities.Divide(FOMCarbohydrate.C[i] + FOMCellulose.C[i] + FOMLignin.C[i],
                               FOMCarbohydrate.N[i] + FOMCellulose.N[i] + FOMLignin.N[i] + NH4[i] + NO3[i], 0.0);

                return values;
            }
        }

        /// <summary>Partition the given FOM C and N into fractions in each layer (FOM pools)</summary>
        /// <param name="FOMPoolData">The in fom pool data.</param>
        [EventSubscribe("IncorpFOMPool")]
        private void OnIncorpFOMPool(FOMPoolType FOMPoolData)
        {
            if (FOMPoolData.Layer.Length > FOMLignin.C.Length)
                throw new Exception("Incorrect number of soil layers of IncorporatedFOM");

            for (int layer = 0; layer < FOMPoolData.Layer.Length; layer++)
            {
                FOMCarbohydrate.C[layer] += FOMPoolData.Layer[layer].Pool[0].C;
                FOMCarbohydrate.N[layer] += FOMPoolData.Layer[layer].Pool[0].N;

                FOMCellulose.C[layer] += FOMPoolData.Layer[layer].Pool[1].C;
                FOMCellulose.N[layer] += FOMPoolData.Layer[layer].Pool[1].N;

                FOMLignin.C[layer] += FOMPoolData.Layer[layer].Pool[2].C;
                FOMLignin.N[layer] += FOMPoolData.Layer[layer].Pool[2].N;
            }
        }


        /// <summary>
        /// Calculate actual decomposition
        /// </summary>
        public SurfaceOrganicMatterDecompType CalculateActualSOMDecomp()
        {
            return PotentialSOMDecomp;
        }

        /// <summary>
        /// Get the information on potential residue decomposition - perform daily calculations as part of this.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoSoilOrganicMatter")]
        private void OnDoSoilOrganicMatter(object sender, EventArgs e)
        {
            // Get potential residue decomposition from surfaceom.
            PotentialSOMDecomp = SurfaceOrganicMatter.PotentialDecomposition();

            SurfaceResidue.C[0] = 0;
            SurfaceResidue.N[0] = 0;
            for (int i = 0; i < PotentialSOMDecomp.Pool.Length; i++)
            {
                SurfaceResidue.C[0] += PotentialSOMDecomp.Pool[i].FOM.C;
                SurfaceResidue.N[0] += PotentialSOMDecomp.Pool[i].FOM.N;
            }

            
        }
    }
}
