using APSIM.Shared.Extensions.Collections;
using Models.Core;
using Models.Functions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.Soils.Nutrients
{
    /// <summary>A nutrient pool.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Nutrient))]
    public class NutrientPool : Model, INutrientPool
    {
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        private readonly IFunction initialCarbon = null;

        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        private readonly IFunction initialNitrogen = null;

        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        private readonly IFunction initialPhosphorus = null;

        [Link(Type=LinkType.Child)]
        private readonly CarbonFlow[] flows = null;

        private double[] c;
        private double[] n;
        private double[] p;

        /// <summary>Amount of carbon (kg/ha)</summary>
        public IReadOnlyList<double> C => c;

        /// <summary>Amount of nitrogen (kg/ha)</summary>
        public IReadOnlyList<double> N => n;

        /// <summary>Amount of phosphorus (kg/ha)</summary>
        public IReadOnlyList<double> P => p;

        /// <summary>Total C lost to the atmosphere (kg/ha)</summary>
        [JsonIgnore]
        public double[] Catm => flows.Select(f => f.Catm).Sum();

        /// <summary>Fraction of each layer occupied by this pool.</summary>
        public double[] LayerFraction { get; set; }

        /// <summary>
        /// Constructor for json serialisation.
        /// </summary>
        public NutrientPool() { }

        /// <summary>
        /// Constructor for injecting dependencies directly rather than via links.
        /// </summary>
        public NutrientPool(int numberLayers)
        {
            Initialise(numberLayers);
        }

        /// <summary>Constructor for creating a pool from a collection of other pools by adding their C,N,P components.</summary>
        /// <param name="pools">A collection of pools.</param>
        public NutrientPool(IEnumerable<INutrientPool> pools)
        {
            Initialise(numberLayers: pools.First().C.Count);
            foreach (var pool in pools)
            {
                for (int i = 0; i < pool.C.Count; i++)
                {
                    c[i] += pool.C[i];
                    n[i] += pool.N[i];
                    p[i] += pool.P[i];
                }
            }
        }

        /// <summary>Constructor for creating a pool from amounts of carbon, nitrogen and phosphorus.</summary>
        /// <param name="c">Amount of carbon (kg/ha).</param>
        /// <param name="n">Amount of nitrogen (kg/ha).</param>
        /// <param name="p">Amount of phosphorus (kg/ha).</param>
        public NutrientPool(double[] c, double[] n, double[] p)
        {
            this.c = c;
            this.n = n;
            this.p = p;
            LayerFraction = new double[c.Length];
            Array.Fill(LayerFraction, 1.0);
        }

        /// <summary>Called after instance has been created via deserialisation.</summary>
        public override void OnCreated()
        {
            IPhysical physical = FindInScope<IPhysical>();
            if (physical != null)
                Initialise(physical.Thickness.Length);
        }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="numberLayers">Number of layers.</param>
        public void Initialise(int numberLayers)
        {
            c = new double[numberLayers];
            n = new double[numberLayers];
            p = new double[numberLayers];
            LayerFraction = new double[numberLayers];
            if (initialCarbon != null)
            {
                for (int i = 0; i < C.Count; i++)
                    c[i] = initialCarbon.Value(i);
            }

            if (initialNitrogen != null)
            {
                for (int i = 0; i < N.Count; i++)
                    n[i] = initialNitrogen.Value(i);
            }

            if (initialPhosphorus != null)
            {
                for (int i = 0; i < P.Count; i++)
                    p[i] = initialPhosphorus.Value(i);
            }

            // Set fraction of the layer undertaking this flow to default value of 1
            Array.Fill(LayerFraction, 1.0);

            if (flows != null)
                foreach (var flow in flows)
                    flow.Initialise(numberLayers);
        }

        /// <summary>Perform all flows from the nutrient pool</summary>
        public void DoFlow()
        {
            if (flows != null)
                foreach (var flow in flows)
                    flow.DoFlow();
        }

        /// <summary>Clear the pool.</summary>
        public void Clear()
        {
            Array.Clear(c);
            Array.Clear(n);
            Array.Clear(p);
        }

        /// <summary>
        /// Add an amount of c, n, p (kg/ha) into a layer.
        /// </summary>
        /// <param name="index">Layer index</param>
        /// <param name="c">Amount of carbon (kg/ha)</param>
        /// <param name="n">Amount of nitrogen (kg/ha)</param>
        /// <param name="p">Amount of phosphorus (kg/ha)</param>
        public void Add(int index, double c, double n, double p)
        {
            this.c[index] += c;
            this.n[index] += n;
            this.p[index] += p;
        }

        /// <summary>Invoked at start of simulation.</summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            IPhysical physical = FindInScope<IPhysical>();
            if (physical != null)
                Initialise(physical.Thickness.Length);
        }
    }
}