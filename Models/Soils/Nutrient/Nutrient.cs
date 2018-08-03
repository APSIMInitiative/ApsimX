namespace Models.Soils.Nutrient
{
    using Interfaces;
    using Models.Core;
    using System;
    using APSIM.Shared.Utilities;
    using Models.Surface;
    using Models.Soils;
    using System.Collections.Generic;
    using Models.Graph;
    using System.Drawing;

    /// <summary>
    /// # [Name]
    /// The soil nutrient model includes functionality for simulating pools of organmic matter and mineral nitrogen.  The processes for each are described below.
    /// ## Structure of nutrient
    /// [DocumentView]
    /// ## Pools
    /// [DocumentType NutrientPool]
    /// ## Solutes:
    /// [DocumentType Solute]
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    [ViewName("UserInterface.Views.DirectedGraphView")]
    [PresenterName("UserInterface.Presenters.DirectedGraphPresenter")]
    public class Nutrient : Model, INutrient, IVisualiseAsDirectedGraph
    {
        private DirectedGraph _directedGraphInfo;

        /// <summary>Get directed graph from model</summary>
        public DirectedGraph DirectedGraphInfo
        {
            get
            {
                CalculateDirectedGraph();
                return _directedGraphInfo;
            }
            set
            {
                _directedGraphInfo = value;
            }
        }

        /// <summary>
        /// Summary file Link
        /// </summary>
        [Link]
        Summary Summary = null;

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

        // Carbon content of FOM
        private double CinFOM = 0.4;

        private SurfaceOrganicMatterDecompType PotentialSOMDecomp = null;

        /// <summary>
        /// Total C in each soil layer
        /// </summary>
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
        /// Total C in each soil layer
        /// </summary>
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


        /// <summary>
        /// 
        /// </summary>
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
        /// 
        /// </summary>
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
        /// <summary>Partition the given FOM C and N into fractions in each layer (one FOM)</summary>
        /// <param name="FOMdata">The in fo mdata.</param>
        [EventSubscribe("IncorpFOM")]
        private void OnIncorpFOM(FOMLayerType FOMdata)
        {
            // +  Purpose:
            //      Partition the given FOM C and N into fractions in each layer.
            //      It will be assumed that the CN ratios of all fractions are equal

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
            for (int i = 0; i < PotentialSOMDecomp.Pool.Length; i++)
            {
                SurfaceResidue.C[0] += PotentialSOMDecomp.Pool[i].FOM.C;
                SurfaceResidue.N[0] += PotentialSOMDecomp.Pool[i].FOM.N;
            }

            
        }

        /// <summary>Calculate / create a directed graph from model</summary>
        public void CalculateDirectedGraph()
        {
            if (_directedGraphInfo == null)
                _directedGraphInfo = new DirectedGraph();

            _directedGraphInfo.Begin();

            bool needAtmosphereNode = false;

            foreach (NutrientPool pool in Apsim.Children(this, typeof(NutrientPool)))
            {
                _directedGraphInfo.AddNode(pool.Name, Color.LightGreen, Color.Black);
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
                        _directedGraphInfo.AddArc(null, pool.Name, destName, Color.Black);

                    }
                }
            }

            foreach (Solute solute in Apsim.Children(this, typeof(Solute)))
            {
                _directedGraphInfo.AddNode(solute.Name, Color.LightCoral, Color.Black);
                foreach (NFlow nitrogenFlow in Apsim.Children(solute, typeof(NFlow)))
                {
                    string destName = nitrogenFlow.destinationName;
                    if (destName == null)
                    {
                        destName = "Atmosphere";
                        needAtmosphereNode = true;
                    }

                    _directedGraphInfo.AddArc(null, nitrogenFlow.sourceName, destName, Color.Black);
                }
            }

            if (needAtmosphereNode)
                _directedGraphInfo.AddNode("Atmosphere", Color.White, Color.White);

            _directedGraphInfo.End();
        }

    }
}
