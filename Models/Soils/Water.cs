namespace Models.Soils
{
    using APSIM.Shared.Utilities;
    using Core;
    using Interfaces;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// This class encapsulates the water content (initial and current) in the simulation.
    /// </summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.WaterView.glade")]
    [PresenterName("UserInterface.Presenters.WaterPresenter")]
    [ValidParent(ParentType = typeof(Soil))]
    public class Water : Model, ITabularData
    {
        /// <summary>Depth strings. Wrapper around Thickness.</summary>
        [Units("mm")]
        [JsonIgnore]
        public string[] Depth
        {
            get
            {
                return SoilUtilities.ToDepthStrings(Thickness);
            }
            set
            {
                Thickness = SoilUtilities.ToThickness(value);
            }
        }

        /// <summary>Thickness</summary>
        [Summary]
        [Units("mm")]
        public double[] Thickness { get; set; }

        /// <summary>Nitrate NO3.</summary>
        [Description("Initial values")]
        [Summary]
        [Units("mm/mm")]
        public double[] InitialValues { get; set; }

        /// <summary>Nitrate NO3.</summary>
        [Summary]
        [Units("mm")]
        public double[] InitialValuesMM => MathUtilities.Multiply(InitialValues, Physical.Thickness);

        /// <summary>Amount water (mm)</summary>
        [Units("mm")]
        public double[] mm => MathUtilities.Multiply(volumetric, Thickness);

        /// <summary>Amount (mm/mm)</summary>
        [JsonIgnore]
        [Units("mm/mm")]
        public double[] volumetric { get; set; }

        /// <summary>Plant available water (mm).</summary>
        [Units("mm")]
        public double InitialPAWmm => MathUtilities.Subtract(InitialValuesMM, RelativeToLLMM).Sum();

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Reset();
        }

        /// <summary>
        /// Set solute to initialisation state
        /// </summary>
        public void Reset()
        {
            if (InitialValues == null)
                throw new Exception("No initial soil water specified.");
            volumetric = (double[]) InitialValues.Clone();
        }

        /// <summary>Tabular data. Called by GUI.</summary>
        public TabularData GetTabularData()
        {
            return new TabularData(Name, new TabularData.Column[]
            {
                new TabularData.Column("Depth", new VariableProperty(this, GetType().GetProperty("Depth"))),
                new TabularData.Column("Initial values", new VariableProperty(this, GetType().GetProperty("InitialValues")))
            });
        }

        /// <summary>The crop name (or LL15) that fraction full is relative to</summary>
        public string RelativeTo { get; set; }

        /// <summary>Allowed strings in 'RelativeTo' property.</summary>
        public IEnumerable<string> AllowedRelativeTo => new string[] { "LL15" }.Concat(FindAllInScope<SoilCrop>().Select(s => s.Name.Replace("Soil", "")));

        /// <summary>Distribute the water at the top of the profile when setting fraction full.</summary>
        public bool FilledFromTop { get; set; }

        /// <summary>Calculate the fraction of the profile that is full.</summary>
        [JsonIgnore]
        public double FractionFull
        {
            get
            {
                return MathUtilities.Subtract(InitialValuesMM, RelativeToLLMM).Sum() /
                       MathUtilities.Subtract(Physical.DULmm, RelativeToLLMM).Sum();
            }
            set
            {
                if (FilledFromTop)
                    InitialValues = DistributeWaterFromTop(value, Physical.Thickness,RelativeToLL, Physical.DUL, RelativeToXF);
                else
                    InitialValues = DistributeWaterEvenly(value, RelativeToLL, Physical.DUL);
            }
        }

        /// <summary>Calculate the depth of wet soil (mm).</summary>
        [JsonIgnore]
        public double DepthWetSoil
        {
            get
            {
                double depthSoFar = 0;
                for (int layer = 0; layer < Thickness.Length; layer++)
                {
                    if (InitialValues[layer] >= Physical.DUL[layer])
                        depthSoFar += Thickness[layer];
                    else
                        return depthSoFar;
                }

                return depthSoFar;
            }
            set
            {
                DistributeToDepthOfWetSoil(value, Thickness, RelativeToLL, Physical.DUL);
            }
        }

        /// <summary>Finds the 'physical' node.</summary>
        private IPhysical Physical
        {
            get
            {
                return FindInScope<IPhysical>();
            }
        }

        /// <summary>Find LL values (mm) for the RelativeTo property.</summary>
        private double[] RelativeToLL
        {
            get
            {
                if (RelativeTo == "LL15")
                    return Physical.LL15;
                else
                {
                    var plantCrop = FindInScope<SoilCrop>(RelativeTo);
                    if (plantCrop == null)
                    {
                        RelativeTo = "LL15";
                        return Physical.LL15;
                    }
                    else
                        return plantCrop.LL;
                }
            }
        }

        /// <summary>Find LL values (mm) for the RelativeTo property.</summary>
        private double[] RelativeToLLMM => MathUtilities.Multiply(RelativeToLL, Thickness);
        
        /// <summary>Find LL values (mm) for the RelativeTo property.</summary>
        private double[] RelativeToXF
        {
            get
            {
                if (RelativeTo != "LL15")
                {
                    var plantCrop = FindInScope<SoilCrop>(RelativeTo);
                    if (plantCrop != null)
                        return plantCrop.XF;
                }
                return Enumerable.Repeat(1.0, Thickness.Length).ToArray();
            }
        }

        /// <summary>Distribute water from the top of the profile using a fraction full.</summary>
        /// <param name="fractionFull">The fraction to fill the profile to.</param>
        /// <param name="thickness">Layer thickness (mm).</param>
        /// <param name="ll">Relative ll (ll15 or crop ll).</param>
        /// <param name="dul">Drained upper limit.</param>
        /// <param name="xf">XF.</param>
        /// <returns>A double array of volumetric soil water values (mm/mm)</returns>
        public static double[] DistributeWaterFromTop(double fractionFull, double[] thickness, double[] ll, double[] dul, double[] xf)
        {
            double[] sw = new double[thickness.Length];
            double[] pawcmm = MathUtilities.Multiply(MathUtilities.Subtract(dul, ll), thickness);

            double amountWater = MathUtilities.Sum(pawcmm) * fractionFull;
            for (int layer = 0; layer < thickness.Length; layer++)
            {
                if (amountWater >= 0 && xf[layer] == 0)
                    sw[layer] = ll[layer];
                else if (amountWater >= pawcmm[layer])
                {
                    sw[layer] = dul[layer];
                    amountWater = amountWater - pawcmm[layer];
                }
                else
                {
                    double prop = amountWater / pawcmm[layer];
                    sw[layer] = (prop * (dul[layer] - ll[layer])) + ll[layer];
                    amountWater = 0;
                }
            }

            return sw;
        }

        /// <summary>
        /// Calculate a layered soil water using a FractionFull and evenly distributed. Units: mm/mm
        /// </summary>
        /// <param name="fractionFull">The fraction to fill the profile to.</param>
        /// <param name="ll">Relative ll (ll15 or crop ll).</param>
        /// <param name="dul">Drained upper limit.</param>
        /// <returns>A double array of volumetric soil water values (mm/mm)</returns>
        public static double[] DistributeWaterEvenly(double fractionFull, double[] ll, double[] dul)
        {
            double[] sw = new double[ll.Length];
            for (int layer = 0; layer < ll.Length; layer++)
                sw[layer] = (fractionFull * (dul[layer] - ll[layer])) + ll[layer];

            return sw;
        }

        /// <summary>
        /// Calculate a layered soil water using a depth of wet soil.
        /// </summary>
        /// <param name="depthOfWetSoil">Depth of wet soil (mm)</param>
        /// <param name="thickness">Layer thickness (mm).</param>
        /// <param name="ll">Relative ll (ll15 or crop ll).</param>
        /// <param name="dul">Drained upper limit.</param>
        /// <returns>A double array of volumetric soil water values (mm/mm)</returns>
        public static double[] DistributeToDepthOfWetSoil(double depthOfWetSoil, double[] thickness, double[] ll, double[] dul)
        {
            double[] sw = new double[thickness.Length];
            double depthSoFar = 0;
            for (int layer = 0; layer < thickness.Length; layer++)
            {
                if (depthOfWetSoil > depthSoFar + thickness[layer])
                {
                    sw[layer] = dul[layer];
                }
                else
                {
                    double prop = Math.Max(depthOfWetSoil - depthSoFar, 0) / thickness[layer];
                    sw[layer] = (prop * (dul[layer] - ll[layer])) + ll[layer];
                }

                depthSoFar += thickness[layer];
            }
            return sw;
        }
    }
}
