using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using APSIM.Shared.Documentation.Tags;
using APSIM.Shared.Extensions.Collections;
using APSIM.Shared.Graphing;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Surface;

namespace Models.Soils.Nutrients
{

    /// <summary>
    /// The soil nutrient model includes functionality for simulating pools of organmic matter and mineral nitrogen.  The processes for each are described below.
    /// </summary>
    /// <structure>
    /// Soil organic matter is modelled as a series of discrete organic matter pools which are described in terms of their masses of carbon and nutrients. These pools are initialised according to approaches specific to each pool.  Organic matter pools may have carbon flows, such as a decomposition process, associated to them.  These carbon flows are also specific to each pool, are independantly specified, and are described in each case in the documentation for each organic matter pool below.
    /// 
    /// Mineral nutrient pools (e.g. Nitrate, Ammonium, Urea) are described as solutes within the model.  Each pool captures the mass of the nutrient (e.g. N,P) and they may also contain nutrient flows to describe losses or transformations for that particular compound (e.g. denitrification of nitrate, hydrolysis of urea).
    /// </structure>
    /// <pools>
    /// A nutrient pool class is used to encapsulate the carbon and nitrogen within each soil organic matter pool.  Child functions within these classes provide information for initialisation and flows of C and N to other pools, or losses from the system.
    ///
    /// The soil organic matter pools used within the model are described in the following sections in terms of their initialisation and the carbon flows occuring from them.
    /// </pools>
    /// <solutes>
    /// The soil mineral nutrient pools used within the model are described in the following sections in terms of their initialisation and the flows occuring from them.
    /// </solutes>
    [Serializable]
    [ScopedModel]
    [ValidParent(ParentType = typeof(Soil))]
    [ViewName("UserInterface.Views.DirectedGraphView")]
    [PresenterName("UserInterface.Presenters.DirectedGraphPresenter")]
    public class Nutrient : Model, INutrient, IVisualiseAsDirectedGraph
    {
        /// <summary>Summary file Link</summary>
        [Link]
        private ISummary summary = null;

        /// <summary>Access the soil physical properties.</summary>
        [Link]
        private IPhysical soilPhysical = null;

        /// <summary>The inert pool.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public INutrientPool Inert { get; set; }

        /// <summary>The microbial pool.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public INutrientPool Microbial { get; set; }

        /// <summary>The humic pool.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public INutrientPool Humic { get; set; }

        /// <summary>The fresh organic matter cellulose pool.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public INutrientPool FOMCellulose { get; set; }

        /// <summary>The fresh organic matter carbohydrate pool.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public INutrientPool FOMCarbohydrate { get; set; }

        /// <summary>The fresh organic matter lignin pool.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public INutrientPool FOMLignin { get; set; }

        /// <summary>The fresh organic matter pool.</summary>
        public INutrientPool FOM => new CompositeNutrientPool(new INutrientPool[] { FOMCarbohydrate, FOMCellulose, FOMLignin });

        /// <summary>The NO3 pool.</summary>
        [Link(ByName = true)]
        public ISolute NO3 { get; set; }

        /// <summary>The NH4 pool.</summary>
        [Link(ByName = true)]
        public ISolute NH4 { get; set; }

        /// <summary>The Urea pool.</summary>
        [Link(ByName = true)]
        public ISolute Urea { get; set; }

        /// <summary>Child carbon flows.</summary>
        [Link(Type = LinkType.Child)]
        private readonly NutrientPool[] nutrientPools = null;

        /// <summary>Surface residue decomposition pool.</summary>
        [Link(ByName = true)]
        private readonly NutrientPool surfaceResidue = null;

        // Carbon content of FOM
        private double CinFOM = 0.4;

        private double[] totalOrganicN;
        private double[] fomCNRFactor;
        private CompositeNutrientPool organic;

        /// <summary>Get directed graph from model</summary>
        public DirectedGraph DirectedGraphInfo { get; set; }

        /// <summary>
        /// Reset all pools and solutes
        /// </summary> 
        public void Reset()
        {
            foreach (NutrientPool P in FindAllChildren<NutrientPool>())
                P.Initialise(soilPhysical.Thickness.Length);

            foreach (Solute S in FindAllInScope<Solute>())
                S.Reset();
        }

        /// <summary>
        /// Total C in each soil layer
        /// </summary>
        [Units("kg/ha")]
        public double[] TotalC
        {
            get
            {
                int numLayers;
                if (FOMLignin.C == null)
                    numLayers = 0;
                else
                    numLayers = FOMLignin.C.Count;
                double[] values = new double[numLayers];
                IEnumerable<NutrientPool> pools = FindAllChildren<NutrientPool>();

                foreach (NutrientPool P in pools)
                    for (int i = 0; i < numLayers; i++)
                        values[i] += P.C[i];
                return values;
            }
        }

        /// <summary>
        /// Total C lost to the atmosphere
        /// </summary>
        [Units("kg/ha")]
        public double[] Catm => nutrientPools.Append(surfaceResidue).Select(p => p.Catm).Sum();
       
        /// <summary>
        /// Total N lost to the atmosphere
        /// </summary>
        [Units("kg/ha")]
        public double[] Natm
        {
            get
            {
                int numLayers;
                if (FOMLignin.C == null)
                    numLayers = 0;
                else
                    numLayers = FOMLignin.C.Count;
                double[] values = new double[numLayers];

                foreach (NFlow f in FindAllChildren<NFlow>())
                {
                    if (f.Natm != null)
                        values = MathUtilities.Add(values, f.Natm);
                }
                return values;
            }
        }

        /// <summary>
        /// Total N2O lost to the atmosphere
        /// </summary>
        [Units("kg/ha")]
        public double[] N2Oatm
        {
            get
            {
                int numLayers;
                if (FOMLignin.C == null)
                    numLayers = 0;
                else
                    numLayers = FOMLignin.C.Count;
                double[] values = new double[numLayers];

                foreach (NFlow f in FindAllChildren<NFlow>())
                    values = MathUtilities.Add(values, f.N2Oatm);
                return values;
            }
        }

        /// <summary>
        /// Total Net N Mineralisation in each soil layer
        /// </summary>
        [Units("kg/ha")]
        public double[] MineralisedN
        {
            get
            {
                int numLayers;
                if (FOMLignin.C == null)
                    numLayers = 0;
                else
                    numLayers = FOMLignin.C.Count;
                double[] values = new double[numLayers];

                // Get a list of N flows that make up mineralisation.
                // All flows except the surface residue N flow.
                List<CarbonFlow> Flows = FindAllDescendants<CarbonFlow>().ToList();
                
                // Add all flows.
                foreach (CarbonFlow f in Flows)
                {
                    for (int i = 0; i < values.Length; i++)
                        values[i] += f.MineralisedN[i];
                }
                return values;
            }
        }

        /// <summary>Denitrified Nitrogen (N flow from NO3).</summary>
        [Units("kg/ha")]
        public double[] DenitrifiedN
        {
            get
            {
                // Get the denitrification N flow under NO3.
                var no3NFlow = FindChild<NFlow>("Denitrification");

                int numLayers;
                if (FOMLignin.C == null)
                    numLayers = 0;
                else
                    numLayers = FOMLignin.C.Count;
                double[] values = new double[numLayers];
                if (no3NFlow.Value != null)
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] = no3NFlow.Value[i] + no3NFlow.Natm[i];
                    }
                }

                return values;
            }
        }

        /// <summary>Nitrified Nitrogen (from NH4 to either NO3 or N2O).</summary>
        [Units("kg/ha")]
        public double[] NitrifiedN
        {
            get
            {
                // Get the denitrification N flow under NO3.
                var nh4NFlow = FindChild<NFlow>("Nitrification");

                int numLayers;
                if (FOMLignin.C == null)
                    numLayers = 0;
                else
                    numLayers = FOMLignin.C.Count;
                double[] values = new double[numLayers];
                for (int i = 0; i < values.Length; i++)
                    values[i] = nh4NFlow.Value[i] + nh4NFlow.Natm[i];

                return values;
            }
        }

        /// <summary>Urea converted to NH4 via hydrolysis.</summary>
        [Units("kg/ha")]
        public double[] HydrolysedN
        {
            get
            {
                // Get the denitrification N flow under NO3.
                var hydrolysis = FindChild<NFlow>("Hydrolysis");

                return hydrolysis.Value;
            }
        }

        /// <summary>Total Mineral N in each soil layer</summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> MineralN
        {
            get
            {
                int numLayers;
                if (FOMLignin.C == null)
                    numLayers = 0;
                else
                    numLayers = FOMLignin.C.Count;
                double[] values = new double[numLayers];
                double[] nh4 = NH4.kgha;
                double[] no3 = NO3.kgha;
                values = MathUtilities.Add(values, nh4);
                values = MathUtilities.Add(values, no3);
                if (Urea != null)
                {
                    double[] urea = Urea.kgha;
                    values = MathUtilities.Add(values, urea);
                }
                return values;
            }
        }

        /// <summary>Soil organic nitrogen (FOM + Microbial + Humic + Inert)</summary>
        public INutrientPool Organic => organic;

        /// <summary>Total organic N in each soil layer, organic and mineral (kg/ha).</summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> TotalOrganicN => totalOrganicN;

        /// <summary>Total N in each soil layer, organic, mineral and nitrogen solutes (kg/ha).</summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> TotalN
        {
            get
            {
                double[] totalN = new double[totalOrganicN.Length];

                // I don't like having hard coded solutes here but I can't think of another
                // way to do this easily.
                for (int i = 0; i < totalN.Length; i++)
                    totalN[i] = totalOrganicN[i] + Urea.kgha[i] + NO3.kgha[i] + NH4.kgha[i];

                return totalN;
            }
        }
        /// <summary>Carbon to Nitrogen Ratio for Fresh Organic Matter used by low level functions.</summary>
        public IReadOnlyList<double> FOMCNRFactor => fomCNRFactor;

        /// <summary>Incorporate the given FOM C and N into each layer</summary>
        /// <param name="FOMdata">The in fo mdata.</param>
        public void DoIncorpFOM(FOMLayerType FOMdata)
        {
            bool nSpecified = false;
            for (int layer = 0; layer < FOMdata.Layer.Length; layer++)
            {
                // If the caller specified CNR values then use them to calculate N from Amount.
                if (FOMdata.Layer[layer].CNR > 0.0)
                    FOMdata.Layer[layer].FOM.N = (FOMdata.Layer[layer].FOM.amount * CinFOM) / FOMdata.Layer[layer].CNR;
                // Was any N specified?
                nSpecified |= FOMdata.Layer[layer].FOM.N != 0.0;
            }

            if (nSpecified)
            {

                // Now convert the IncorpFOM.DeltaWt and IncorpFOM.DeltaN arrays to include fraction information and add to pools.
                for (int layer = 0; layer < FOMdata.Layer.Length; layer++)
                {
                    if (layer < FOMCarbohydrate.C.Count)
                    {
                        FOMCarbohydrate.Add(layer, c: FOMdata.Layer[layer].FOM.amount * 0.2 * CinFOM,
                                                   n: FOMdata.Layer[layer].FOM.N * 0.2,
                                                   p:0);
                        FOMCellulose.Add(layer, c: FOMdata.Layer[layer].FOM.amount * 0.7 * CinFOM,
                                                n: FOMdata.Layer[layer].FOM.N * 0.7,
                                                p: 0);
                        FOMLignin.Add(layer, c: FOMdata.Layer[layer].FOM.amount * 0.1 * CinFOM,
                                             n: FOMdata.Layer[layer].FOM.N * 0.1,
                                             p: 0);
                    }
                    else
                        summary.WriteMessage(this, " Number of FOM values given is larger than the number of layers, extra values will be ignored", MessageType.Diagnostic);
                }
            }
        }

        /// <summary>
        /// Get the information on potential residue decomposition - perform daily calculations as part of this.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            // !!!! No links resolved at this point.

            fomCNRFactor = new double[soilPhysical.Thickness.Length];
            totalOrganicN = new double[soilPhysical.Thickness.Length];
            organic = new CompositeNutrientPool(nutrientPools);

            foreach (var pool in nutrientPools)
                pool.Initialise(soilPhysical.Thickness.Length);
        }

        /// <summary>
        /// Get the information on potential residue decomposition - perform daily calculations as part of this.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoSoilOrganicMatter")]
        private void OnDoSoilOrganicMatter(object sender, EventArgs e)
        {
            for (int layer = 0; layer < fomCNRFactor.Length; layer++)
                fomCNRFactor[layer] = MathUtilities.Divide(FOMCarbohydrate.C[layer] + FOMCellulose.C[layer] + FOMLignin.C[layer],
                                                     FOMCarbohydrate.N[layer] + FOMCellulose.N[layer] + FOMLignin.N[layer] + NH4.kgha[layer] + NO3.kgha[layer], 0.0);

            foreach (var pool in nutrientPools)
                pool.DoFlow();

            // Calculate variables.
            Array.Clear(totalOrganicN);
            foreach (NutrientPool pool in nutrientPools)
                for (int i = 0; i < soilPhysical.Thickness.Length; i++)
                    totalOrganicN[i] += pool.N[i];

            organic.Calculate();
        }
    }
}
