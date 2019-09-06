namespace Models.WaterModel
{
    using APSIM.Shared.Utilities;
    using Core;
    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// Lateral movement of water is calculated from a user specified lateral inflow ('InFlow'). 
    /// 
    /// Lateral Outflow is the flow that occurs as a result of the soil water going above DUL and the soil being on a slope. So if there is no slope and the water goes above DUL there is no lateral outflow. KLAT is just the lateral resistance of the soil to this flow. It is a soil water conductivity.
    ///
    /// The calculation of lateral outflow on a layer basis is now performed using the equation: 
    /// Lateral flow for a layer = KLAT * d * s / (1 + s<sup>2</sup>)<sup>0.5</sup> * L / A * unit conversions.
    /// Where: 
    ///     KLAT = lateral conductivity (mm/day)
    ///     d = depth of saturation in the layer(mm) = Thickness * (SW - DUL) / (SAT - DUL) if SW > DUL.
    ///     (Note this allows lateral flow in any "saturated" layer, not just those inside a water table.)
    ///     s = slope(m / m)
    ///     L = catchment discharge width. Basically, it's the width of the downslope boundary of the catchment. (m)
    ///     A = catchment area. (m<sup>2</sup>)
    /// 
    /// NB. with Lateral Inflow it is assumed that ALL the water goes straight into the layer. 
    /// Irrespective of the layers ability to hold it. It is like an irrigation. 
    /// KLAT has no effect and does not alter the amount of water coming into the layer. 
    /// KLAT only alters the amount of water flowing out of the layer
    /// </summary>
    [Serializable]
    public class LateralFlowModel : Model
    {
        /// <summary>The water movement model.</summary>
        [Link]
        private WaterBalance soil = null;

        /// <summary>The discharge_width (m)</summary>
        public double DischargeWidth { get; set; }

        /// <summary>The slope</summary>
        public double Slope { get; set; } 

        /// <summary>The catchment_area (m2)</summary>
        public double CatchmentArea { get; set; }

        /// <summary>The KLAT (mm/day)</summary>
        public double[] KLAT { get; set; }

        /// <summary>The amount of incoming water (mm)</summary>
        [XmlIgnore]
        public double[] InFlow { get; set; }

        /// <summary>Constructor</summary>
        public LateralFlowModel()
        {
            DischargeWidth = 5.0;
            Slope = 0.5;
            CatchmentArea = 10.0;
        }

        /// <summary>Perform the movement of water.</summary>
        public double[] Values
        {
            get
            {
                // Lateral flow does not move solutes. We should add this feature one day.
                if (InFlow == null)
                    return new double[0];

                else
                {
                    double[] Out = new double[InFlow.Length];
                    double[] SW = MathUtilities.Add(soil.Water, InFlow);
                    double[] DUL = MathUtilities.Multiply(soil.Properties.Water.DUL, soil.Properties.Water.Thickness);
                    double[] SAT = MathUtilities.Multiply(soil.Properties.Water.SAT, soil.Properties.Water.Thickness);

                    for (int layer = 0; layer < soil.Properties.Water.Thickness.Length; layer++)
                    {
                        // Calculate depth of water table (m)
                        double depthWaterTable = soil.Properties.Water.Thickness[layer] * MathUtilities.Divide((SW[layer] - DUL[layer]), (SAT[layer] - DUL[layer]), 0.0);
                        depthWaterTable = Math.Max(0.0, depthWaterTable);  // water table depth in layer must be +ve

                        // Calculate out flow (mm)
                        double i, j;
                        i = KLAT[layer] * depthWaterTable * (DischargeWidth / UnitConversion.mm2m) * Slope;
                        j = (CatchmentArea * UnitConversion.sm2smm) * (Math.Pow((1.0 + Math.Pow(Slope, 2)), 0.5));
                        Out[layer] = MathUtilities.Divide(i, j, 0.0);

                        // Bound out flow to max flow
                        double max_flow = Math.Max(0.0, (SW[layer] - DUL[layer]));
                        Out[layer] = MathUtilities.Bound(Out[layer], 0.0, max_flow);
                    }

                    return Out;
                }
            }
        }

    }
}
