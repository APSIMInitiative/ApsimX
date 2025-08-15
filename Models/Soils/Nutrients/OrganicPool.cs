using APSIM.Core;
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
    public class OrganicPool : Model, IOrganicPool
    {
        private double[] c;
        private double[] n;
        private double[] p;
        private double[] catm;
        private double[] layerFraction;


        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        private readonly IFunction initialCarbon = null;

        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        private readonly IFunction initialNitrogen = null;

        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        private readonly IFunction initialPhosphorus = null;

        [Link(Type=LinkType.Child)]
        private readonly OrganicFlow[] flows = null;


        /// <summary>
        /// Constructor for json serialisation.
        /// </summary>
        public OrganicPool() { }

        /// <summary>Constructor for creating a pool from amounts of carbon, nitrogen and phosphorus.</summary>
        /// <param name="c">Amount of carbon (kg/ha).</param>
        /// <param name="n">Amount of nitrogen (kg/ha).</param>
        /// <param name="p">Amount of phosphorus (kg/ha).</param>
        public OrganicPool(double[] c, double[] n, double[] p)
        {
            this.c = c;
            this.n = n;
            this.p = p;
        }


        /// <summary>Amount of carbon (kg/ha)</summary>
        public IReadOnlyList<double> C => c;

        /// <summary>Amount of nitrogen (kg/ha)</summary>
        public IReadOnlyList<double> N => n;

        /// <summary>Amount of phosphorus (kg/ha)</summary>
        public IReadOnlyList<double> P => p;

        /// <summary>Total C lost to the atmosphere (kg/ha)</summary>
        public IReadOnlyList<double> Catm => catm;

        /// <summary>Fraction of each layer occupied by this pool.</summary>
        public IReadOnlyList<double> LayerFraction => layerFraction;


        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="numberLayers">Number of layers.</param>
        public void Initialise(int numberLayers)
        {
            c = new double[numberLayers];
            catm = new double[numberLayers];
            n = new double[numberLayers];
            p = new double[numberLayers];
            layerFraction = new double[numberLayers];
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
            Array.Fill(layerFraction, 1.0);

            if (flows != null)
                foreach (var flow in flows)
                    flow.Initialise(numberLayers);
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

        /// <summary>
        /// Add an amount of c, n, p (kg/ha).
        /// </summary>
        /// <param name="c">Amount of carbon (kg/ha)</param>
        /// <param name="n">Amount of nitrogen (kg/ha)</param>
        /// <param name="p">Amount of phosphorus (kg/ha)</param>
        public void Add(double[] c, double[] n, double[] p)
        {
            for (int i = 0; i < c.Length; i++)
                Add(i, c[i], n[i], p[i]);
        }

        /// <summary>Set the layer fraction.</summary>
        /// <param name="values">The new values.</param>
        public void SetLayerFraction(IReadOnlyList<double> values)
        {
            if (values.Count != layerFraction.Length)
                throw new Exception("Incorrect number of values passed to NutrientPool.SetLayerFraction");
            values.CopyTo(layerFraction);
        }

        /// <summary>Perform all flows from the nutrient pool</summary>
        public void DoFlow()
        {
            if (flows != null)
                foreach (var flow in flows)
                    flow.DoFlow();

            Array.Clear(catm);
            foreach (var flow in flows)
                for (int i = 0; i < C.Count; i++)
                {
                    catm[i] += flow.Catm[i];
                }
        }
    }
}