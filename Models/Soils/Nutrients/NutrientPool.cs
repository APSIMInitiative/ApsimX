using Models.Core;
using Models.Functions;
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
        private IFunction initialCarbon;
        private IFunction initialNitrogen;
        private IFunction initialPhosphorus;

        /// <summary>Amount of carbon (kg/ha)</summary>
        public double[] C { get; private set; }

        /// <summary>Amount of nitrogen (kg/ha)</summary>
        public double[] N { get; private set; }

        /// <summary>Amount of phosphorus (kg/ha)</summary>
        public double[] P { get; private set; }

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
            Initialise(numLayers: pools.First().C.Length);
            foreach (var pool in pools)
            {
                for (int i = 0; i < pool.C.Length; i++)
                {
                    C[i] += pool.C[i];
                    N[i] += pool.N[i];
                    P[i] += pool.P[i];
                }
            }
        }

        /// <summary>Constructor for creating a pool from amounts of carbon, nitrogen and phosphorus.</summary>
        /// <param name="c">Amount of carbon (kg/ha).</param>
        /// <param name="n">Amount of nitrogen (kg/ha).</param>
        /// <param name="p">Amount of phosphorus (kg/ha).</param>
        public NutrientPool(double[] c, double[] n, double[] p)
        {
            C = c;
            N = n;
            P = p;
            LayerFraction = new double[c.Length];
            Array.Fill(LayerFraction, 1.0);
        }

        /// <summary>Called after instance has been created via deserialisation.</summary>
        public override void OnCreated()
        {
            IPhysical physical = FindInScope<IPhysical>();
            if (physical != null)
            {
                initialCarbon = FindChild<IFunction>("InitialCarbon");
                initialNitrogen = FindChild<IFunction>("InitialNitrogen");
                initialPhosphorus = FindChild<IFunction>("InitialPhosphorus");

                Initialise(physical.Thickness.Length);
            }
        }

        /// <summary>Performs the initial checks and setup</summary>
        public void Initialise(int numLayers)
        {
            IPhysical physical = FindInScope<IPhysical>();

            C = new double[numLayers];
            N = new double[numLayers];
            P = new double[numLayers];
            LayerFraction = new double[numLayers];
            if (initialCarbon != null)
            {
                for (int i = 0; i < C.Length; i++)
                    C[i] = initialCarbon.Value(i);
            }

            if (initialNitrogen != null)
            {
                for (int i = 0; i < N.Length; i++)
                    N[i] = initialNitrogen.Value(i);
            }

            if (initialPhosphorus != null)
            {
                for (int i = 0; i < P.Length; i++)
                    P[i] = initialPhosphorus.Value(i);
            }

            // Set fraction of the layer undertaking this flow to default value of 1
            Array.Fill(LayerFraction, 1.0);
        }

        /// <summary>Add C, N, P into nutrient pool</summary>
        /// <param name="c">Amount of carbon to add (kg/ha)</param>
        /// <param name="n">Amount of nitrogen to add (kg/ha)</param>
        /// <param name="p">Amount of phosphorus to add (kg/ha)</param>
        public void Add(double[] c, double[] n, double[] p)
        {
            if (c.Length != n.Length || c.Length != p.Length || c.Length > C.Length)
                throw new Exception("Arrays for C, N andP must be the same length.");

            for (int i = 0; i < c.Length; i++)
            {
                C[i] += c[i];
                N[i] += n[i];
                P[i] += p[i];
            }
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