using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using APSIM.Shared.Documentation;
using APSIM.Shared.Documentation.Tags;
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
        private DirectedGraph directedGraphInfo;

        // Carbon content of FOM
        private double CinFOM = 0.4;

        [NonSerialized]
        private INutrientPool fom;

        /// <summary>Summary file Link</summary>
        [Link]
        private ISummary Summary = null;

        /// <summary>The surface organic matter</summary>
        [Link]
        private SurfaceOrganicMatter SurfaceOrganicMatter = null;

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
        public INutrientPool FOM { get { return fom; } }

        /// <summary>The fresh organic matter surface residue pool.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public INutrientPool SurfaceResidue { get; set; }

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
        [Link]
        private CarbonFlow[] CarbonFlows { get; set; }

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

        /// <summary>Potential soil organic matter decomposition for today.</summary>
        public SurfaceOrganicMatterDecompType SurfaceResidueDecomposition { get; private set; } = null;

        /// <summary>
        /// Reset all pools and solutes
        /// </summary> 
        public void Reset()
        {
            foreach (NutrientPool P in FindAllChildren<NutrientPool>())
                P.Reset();

            foreach (Solute S in FindAllInScope<ISolute>())
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
                    numLayers = FOMLignin.C.Length;
                double[] values = new double[numLayers];
                IEnumerable<NutrientPool> pools = FindAllChildren<NutrientPool>().Where(pool => pool != SurfaceResidue);

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
        public double[] Catm
        {
            get
            {
                int numLayers;
                if (FOMLignin.C == null)
                    numLayers = 0;
                else
                    numLayers = FOMLignin.C.Length;
                double[] values = new double[numLayers];

                foreach (CarbonFlow f in CarbonFlows)
                    for (int i = 0; i < numLayers; i++)
                        values[i] += f.Catm[i];
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
                int numLayers;
                if (FOMLignin.C == null)
                    numLayers = 0;
                else
                    numLayers = FOMLignin.C.Length;
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
                    numLayers = FOMLignin.C.Length;
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
                    numLayers = FOMLignin.C.Length;
                double[] values = new double[numLayers];

                // Get a list of N flows that make up mineralisation.
                // All flows except the surface residue N flow.
                List<CarbonFlow> Flows = FindAllDescendants<CarbonFlow>().ToList();
                Flows.RemoveAll(flow => flow.Parent == SurfaceResidue);

                // Add all flows.
                foreach (CarbonFlow f in Flows)
                {
                    for (int i = 0; i < values.Length; i++)
                        values[i] += f.MineralisedN[i];
                }
                return values;
            }
        }

        /// <summary>Net N Mineralisation from surface residue</summary>
        public double[] MineralisedNSurfaceResidue
        {
            get
            {
                var decomposition = (SurfaceResidue as IModel).FindChild<CarbonFlow>("Decomposition");
                return decomposition.MineralisedN;
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
                    numLayers = FOMLignin.C.Length;
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
                    numLayers = FOMLignin.C.Length;
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

        /// <summary>
        /// Total Mineral N in each soil layer
        /// </summary>
        [Units("kg/ha")]
        public double[] MineralN
        {
            get
            {
                int numLayers;
                if (FOMLignin.C == null)
                    numLayers = 0;
                else
                    numLayers = FOMLignin.C.Length;
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

        /// <summary>Soil organic nitrogen (FOM + Microbial + Humic)</summary>
        public INutrientPool Organic
        {
            get
            {
                // Get the denitrification N flow under NO3.
                var pools = FindAllDescendants<NutrientPool>().Cast<INutrientPool>().ToList();
                //pools.Remove(Inert); this should be included as without it is contrary to user expectations
                pools.Remove(SurfaceResidue);

                NutrientPool returnPool = new NutrientPool();
                returnPool.C = new double[FOMLignin.C.Length];
                returnPool.N = new double[FOMLignin.C.Length];
                foreach (var pool in pools)
                {
                    for (int i = 0; i < pool.C.Length; i++)
                    {
                        returnPool.C[i] += pool.C[i];
                        returnPool.N[i] += pool.N[i];
                    }
                }

                return returnPool;
            }
        }

        /// <summary>
        /// Total organic N in each soil layer, organic and mineral
        /// </summary>
        [Units("kg/ha")]
        public double[] TotalOrganicN
        {
            get
            {
                int numLayers;
                if (FOMLignin.N == null)
                    numLayers = 0;
                else
                    numLayers = FOMLignin.N.Length;
                double[] values = new double[numLayers];
                IEnumerable<NutrientPool> Pools = FindAllChildren<NutrientPool>();

                foreach (NutrientPool P in Pools)
                    for (int i = 0; i < numLayers; i++)
                        values[i] += P.N[i];
                return values;
            }
        }

        /// <summary>
        /// Total N in each soil layer, organic, mineral and nitrogen solutes
        /// </summary>
        [Units("kg/ha")]
        public double[] TotalN
        {
            get
            {
                double[] totalN = TotalOrganicN;

                // I don't like having hard coded solutes here but I can't think of another
                // way to do this easily.
                for (int i = 0; i < totalN.Length; i++)
                    totalN[i] += Urea.kgha[i] + NO3.kgha[i] + NH4.kgha[i];

                return totalN;
            }
        }

        /// <summary>
        /// Carbon to Nitrogen Ratio for Fresh Organic Matter used by low level functions
        /// </summary>
        public double[] FOMCNRFactor { get; private set; }

        /// <summary>Invoked at start of simulation.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            fom = new CompositeNutrientPool(new INutrientPool[] { FOMCarbohydrate, FOMCellulose, FOMLignin });
            FOMCNRFactor = new double[soilPhysical.Thickness.Length];
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
                        Summary.WriteMessage(this, " Number of FOM values given is larger than the number of layers, extra values will be ignored", MessageType.Diagnostic);
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
            SurfaceOrganicMatterDecompType actualSOMDecomp = new SurfaceOrganicMatterDecompType();
            actualSOMDecomp.Pool = new SurfaceOrganicMatterDecompPoolType[SurfaceResidueDecomposition.Pool.Length];
            for (int i = 0; i < SurfaceResidueDecomposition.Pool.Length; i++)
            {
                actualSOMDecomp.Pool[i] = new SurfaceOrganicMatterDecompPoolType();
                actualSOMDecomp.Pool[i].Name = SurfaceResidueDecomposition.Pool[i].Name;
                actualSOMDecomp.Pool[i].OrganicMatterType = SurfaceResidueDecomposition.Pool[i].OrganicMatterType;
                actualSOMDecomp.Pool[i].FOM = new FOMType();
                actualSOMDecomp.Pool[i].FOM.amount = SurfaceResidueDecomposition.Pool[i].FOM.amount;
            }

            double InitialResidueC = 0;  // Potential residue decomposition provided by surfaceorganicmatter model
            double FinalResidueC = 0;    // How much is left after decomposition
            double FractionDecomposed;

            for (int i = 0; i < SurfaceResidueDecomposition.Pool.Length; i++)
                InitialResidueC += SurfaceResidueDecomposition.Pool[i].FOM.C;
            FinalResidueC = SurfaceResidue.C[0];
            FractionDecomposed = 1.0 - MathUtilities.Divide(FinalResidueC, InitialResidueC, 0);
            if (FractionDecomposed < 1)
            { }
            for (int i = 0; i < SurfaceResidueDecomposition.Pool.Length; i++)
            {
                actualSOMDecomp.Pool[i].FOM.C = SurfaceResidueDecomposition.Pool[i].FOM.C * FractionDecomposed;
                actualSOMDecomp.Pool[i].FOM.N = SurfaceResidueDecomposition.Pool[i].FOM.N * FractionDecomposed;
            }
            return actualSOMDecomp;
        }

        /// <summary>
        /// Get the information on potential residue decomposition - perform daily calculations as part of this.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoSoilOrganicMatter")]
        private void OnDoSoilOrganicMatter(object sender, EventArgs e)
        {
            for (int layer = 0; layer < FOMCNRFactor.Length; layer++)
                FOMCNRFactor[layer] = MathUtilities.Divide(FOMCarbohydrate.C[layer] + FOMCellulose.C[layer] + FOMLignin.C[layer],
                                                     FOMCarbohydrate.N[layer] + FOMCellulose.N[layer] + FOMLignin.N[layer] + NH4.kgha[layer] + NO3.kgha[layer], 0.0);

            // Get potential residue decomposition from surfaceom.
            SurfaceResidueDecomposition = SurfaceOrganicMatter.PotentialDecomposition();

            var surfaceResiduePool = (NutrientPool)SurfaceResidue;

            surfaceResiduePool.C[0] = 0;
            surfaceResiduePool.N[0] = 0;
            surfaceResiduePool.LayerFraction[0] = Math.Max(Math.Min(1.0, 100 / soilPhysical.Thickness[0]), 0.0);

            for (int i = 0; i < SurfaceResidueDecomposition.Pool.Length; i++)
            {
                surfaceResiduePool.C[0] += SurfaceResidueDecomposition.Pool[i].FOM.C;
                surfaceResiduePool.N[0] += SurfaceResidueDecomposition.Pool[i].FOM.N;
            }
        }

        /// <summary>Calculate / create a directed graph from model</summary>
        public void CalculateDirectedGraph()
        {
            DirectedGraph oldGraph = directedGraphInfo;
            if (directedGraphInfo == null)
                directedGraphInfo = new DirectedGraph();

            directedGraphInfo.Begin();

            bool needAtmosphereNode = false;

            foreach (NutrientPool pool in this.FindAllChildren<NutrientPool>())
            {
                Point location = default(Point);
                Node oldNode;
                if (oldGraph != null && pool.Name != null && (oldNode = oldGraph.Nodes.Find(f => f.Name == pool.Name)) != null)
                    location = oldNode.Location;
                directedGraphInfo.AddNode(pool.Name, ColourUtilities.ChooseColour(3), Color.Black, location);

                foreach (CarbonFlow cFlow in pool.FindAllChildren<CarbonFlow>())
                {
                    foreach (string destinationName in cFlow.destinationNames)
                    {
                        string destName = destinationName;
                        if (destName == null)
                        {
                            destName = "Atmosphere";
                            needAtmosphereNode = true;
                        }

                        location = default(Point);
                        Arc oldArc;
                        if (oldGraph != null && pool.Name != null && (oldArc = oldGraph.Arcs.Find(f => f.SourceName == pool.Name && f.DestinationName == destName)) != null)
                            location = oldArc.Location;

                        directedGraphInfo.AddArc(null, pool.Name, destName, Color.Black, location);

                    }
                }
            }

            foreach (Solute solute in FindAllInScope<ISolute>())
            {
                Point location = new Point(0, 0);
                Node oldNode;
                if (oldGraph != null && solute.Name != null && (oldNode = oldGraph.Nodes.Find(f => f.Name == solute.Name)) != null)
                    location = oldNode.Location;
                directedGraphInfo.AddNode(solute.Name, ColourUtilities.ChooseColour(2), Color.Black, location);
                foreach (NFlow nitrogenFlow in FindAllChildren<NFlow>().Where(flow => flow.sourceName == solute.Name))
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

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            yield return new Section("The APSIM Nutrient Model", DocumentNutrient());
        }

        private IEnumerable<ITag> DocumentNutrient()
        {
            // Basic model description.
            yield return new Paragraph(CodeDocumentation.GetSummary(GetType()));

            foreach (ITag tag in DocumentChildren<Memo>())
                yield return tag;

            // Document model structure.
            List<ITag> structureTags = new List<ITag>();
            structureTags.Add(new DirectedGraphTag(DirectedGraphInfo));
            structureTags.Add(new Paragraph(CodeDocumentation.GetCustomTag(GetType(), "structure")));
            yield return new Section("Soil Nutrient Model Structure", structureTags);

            // Document nutrient pools.
            List<ITag> poolTags = new List<ITag>();
            poolTags.Add(new Paragraph(CodeDocumentation.GetCustomTag(GetType(), "pools")));
            poolTags.AddRange(DocumentChildren<NutrientPool>(true));
            yield return new Section("Pools", poolTags);

            // Document solutes.
            List<ITag> soluteTags = new List<ITag>();
            soluteTags.Add(new Paragraph(CodeDocumentation.GetCustomTag(GetType(), "solutes")));
            soluteTags.AddRange(DocumentChildren<Solute>(true));
            yield return new Section("Solutes", soluteTags);
        }
    }
}
