using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;

namespace Models.Soils.Nutrients
{

    /// <summary>
    /// A nutrient pool.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Nutrient))]
    public class NutrientPool : Model, INutrientPool
    {
        /// <summary>Access the soil physical properties.</summary>
        [Link]
        private IPhysical soilPhysical = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction InitialCarbon = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction InitialNitrogen = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction InitialPhosphorus = null;

        /// <summary>Amount of carbon (kg/ha)</summary>
        public double[] C { get; set; }

        /// <summary>Amount of nitrogen (kg/ha)</summary>
        public double[] N { get; set; }

        /// <summary>Amount of phosphorus (kg/ha)</summary>
        public double[] P { get; set; }

        /// <summary>
        /// Fraction of each layer occupied by this pool.
        /// /// </summary>
        public double[] LayerFraction { get; set; }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Reset();
        }

        /// <summary>
        /// Set nutrient pool to initialisation state
        /// </summary>
        public void Reset()
        {
            C = new double[soilPhysical.Thickness.Length];
            for (int i = 0; i < C.Length; i++)
                C[i] = InitialCarbon.Value(i);

            N = new double[soilPhysical.Thickness.Length];
            for (int i = 0; i < N.Length; i++)
                N[i] = InitialNitrogen.Value(i);

            P = new double[soilPhysical.Thickness.Length];
            for (int i = 0; i < P.Length; i++)
                P[i] = InitialPhosphorus.Value(i);

            // Set fraction of the layer undertaking this flow to 1 - default unless changed by parent model
            LayerFraction = new double[soilPhysical.Thickness.Length];
            for (int i = 0; i < LayerFraction.Length; i++)
                LayerFraction[i] = 1.0;
        }

        /// <summary>
        /// Add C and N into nutrient pool
        /// </summary>
        /// <param name="CAdded"></param>
        /// <param name="NAdded"></param>
        /// <param name="PAdded"></param>
        public void Add(double[] CAdded, double[] NAdded, double[] PAdded)
        {
            if (CAdded.Length != NAdded.Length)
                throw new Exception("Arrays for addition of soil organic matter and N must be of same length.");
            if (CAdded.Length != PAdded.Length)
                throw new Exception("Arrays for addition of soil organic matter and P must be of same length.");
            if (CAdded.Length > C.Length)
                throw new Exception("Array for addition of soil organic matter must be less than or equal to the number of soil layers.");

            for (int i = 0; i < CAdded.Length; i++)
            {
                C[i] += CAdded[i];
                N[i] += NAdded[i];
                P[i] += PAdded[i];
            }
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<ITag> Document()
        {
            foreach (ITag tag in DocumentChildren<Memo>())
                yield return tag;

            List<ITag> initialisationTags = new List<ITag>();
            initialisationTags.Add(new Paragraph("The initialisation of Carbon and Nutrient contents of this pool is described as follows:"));
            initialisationTags.AddRange(InitialCarbon.Document());
            initialisationTags.AddRange(InitialNitrogen.Document());
            // todo: include initial P in docs once soil P is released.
            // initialisationTags.AddRange(InitialPhosphorus.Document());
            yield return new Section("Initialisation", initialisationTags);

            yield return new Section("Organic Matter Flows", DocumentChildren<CarbonFlow>(true));
        }
    }
}
