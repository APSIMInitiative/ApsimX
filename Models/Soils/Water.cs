namespace Models.Soils
{
    using APSIM.Shared.APSoil;
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
        [Summary]
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
        public double[] Thickness { get; set; }

        /// <summary>Nitrate NO3.</summary>
        [Description("Initial values")]
        [Summary]
        [Units("mm/mm")]
        [Display(Format = "N3")]
        public double[] InitialValues { get; set; }

        /// <summary>Nitrate NO3.</summary>
        [Summary]
        [Units("mm")]
        [Display(Format = "N1")]
        public double[] InitialValuesMM => InitialValues == null ? null : MathUtilities.Multiply(InitialValues, Physical.Thickness);

        /// <summary>Amount water (mm)</summary>
        [Units("mm")]
        public double[] MM => Volumetric == null ? null : MathUtilities.Multiply(Volumetric, Thickness);

        /// <summary>Amount (mm/mm)</summary>
        [JsonIgnore]
        [Units("mm/mm")]
        public double[] Volumetric { get; set; }

        /// <summary>Plant available water (mm).</summary>
        [Units("mm")]
        public double InitialPAWmm => InitialValues == null ? 0 : MathUtilities.Subtract(InitialValuesMM, RelativeToLLMM).Sum();

        /// <summary>Plant available water SW-LL15 (mm/mm).</summary>
        [Units("mm/mm")]
        public double[] PAW => APSoilUtilities.CalcPAWC(Physical.Thickness, Physical.LL15, Volumetric, null);

        /// <summary>Plant available water SW-LL15 (mm).</summary>
        [Units("mm")]
        public double[] PAWmm => MathUtilities.Multiply(PAW, Physical.Thickness);

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Reset();
        }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("EndOfSimulation")]
        private void OnSimulationEnding(object sender, EventArgs e)
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
            Volumetric = (double[]) InitialValues.Clone();
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
                return InitialValues == null ? 0 : MathUtilities.Subtract(InitialValuesMM, RelativeToLLMM).Sum() /
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
                if (InitialValues == null)
                    return 0;
                var ll = RelativeToLL;
                double depthSoFar = 0;
                for (int layer = 0; layer < Thickness.Length; layer++)
                {
                    var prop = (InitialValues[layer] - ll[layer]) / (Physical.DUL[layer] - ll[layer]);

                    if (MathUtilities.IsGreaterThanOrEqual(prop, 1.0))
                        depthSoFar += Thickness[layer];
                    else
                    {
                        depthSoFar += Thickness[layer] * prop;
                        return depthSoFar;
                    }
                }

                return depthSoFar;
            }
            set
            {
                InitialValues = DistributeToDepthOfWetSoil(value, Thickness, RelativeToLL, Physical.DUL);
            }
        }

        /// <summary>Finds the 'physical' node.</summary>
        public IPhysical Physical => FindInScope<IPhysical>();

        /// <summary>Find LL values (mm) for the RelativeTo property.</summary>
        public double[] RelativeToLL
        {
            get
            {
                if (RelativeTo == "LL15")
                    return Physical.LL15;
                else
                {
                    var plantCrop = FindInScope<SoilCrop>(RelativeTo + "Soil");
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

        /// <summary>Gets the model ready for running in a simulation.</summary>
        /// <param name="targetThickness">Target thickness.</param>
        public void Standardise(double[] targetThickness)
        {
            SetThickness(targetThickness);
            Reset();
        }

        /// <summary>Sets the sample thickness.</summary>
        /// <param name="thickness">The thickness to change the sample to.</param>
        private void SetThickness(double[] thickness)
        {
            if (!MathUtilities.AreEqual(thickness, Thickness))
            {
                if (InitialValues != null)
                    InitialValues = MapSW(InitialValues, Thickness, thickness);

                Thickness = thickness;
            }
        }

        /// <summary>Map soil water from one layer structure to another.</summary>
        /// <param name="fromValues">The from values.</param>
        /// <param name="fromThickness">The from thickness.</param>
        /// <param name="toThickness">To thickness.</param>
        /// <returns></returns>
        private double[] MapSW(double[] fromValues, double[] fromThickness, double[] toThickness)
        {
            if (fromValues == null || fromThickness == null)
                return null;

            // convert from values to a mass basis with a dummy bottom layer.
            List<double> values = new List<double>();
            values.AddRange(fromValues);
            values.Add(MathUtilities.LastValue(fromValues) * 0.8);
            values.Add(MathUtilities.LastValue(fromValues) * 0.4);
            values.Add(0.0);
            List<double> thickness = new List<double>();
            thickness.AddRange(fromThickness);
            thickness.Add(MathUtilities.LastValue(fromThickness));
            thickness.Add(MathUtilities.LastValue(fromThickness));
            thickness.Add(3000);

            // Get the first crop ll or ll15.
            var firstCrop = (Physical as IModel).FindChild<SoilCrop>();
            double[] LowerBound;
            if (Physical != null && firstCrop != null)
                LowerBound = SoilUtilities.MapConcentration(firstCrop.LL, Physical.Thickness, thickness.ToArray(), MathUtilities.LastValue(firstCrop.LL)); 
            else
                LowerBound = SoilUtilities.MapConcentration(Physical.LL15, Physical.Thickness, thickness.ToArray(), Physical.LL15.Last()); 
            if (LowerBound == null)
                throw new Exception("Cannot find crop lower limit or LL15 in soil");

            // Make sure all SW values below LastIndex don't go below CLL.
            int bottomLayer = fromThickness.Length - 1;
            for (int i = bottomLayer + 1; i < thickness.Count; i++)
                values[i] = Math.Max(values[i], LowerBound[i]);

            double[] massValues = MathUtilities.Multiply(values.ToArray(), thickness.ToArray());

            // Convert mass back to concentration and return
            return MathUtilities.Divide(SoilUtilities.MapMass(massValues, thickness.ToArray(), toThickness), toThickness);
        }
    }
}
