namespace Models.Soils.Nutrient
{
    using Interfaces;
    using Models.Core;
    using System;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// Soil carbon model
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    public class Nutrient : Model
    {
        [ChildLinkByName]
        NutrientPool FOMCellulose = null;
        [ChildLinkByName]
        NutrientPool FOMCarbohydrate = null;
        [ChildLinkByName]
        NutrientPool FOMLignin = null;
        /// <summary>
        /// 
        /// </summary>
        public double[] FOMCNR
        {
            get
            {
                double[] values = new double[FOMLignin.C.Length];
                for(int i=0; i<FOMLignin.C.Length;i++)
                    values[i] = MathUtilities.Divide(FOMCarbohydrate.C[i] + FOMCellulose.C[i] + FOMLignin.C[i],
                               FOMCarbohydrate.N[i] + FOMCellulose.N[i] + FOMLignin.N[i],0.0);

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

    }
}
