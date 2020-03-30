namespace Models.Soils.Nutrients
{
    using Interfaces;
    using Models.Core;
    using System;
    using APSIM.Shared.Utilities;
    using Models.Surface;
    using Models.Soils;
    using System.Collections.Generic;
    using Models;
    using System.Drawing;

    /// <summary>
    /// # [Name]
    /// The soil nutrient model includes functionality for simulating pools of organmic matter and mineral nitrogen.  The processes for each are described below.
    /// ## Soil Nutrient Model Structure
    /// Soil organic matter is modelled as a series of discrete organic matter pools which are described in terms of their masses of carbon and nutrients.  These pools are initialised according to approaches specific to each pool.  Organic matter pools may have carbon flows, such as a decomposition process, associated to them.  These carbon flows are also specific to each pool, are independantly specified, and are described in each case in the documentation for each organic matter pool below.
    /// 
    /// Mineral nutrient pools (e.g. Nitrate, Ammonium, Urea) are described as solutes within the model.  Each pool captures the mass of the nutrient (e.g. N,P) and they may also contain nutrient flows to describe losses or transformations for that particular compound (e.g. denitrification of nitrate, hydrolysis of urea).
    /// [DocumentView]
    /// ## Pools
    /// A nutrient pool class is used to encapsulate the carbon and nitrogen within each soil organic matter pool.  Child functions within these classes provide information for initialisation and flows of C and N to other pools, or losses from the system.
    ///
    /// The soil organic matter pools used within the model are described in the following sections in terms of their initialisation and the carbon flows occuring from them.
    /// [DocumentType NutrientPool]
    /// ## Solutes
    /// The soil mineral nutrient pools used within the model are described in the following sections in terms of their initialisation and the flows occuring from them.
    /// [DocumentType Solute]
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    [ViewName("UserInterface.Views.DirectedGraphView")]
    [PresenterName("UserInterface.Presenters.DirectedGraphPresenter")]
    public class Nutrient : ModelCollectionFromResource, INutrient, IVisualiseAsDirectedGraph
    {
        private DirectedGraph directedGraphInfo;

        /// <summary>Get directed graph from model</summary>
        public DirectedGraph DirectedGraphInfo
        {
            get
            {
                if (Children != null && Children.Count > 0)
                    CalculateDirectedGraph();
                return directedGraphInfo;
            }
            set
            {
                directedGraphInfo = value;
            }
        }

        /// <summary>
        /// Summary file Link
        /// </summary>
        [Link]
        ISummary Summary = null;

        /// <summary>The surface organic matter</summary>
        [Link]
        private SurfaceOrganicMatter SurfaceOrganicMatter = null;

        /// <summary>The surface organic matter</summary>
        [Link]
        private Soil Soil = null;

        [Link(Type = LinkType.Child, ByName = true)]
        INutrientPool FOMCellulose = null;
        [Link(Type = LinkType.Child, ByName = true)]
        INutrientPool FOMCarbohydrate = null;
        [Link(Type = LinkType.Child, ByName = true)]
        INutrientPool FOMLignin = null;
        [Link(Type = LinkType.Child, ByName = true)]
        INutrientPool SurfaceResidue = null;
        [Link(ByName = true)]
        private ISolute NO3 = null;
        [Link(ByName = true)]
        private ISolute NH4 = null;
        [Link(ByName = true)]
        private ISolute Urea = null;

        // Carbon content of FOM
        private double CinFOM = 0.4;

        private SurfaceOrganicMatterDecompType PotentialSOMDecomp = null;

        /// <summary>
        /// Reset all pools and solutes
        /// </summary> 
        public void Reset()
        {
            List<IModel> Pools = Apsim.Children(this, typeof(NutrientPool));
            foreach (NutrientPool P in Pools)
                P.Reset();

            List<IModel> Solutes = Apsim.Children(this, typeof(ISolute));
            foreach (Solute S in Solutes)
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
                double[] values = new double[FOMLignin.C.Length];
                List<IModel> Pools = Apsim.Children(this, typeof(NutrientPool));

                foreach (NutrientPool P in Pools)
                    for (int i = 0; i < P.C.Length; i++)
                        values[i] += P.C[i];
                return values;
            }
        }


        /// <summary>
        /// Total C lost to the atmosphere
        /// </summary>
        [Units("kg/ha")]
        public double[] Catm
        {
            get
            {
                double[] values = new double[FOMLignin.C.Length];
                List<IModel> Flows = Apsim.Children(this, typeof(CarbonFlow));

                foreach (CarbonFlow f in Flows)
                    values = MathUtilities.Add(values, f.Catm);
                return values;
            }
        }

        /// <summary>
        /// Total N lost to the atmosphere
        /// </summary>
        [Units("kg/ha")]
        public double[] Natm
        {
            get
            {
                double[] values = new double[FOMLignin.C.Length];
                List<IModel> Flows = Apsim.Children(this, typeof(NFlow));

                foreach (NFlow f in Flows)
                    values = MathUtilities.Add(values, f.Natm);
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
                double[] values = new double[FOMLignin.C.Length];
                List<IModel> Flows = Apsim.Children(this, typeof(NFlow));

                foreach (NFlow f in Flows)
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
                double[] values = new double[FOMLignin.C.Length];
                List<IModel> Flows = Apsim.Children(this, typeof(CarbonFlow));

                foreach (CarbonFlow f in Flows)
                    values = MathUtilities.Add(values, f.MineralisedN);
                return values;
            }
        }
        /// <summary>
        /// Total Mineral N in each soil layer
        /// </summary>
        [Units("kg/ha")]
        public double[] MineralN
        {
            get
            {
                double[] values = new double[FOMLignin.C.Length];
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
        /// <summary>
        /// Total N in each soil layer
        /// </summary>
        [Units("kg/ha")]
        public double[] TotalN
        {
            get
            {
                double[] values = new double[FOMLignin.N.Length];
                List<IModel> Pools = Apsim.Children(this, typeof(NutrientPool));

                foreach (NutrientPool P in Pools)
                    for (int i = 0; i < P.N.Length; i++)
                        values[i] += P.N[i];
                return values;
            }
        }
        /// <summary>
        /// Carbon to Nitrogen Ratio for Fresh Organic Matter in each layer
        /// </summary>
        [Units("-")]
        public double[] FOMCNR
        {
            get
            {
                double[] nh4 = NH4.kgha;
                double[] no3 = NO3.kgha;

                double[] values = new double[FOMLignin.C.Length];
                for (int i = 0; i < FOMLignin.C.Length; i++)
                    values[i] = MathUtilities.Divide(FOMCarbohydrate.C[i] + FOMCellulose.C[i] + FOMLignin.C[i],
                               FOMCarbohydrate.N[i] + FOMCellulose.N[i] + FOMLignin.N[i] + nh4[i] + no3[i], 0.0);

                return values;
            }
        }


        /// <summary>
        /// Nitrogen in Fresh Organic Matter in each soil layer
        /// </summary>
        [Units("kg/ha")]
        public double[] FOMN
        {
            get
            {

                double[] values = new double[FOMLignin.C.Length];
                for (int i = 0; i < FOMLignin.C.Length; i++)
                    values[i] = FOMCarbohydrate.N[i] + FOMCellulose.N[i] + FOMLignin.N[i];

                return values;
            }
        }

        /// <summary>
        /// Carbon in Fresh Organic Matter in each soil layer
        /// </summary>
        [Units("kg/ha")]
        public double[] FOMC
        {
            get
            {

                double[] values = new double[FOMLignin.C.Length];
                for (int i = 0; i < FOMLignin.C.Length; i++)
                    values[i] = FOMCarbohydrate.C[i] + FOMCellulose.C[i] + FOMLignin.C[i];

                return values;
            }
        }
        /// <summary>Incorporate the given FOM C and N into each layer</summary>
        /// <param name="FOMdata">The in fo mdata.</param>
        [EventSubscribe("IncorpFOM")]
        private void IncorpFOM(FOMLayerType FOMdata)
        {
            DoIncorpFOM(FOMdata);
        }

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
                    if (layer < FOMCarbohydrate.C.Length)
                    {
                        FOMCarbohydrate.C[layer] += FOMdata.Layer[layer].FOM.amount * 0.2 * CinFOM;
                        FOMCellulose.C[layer] += FOMdata.Layer[layer].FOM.amount * 0.7 * CinFOM;
                        FOMLignin.C[layer] += FOMdata.Layer[layer].FOM.amount * 0.1 * CinFOM;

                        FOMCarbohydrate.N[layer] += FOMdata.Layer[layer].FOM.N * 0.2;
                        FOMCellulose.N[layer] += FOMdata.Layer[layer].FOM.N * 0.7;
                        FOMLignin.N[layer] += FOMdata.Layer[layer].FOM.N * 0.1;
                    }
                    else
                        Summary.WriteMessage(this, " Number of FOM values given is larger than the number of layers, extra values will be ignored");
                }
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
            SurfaceOrganicMatterDecompType ActualSOMDecomp = new SurfaceOrganicMatterDecompType();
            ActualSOMDecomp = ReflectionUtilities.Clone(PotentialSOMDecomp) as SurfaceOrganicMatterDecompType;

            double InitialResidueC = 0;  // Potential residue decomposition provided by surfaceorganicmatter model
            double FinalResidueC = 0;    // How much is left after decomposition
            double FractionDecomposed;

            for (int i = 0; i < PotentialSOMDecomp.Pool.Length; i++)
                InitialResidueC += PotentialSOMDecomp.Pool[i].FOM.C;
            FinalResidueC = SurfaceResidue.C[0];
            FractionDecomposed = 1.0 - MathUtilities.Divide(FinalResidueC,InitialResidueC,0);
            if (FractionDecomposed <1)
            { }
            for (int i = 0; i < PotentialSOMDecomp.Pool.Length; i++)
            {
                ActualSOMDecomp.Pool[i].FOM.C = PotentialSOMDecomp.Pool[i].FOM.C * FractionDecomposed;
                ActualSOMDecomp.Pool[i].FOM.N = PotentialSOMDecomp.Pool[i].FOM.N * FractionDecomposed;
            }
            return ActualSOMDecomp;
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
            SurfaceResidue.LayerFraction[0] = Math.Max(Math.Min(1.0, 100 / Soil.Thickness[0]),0.0);
            for (int i = 0; i < PotentialSOMDecomp.Pool.Length; i++)
            {
                SurfaceResidue.C[0] += PotentialSOMDecomp.Pool[i].FOM.C;
                SurfaceResidue.N[0] += PotentialSOMDecomp.Pool[i].FOM.N;
            }

            
        }

        /// <summary>Calculate / create a directed graph from model</summary>
        public void CalculateDirectedGraph()
        {
            if (directedGraphInfo == null)
                directedGraphInfo = new DirectedGraph();

            directedGraphInfo.Begin();

            bool needAtmosphereNode = false;

            foreach (NutrientPool pool in Apsim.Children(this, typeof(NutrientPool)))
            {
                directedGraphInfo.AddNode(pool.Name, ColourUtilities.ChooseColour(3), Color.Black);

                foreach (CarbonFlow cFlow in Apsim.Children(pool, typeof(CarbonFlow)))
                {
                    foreach (string destinationName in cFlow.destinationNames)
                    {
                        string destName = destinationName;
                        if (destName == null)
                        {
                            destName = "Atmosphere";
                            needAtmosphereNode = true;
                        }
                        directedGraphInfo.AddArc(null, pool.Name, destName, Color.Black);

                    }
                }
            }

            foreach (Solute solute in Apsim.Children(this, typeof(Solute)))
            {
                directedGraphInfo.AddNode(solute.Name, ColourUtilities.ChooseColour(2), Color.Black);
                foreach (NFlow nitrogenFlow in Apsim.Children(solute, typeof(NFlow)))
                {
                    string destName = nitrogenFlow.destinationName;
                    if (destName == null)
                    {
                        destName = "Atmosphere";
                        needAtmosphereNode = true;
                    }
                    directedGraphInfo.AddArc(null, nitrogenFlow.sourceName, destName, Color.Black);
                }
            }

            if (needAtmosphereNode)
                directedGraphInfo.AddTransparentNode("Atmosphere");

            
            directedGraphInfo.End();
        }

    }
}
