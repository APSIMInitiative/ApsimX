using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Numerics;
using APSIM.Soils;
using APSIM.Shared.APSoil;
using Models.Core;
using Models.Interfaces;
using Newtonsoft.Json;

namespace Models.Soils
{

    /// <summary>
    /// This class encapsulates the water content (initial and current) in the simulation.
    /// </summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.WaterView.glade")]
    [PresenterName("UserInterface.Presenters.WaterPresenter")]
    [ValidParent(ParentType = typeof(Soil))]
    public class Water : Model
    {
        private double[] volumetric;

        /// <summary>Last initialisation event.</summary>
        public event EventHandler WaterChanged;


        /// <summary>Depth strings. Wrapper around Thickness.</summary>
        [Display]
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

        /// <summary>Initial water values</summary>
        [Description("Initial values")]
        [Summary]
        [Units("mm/mm")]
        [Display(Format = "N3")]
        public double[] InitialValues { get; set; }

        /// <summary>Initial values total mm</summary>
        [Summary]
        [Units("mm")]
        public double[] InitialValuesMM => InitialValues == null ? null : MathUtilities.Multiply(InitialValues, Thickness);

        /// <summary>Amount water (mm)</summary>
        [Units("mm")]
        public double[] MM => Volumetric == null ? null : MathUtilities.Multiply(Volumetric, Thickness);

        /// <summary>Amount (mm/mm)</summary>
        [JsonIgnore]
        [Units("mm/mm")]
        public double[] Volumetric
        {
            get
            {
                return volumetric;
            }
            set
            {
                volumetric = value;
                WaterChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Soil water potential (kPa)</summary>
        [Units("kPa")]
        [JsonIgnore]
        public double[] Potential
        {
            get
            {
                return MathUtilities.Multiply_Value(WaterModel.PSI, 0.1);
            }
        }

        /// <summary>Soil water potential (kPa)</summary>
        [Units("-")]
        [JsonIgnore]
        public double[] pF
        {
            get
            {
                double[] psi = WaterModel.PSI;
                double[] value = new double[WaterModel.PSI.Length];
                for (int layer = 0; layer < Thickness.Length; layer++)
                {
                    if (psi[layer] < 0.0)
                        value[layer] = Math.Log10(-psi[layer]);
                    else
                        value[layer] = 0;
                }
                return value;
            }
        }

        /// <summary>Soil hydraulic conductivity (mm/d)</summary>
        [Units("mm/d")]
        [JsonIgnore]
        public double[] HydraulicConductivity
        {
            get
            {
                return MathUtilities.Multiply_Value(WaterModel.K, 10 * 24);
            }
        }

        /// <summary>Plant available water (mm).</summary>
        [Units("mm")]
        public double InitialPAWmm
        {
            get
            {
                if (InitialValues == null)
                    return 0;
                double[] values =  MathUtilities.Subtract(InitialValuesMM, RelativeToLLMM);
                if (values != null)
                    return values.Sum();
                return 0;
            }
            set
            {
                if (Physical != null)
                {
                    double[] airdry = SoilUtilities.MapConcentration(Physical.AirDry, Physical.Thickness, Thickness, Physical.AirDry.Last());
                    double[] dul = SoilUtilities.MapConcentration(Physical.DUL, Physical.Thickness, Thickness, Physical.DUL.Last());
                    double[] sat = SoilUtilities.MapConcentration(Physical.SAT, Physical.Thickness, Thickness, Physical.SAT.Last());
                    double[] thickness = Physical.Thickness;

                    if (FilledFromTop)
                        InitialValues = SoilUtilities.DistributeAmountWaterFromTop(value, thickness, airdry, RelativeToLL, dul, sat, RelativeToXF);
                    else
                        InitialValues = SoilUtilities.DistributeAmountWaterEvenly(value, thickness, airdry, RelativeToLL, dul, sat, RelativeToXF);
                }
            }
        }

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
            Volumetric = (double[])InitialValues.Clone();
        }

        [JsonIgnore]
        private string relativeToCheck = "LL15";

        /// <summary>The crop name (or LL15) that fraction full is relative to</summary>
        public string RelativeTo
        {
            get => relativeToCheck;
            set
            {
                string newValue = value;
                if (newValue == null)
                    newValue = "LL15";
                // This structure is required to create a 'source of truth' to ensure
                // a stack overflow does not occurs.
                if (relativeToCheck != newValue)
                {
                    double percent = FractionFull;
                    relativeToCheck = newValue;
                    UpdateInitialValuesFromFractionFull(percent);
                }
                else
                {
                    relativeToCheck = newValue;
                }
            }
        }

        /// <summary>Allowed strings in 'RelativeTo' property.</summary>
        public IEnumerable<string> AllowedRelativeTo => (GetAllowedRelativeToStrings());

        [JsonIgnore]
        private bool filledFromTop = false;

        /// <summary>Distribute the water at the top of the profile when setting fraction full.</summary>
        public bool FilledFromTop
        {
            get => filledFromTop;
            set
            {
                double percent = FractionFull;
                filledFromTop = value;
                if(Physical != null)
                    UpdateInitialValuesFromFractionFull(percent);
            }
        }

        /// <summary>Calculate the fraction of the profile that is full.</summary>
        [JsonIgnore]
        public double FractionFull
        {
            get
            {
                if (Physical != null)
                {
                    double newFractionFull;
                    double[] dul = SoilUtilities.MapConcentration(Physical.DUL, Physical.Thickness, Thickness, Physical.DUL.Last());
                    double[] dulMM = MathUtilities.Multiply(dul, Thickness);
                    if (InitialValues == null)
                        return 0;
                    else
                    {
                        if (RelativeTo != "LL15")
                        {
                            //Get layer indices that have a XF as 0.
                            var plantCrop = GetCropSoil();

                            double[] initialValuesMMMinusEmptyXFLayers = MathUtilities.Multiply(plantCrop.XF, InitialValuesMM);
                            double[] relativeToLLMMMinusEmptyXFLayers = MathUtilities.Multiply(plantCrop.XF, RelativeToLLMM);
                            double[] dulMMMinusEmptyXFLayers = MathUtilities.Multiply(plantCrop.XF, dulMM);

                            newFractionFull = MathUtilities.Subtract(initialValuesMMMinusEmptyXFLayers, relativeToLLMMMinusEmptyXFLayers).Sum() /
                                                MathUtilities.Subtract(dulMMMinusEmptyXFLayers, relativeToLLMMMinusEmptyXFLayers).Sum();
                        }
                        else
                        {

                            var paw = MathUtilities.Subtract(InitialValuesMM, RelativeToLLMM);
                            if (paw == null)
                                newFractionFull = 0;
                            else
                                newFractionFull = paw.Sum() / MathUtilities.Subtract(dulMM, RelativeToLLMM).Sum();
                        }

                        return newFractionFull;
                    }
                }
                else return 0;
            }
            set
            {
                UpdateInitialValuesFromFractionFull(value);
            }
        }

        /// <summary>
        /// Updates InitialValues from FractionFull.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void UpdateInitialValuesFromFractionFull(double value)
        {
            if (Physical != null)
            {
                double[] airdry = SoilUtilities.MapConcentration(Physical.AirDry, Physical.Thickness, Thickness, Physical.AirDry.Last());
                double[] dul = SoilUtilities.MapConcentration(Physical.DUL, Physical.Thickness, Thickness, Physical.DUL.Last());
                double[] sat = SoilUtilities.MapConcentration(Physical.DUL, Physical.Thickness, Thickness, Physical.SAT.Last());
                if (FilledFromTop)
                    InitialValues = APSIM.Soils.SoilUtilities.DistributeWaterFromTop(value, Thickness, airdry, RelativeToLL, dul, sat, RelativeToXF);
                else
                    InitialValues = APSIM.Soils.SoilUtilities.DistributeWaterEvenly(value, Thickness, airdry, RelativeToLL, dul, sat, RelativeToXF);

                double fraction = FractionFull;
            }
        }

        /// <summary>Calculate the depth of wet soil (mm).</summary>
        [JsonIgnore]
        public double DepthWetSoil
        {
            get
            {
                if (InitialValues == null || InitialValues.Length != Thickness.Length)
                    return 0;
                var ll = RelativeToLL;
                double[] dul = SoilUtilities.MapConcentration(Physical.DUL, Physical.Thickness, Thickness, Physical.DUL.Last());

                double depthSoFar = 0;
                for (int layer = 0; layer < Thickness.Length; layer++)
                {
                    double prop = 0;
                    if (dul[layer] - ll[layer] != 0)
                        prop = (InitialValues[layer] - ll[layer]) / (dul[layer] - ll[layer]);

                    if (MathUtilities.IsGreaterThanOrEqual(prop, 1.0))
                        depthSoFar += Thickness[layer];
                    else
                        depthSoFar += Thickness[layer] * prop;
                }
                return depthSoFar;
            }
            set
            {
                double[] dul = SoilUtilities.MapConcentration(Physical.DUL, Physical.Thickness, Thickness, Physical.DUL.Last());
                InitialValues = APSIM.Soils.SoilUtilities.DistributeToDepthOfWetSoil(value, Thickness, RelativeToLL, dul);
            }
        }

        /// <summary>Finds the 'Physical' node.</summary>
        public IPhysical Physical => FindAncestor<Soil>()?.FindDescendant<IPhysical>() ?? FindInScope<IPhysical>();

        /// <summary>Finds the 'SoilWater' node.</summary>
        public ISoilWater WaterModel => FindAncestor<Soil>()?.FindDescendant<ISoilWater>() ?? FindInScope<ISoilWater>();

        /// <summary>Find LL values (mm) for the RelativeTo property.</summary>
        public double[] RelativeToLL
        {
            get
            {
                double[] values;
                if (RelativeTo == "LL15")
                    values = Physical.LL15;
                else
                {
                    var plantCrop = GetCropSoil();
                    if (plantCrop == null)
                    {
                        RelativeTo = "LL15";
                        values = Physical.LL15;
                    }
                    else
                        values = plantCrop.LL;
                }

                return SoilUtilities.MapConcentration(values, Physical.Thickness, Thickness, values.Last());
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
                    SoilCrop plantCrop = GetCropSoil();
                    if (plantCrop != null)
                        return SoilUtilities.MapConcentration(plantCrop.XF, Physical.Thickness, Thickness, plantCrop.XF.Last());
                }
                return Enumerable.Repeat(1.0, Thickness.Length).ToArray();
            }
        }



        /// <summary>
        /// Get all soil crop names as strings from the relevant Soil this water node is a child of as well as LL15 (default value).
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetAllowedRelativeToStrings()
        {
            IEnumerable<string> results = new List<string>();
            IEnumerable<SoilCrop> ancestorSoilCropLists = new List<SoilCrop>();
            // LL15 is here as this is the default value.
            List<string> newSoilCropNames = new List<string> { "LL15" };
            Soil ancestorSoil = FindAncestor<Soil>();
            if (ancestorSoil != null)
            {
                ancestorSoilCropLists = ancestorSoil.FindAllDescendants<SoilCrop>();
                newSoilCropNames.AddRange(ancestorSoilCropLists.Select(s => s.Name.Replace("Soil", "")));
            }
            return newSoilCropNames;
        }

        /// <summary>
        /// Checks to make sure every InitialValue value is within airdry and SAT values.
        /// </summary>
        public bool AreInitialValuesWithinPhysicalBoundaries()
        {
            if (this.Physical == null)
                throw new Exception("To check boundaries of InitialValues Physical must not be null.");

            if (InitialValues.Length != Thickness.Length)
                return false;

            var mappedInitialValues = SoilUtilities.MapConcentration(InitialValues, Thickness, Physical.Thickness, MathUtilities.LastValue(Physical.LL15));

            for (int i = 0; i < mappedInitialValues.Length; i++)
            {
                if (mappedInitialValues[i] < Physical.AirDry[i] || mappedInitialValues[i] > Physical.SAT[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Attempts to get an appropriate SoilCrop.
        /// </summary>
        /// <exception cref="Exception"></exception>
        private SoilCrop GetCropSoil()
        {
            var physical = FindSibling<Physical>();
            if (physical == null)
                physical = FindInScope<Physical>();
                if (physical == null)
                    throw new Exception($"Unable to locate a Physical node when updating {this.Name}.");
            var plantCrop = physical.FindChild<SoilCrop>(RelativeTo + "Soil");
            if (plantCrop == null)
                throw new Exception($"Unable to locate an appropriate SoilCrop with the name of {RelativeTo + "Soil"} under {physical.Name}.");
            return plantCrop;
        }
    }
}
