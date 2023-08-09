using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Extensions.Collections;
using APSIM.Shared.Graphing;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Surface;
using Newtonsoft.Json;

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

        /// <summary>The Urea pool.</summary>
        [Link]
        private Solute[] solutes { get; set; }

        /// <summary>Child carbon flows.</summary>
        [Link(Type = LinkType.Child)]
        private readonly NutrientPool[] nutrientPools = null;

        /// <summary>Child nutrient flows.</summary>
        [Link(Type = LinkType.Child)]
        private readonly NFlow[] nutrientFlows = null;

        /// <summary>Surface residue decomposition pool.</summary>
        [Link(ByName = true)]
        private readonly NutrientPool surfaceResidue = null;

        /// <summary>Hydrolysis flow.</summary>
        [Link(ByName = true)]
        private readonly NFlow hydrolysis = null;

        /// <summary>Denitrification N flow.</summary>
        [Link(ByName = true)]
        private readonly NFlow denitrification = null;

        /// <summary>Nitrification N flow.</summary>
        [Link(ByName = true)]
        private readonly NFlow nitrification = null;

        // Carbon content of FOM
        private double CinFOM = 0.4;

        private double[] totalOrganicN;
        private double[] fomCNRFactor;
        private double[] catm;
        private double[] natm;
        private double[] n2oatm;
        private double[] totalC;
        private double[] denitrifiedN;
        private double[] nitrifiedN;

        [NonSerialized]
        private CompositeNutrientPool organic;

        /// <summary>Get directed graph from model</summary>
        public DirectedGraph DirectedGraphInfo { get; set; }

        /// <summary>
        /// Total C in each soil layer
        /// </summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> TotalC => totalC;

        /// <summary>
        /// Total C lost to the atmosphere
        /// </summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> Catm => catm;
       
        /// <summary>
        /// Total N lost to the atmosphere
        /// </summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> Natm => natm;

        /// <summary>
        /// Total N2O lost to the atmosphere
        /// </summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> N2Oatm => n2oatm;

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
        public IReadOnlyList<double> DenitrifiedN => denitrifiedN;

        /// <summary>Nitrified Nitrogen (from NH4 to either NO3 or N2O).</summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> NitrifiedN => nitrifiedN;
        
        /// <summary>Urea converted to NH4 via hydrolysis.</summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> HydrolysedN => hydrolysis.Value;

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


        /// <summary>Reset all pools, flows and solutes</summary> 
        public void Reset()
        {
            foreach (NutrientPool pool in nutrientPools)
                pool.Initialise(soilPhysical.Thickness.Length);

            foreach (NFlow flow in nutrientFlows)
                flow.Initialise(soilPhysical.Thickness.Length);

            foreach (Solute solute in solutes)
                solute.Reset();
        }

        /// <summary>
        /// Get the information on potential residue decomposition - perform daily calculations as part of this.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            fomCNRFactor = new double[soilPhysical.Thickness.Length];
            totalOrganicN = new double[soilPhysical.Thickness.Length];
            catm = new double[soilPhysical.Thickness.Length];
            natm = new double[soilPhysical.Thickness.Length];
            totalC = new double[soilPhysical.Thickness.Length];
            n2oatm = new double[soilPhysical.Thickness.Length];
            denitrifiedN = new double[soilPhysical.Thickness.Length];
            nitrifiedN = new double[soilPhysical.Thickness.Length];

            Reset();

            organic = new CompositeNutrientPool(nutrientPools);
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

            // Perform all flows.
            foreach (var pool in nutrientPools)
                pool.DoFlow();

            // Calculate variables.
            Array.Clear(totalOrganicN);
            Array.Clear(catm);
            Array.Clear(totalC);
            Array.Clear(natm);

            for (int i = 0; i < soilPhysical.Thickness.Length; i++)
                catm[i] = surfaceResidue.Catm[i];

            foreach (NutrientPool pool in nutrientPools)
            {
                for (int i = 0; i < soilPhysical.Thickness.Length; i++)
                {
                    totalOrganicN[i] += pool.N[i];
                    catm[i] += pool.Catm[i];
                    totalC[i] += pool.C[i];
                }
            }

            foreach (NFlow flow in nutrientFlows)
            {
                for (int i = 0; i < soilPhysical.Thickness.Length; i++)
                {
                    natm[i] += flow.Natm[i];
                    n2oatm[i] += flow.N2Oatm[i];
                }
            }

            Array.Clear(denitrifiedN);
            Array.Clear(nitrifiedN);
            for (int i = 0; i < soilPhysical.Thickness.Length; i++)
            {
                denitrifiedN[i] += denitrification.Value[i] + denitrification.Natm[i];
                nitrifiedN[i] += nitrification.Value[i] + nitrification.Natm[i];
            }

            organic.Calculate();
        }
    }
}
