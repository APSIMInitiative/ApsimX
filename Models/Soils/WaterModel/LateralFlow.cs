namespace Models.WaterModel
{
    using APSIM.Shared.Utilities;
    using Core;
    using System;
    using Newtonsoft.Json;
    using Models.Soils;

    /// <summary>
    /// Lateral movement of water is calculated from a user specified lateral inflow ('InFlow'). 
    /// 
    /// Lateral Outflow is the flow that occurs as a result of the soil water going above DUL and the soil being on a slope. So if there is no slope and the water goes above DUL there is no lateral outflow. KLAT is just the lateral resistance of the soil to this flow. It is a soil water conductivity.
    ///
    /// The calculation of lateral outflow on a layer basis is now performed using the equation: 
    /// Lateral flow for a layer = KLAT * d * s / (1 + s^2^)^0.5^ * L / A * unit conversions.
    /// Where: 
    ///     KLAT = lateral conductivity (mm/day)
    ///     d = depth of saturation in the layer(mm) = Thickness * (SW - DUL) / (SAT - DUL) if SW > DUL.
    ///     (Note this allows lateral flow in any "saturated" layer, not just those inside a water table.)
    ///     s = slope(m / m)
    ///     L = catchment discharge width. Basically, it's the width of the downslope boundary of the catchment. (m)
    ///     A = catchment area. (m^2^)
    /// 
    /// NB. with Lateral Inflow it is assumed that ALL the water goes straight into the layer. 
    /// Irrespective of the layers ability to hold it. It is like an irrigation. 
    /// KLAT has no effect and does not alter the amount of water coming into the layer. 
    /// KLAT only alters the amount of water flowing out of the layer
    /// </summary>
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType = typeof(WaterBalance))]
    [Serializable]
    public class LateralFlowModel : Model
    {
        /// <summary>The water movement model.</summary>
        [Link]
        private WaterBalance soilWater = null;

        /// <summary> The field. </summary>
        [Link]
        private Zone field = null;
        
        /// <summary>Access the soil physical properties.</summary>
        [Link] 
        private IPhysical soilPhysical = null;

        /// <summary>The amount of incoming water (mm)</summary>
        [JsonIgnore]
        public double[] InFlow { get; set; }

        /// <summary>The amount of outgoing water (mm).</summary>
        public double[] OutFlow { get; private set; } = new double[0];

        /// <summary>Perform the movement of water.</summary>
        public void Calculate()
        {
            // Lateral flow does not move solutes. We should add this feature one day.
            if (InFlow != null)
            {
                if (OutFlow.Length != InFlow.Length)
                    OutFlow = new double[InFlow.Length];
                double[] SW = MathUtilities.Add(soilWater.Water, InFlow);
                double[] DUL = MathUtilities.Multiply(soilPhysical.DUL, soilPhysical.Thickness);
                double[] SAT = MathUtilities.Multiply(soilPhysical.SAT, soilPhysical.Thickness);

                for (int layer = 0; layer < soilPhysical.Thickness.Length; layer++)
                {
                    // Calculate depth of water table (m)
                    double depthWaterTable = soilPhysical.Thickness[layer] * MathUtilities.Divide((SW[layer] - DUL[layer]), (SAT[layer] - DUL[layer]), 0.0);
                    depthWaterTable = Math.Max(0.0, depthWaterTable);  // water table depth in layer must be +ve

                    // Calculate out flow (mm)
                    double i, j;

                    // Convert slope from degrees to m/m (proportion). Should we bound this to [0, 1]?
                    double slope = Math.Tan(field.Slope * Math.PI / 180);
                    i = soilWater.KLAT[layer] * depthWaterTable * (soilWater.DischargeWidth / UnitConversion.mm2m) * slope;
                    j = (soilWater.CatchmentArea * UnitConversion.sm2smm) * (Math.Pow((1.0 + Math.Pow(slope, 2)), 0.5));
                    OutFlow[layer] = MathUtilities.Divide(i, j, 0.0);

                    // Bound out flow to max flow
                    double max_flow = Math.Max(0.0, (SW[layer] - DUL[layer]));
                    OutFlow[layer] = MathUtilities.Bound(OutFlow[layer], 0.0, max_flow);
                }
            }
        }
    }
}
