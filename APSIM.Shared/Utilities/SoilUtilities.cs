namespace APSIM.Shared.Utilities
{
    using System;
    using System.Linq;

    /// <summary>Various soil utilities.</summary>
    public class SoilUtilities
    {
        /// <summary>Convert the specified thicknesses to mid points for plotting.</summary>
        /// <param name="Thickness">The thicknesses.</param>
        static public double[] ToMidPoints(double[] Thickness)
        {
            double[] CumThickness = ToCumThickness(Thickness);
            double[] MidPoints = new double[CumThickness.Length];
            for (int Layer = 0; Layer != CumThickness.Length; Layer++)
            {
                if (Layer == 0)
                    MidPoints[Layer] = CumThickness[Layer] / 2.0;
                else
                    MidPoints[Layer] = (CumThickness[Layer] + CumThickness[Layer - 1]) / 2.0;
            }
            return MidPoints;
        }

        /// <summary>Returns a cumulative thickness based on the specified thickness.</summary>
        /// <param name="Thickness">The thickness.</param>
        static public double[] ToCumThickness(double[] Thickness)
        {
            // ------------------------------------------------
            // Return cumulative thickness for each layer - mm
            // ------------------------------------------------
            double[] CumThickness = new double[Thickness.Length];
            if (Thickness.Length > 0)
            {
                CumThickness[0] = Thickness[0];
                for (int Layer = 1; Layer != Thickness.Length; Layer++)
                    CumThickness[Layer] = Thickness[Layer] + CumThickness[Layer - 1];
            }
            return CumThickness;
        }

        /// <summary>Return the index of the layer that contains the specified depth.</summary>
        /// <param name="thickness">The soil layer thicknesses.</param>
        /// <param name="depth">The depth to search for.</param>
        /// <returns></returns>
        static public int LayerIndexOfDepth(double[] thickness, double depth)
        {
            if (depth > thickness.Sum())
                throw new Exception("Depth deeper than bottom of soil profile");
            else
                return LayerIndexOfClosestDepth(thickness, depth);
        }

        /// <summary>Return the index of the closest layer that contains the specified depth.</summary>
        /// <param name="thickness">The soil layer thicknesses.</param>
        /// <param name="depth">The depth to search for.</param>
        /// <returns></returns>
        static public int LayerIndexOfClosestDepth(double[] thickness, double depth)
        {
            double CumDepth = 0;
            for (int i = 0; i < thickness.Length; i++)
            {
                CumDepth = CumDepth + thickness[i];
                if (CumDepth >= depth) { return i; }
            }
            throw new Exception("Depth deeper than bottom of soil profile");
        }

        /// <summary>Returns the proportion that 'depth' is through the layer.</summary>
        /// <param name="thickness">Soil layer thickness.</param>
        /// <param name="layerIndex">The layer index</param>
        /// <param name="depth">The depth</param>
        public static double ProportionThroughLayer(double[] thickness, int layerIndex, double depth)
        {
            double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
            for (int i = 0; i <= layerIndex; i++)
                depth_to_layer_bottom += thickness[i];

            double depth_to_layer_top = depth_to_layer_bottom - thickness[layerIndex];
            double depth_to_root = Math.Min(depth_to_layer_bottom, depth);
            double depth_of_root_in_layer = Math.Max(0.0, depth_to_root - depth_to_layer_top);

            return depth_of_root_in_layer / thickness[layerIndex];
        }

        /// <summary>Calculate conversion factor from kg/ha to ppm (mg/kg)</summary>
        /// <param name="thickness">Soil layer thickness.</param>
        /// <param name="bd">Bulk density.</param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static double[] kgha2ppm(double[] thickness, double[] bd, double[] values)
        {
            if (values == null)
                return null;

            double[] ppm = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
                ppm[i] = values[i] * (100.0 / (bd[i] * thickness[i]));
            return ppm;
        }

        /// <summary>Calculate conversion factor from ppm to kg/ha</summary>
        /// <param name="thickness">Soil layer thickness.</param>
        /// <param name="bd">Bulk density.</param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static double[] ppm2kgha(double[] thickness, double[] bd, double[] values)
        {
            if (values == null)
                return null;

            double[] kgha = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
                kgha[i] = values[i] * (bd[i] * thickness[i] / 100);
            return kgha;
        }

        /// <summary>Convert an array of thickness (mm) to depth strings (cm)</summary>
        /// <param name="Thickness">The thickness.</param>
        /// <returns></returns>
        public static string[] ToDepthStrings(double[] Thickness)
        {
            if (Thickness == null)
                return null;
            string[] Strings = new string[Thickness.Length];
            double DepthSoFar = 0;
            for (int i = 0; i != Thickness.Length; i++)
            {
                if (Thickness[i] == MathUtilities.MissingValue)
                    Strings[i] = "";
                else
                {
                    double ThisThickness = Thickness[i] / 10; // to cm
                    double TopOfLayer = DepthSoFar;
                    double BottomOfLayer = DepthSoFar + ThisThickness;
                    Strings[i] = TopOfLayer.ToString() + "-" + BottomOfLayer.ToString();
                    DepthSoFar = BottomOfLayer;
                }
            }
            return Strings;
        }

        /// <summary>
        /// Convert an array of depth strings (cm) to thickness (mm) e.g.
        /// "0-10", "10-30"
        /// To
        /// 100, 200
        /// </summary>
        /// <param name="DepthStrings">The depth strings.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Invalid layer string:  + DepthStrings[i] +
        ///                                   . String must be of the form: 10-30</exception>
        public static double[] ToThickness(string[] DepthStrings)
        {
            if (DepthStrings == null)
                return null;

            double[] Thickness = new double[DepthStrings.Length];
            for (int i = 0; i != DepthStrings.Length; i++)
            {
                if (string.IsNullOrEmpty(DepthStrings[i]))
                    Thickness[i] = MathUtilities.MissingValue;
                else
                {
                    int PosDash = DepthStrings[i].IndexOf('-');
                    if (PosDash == -1)
                        throw new Exception("Invalid layer string: " + DepthStrings[i] +
                                  ". String must be of the form: 10-30");
                    double TopOfLayer;
                    double BottomOfLayer;

                    if (!Double.TryParse(DepthStrings[i].Substring(0, PosDash), out TopOfLayer))
                        throw new Exception("Invalid string for layer top: '" + DepthStrings[i].Substring(0, PosDash) + "'");
                    if (!Double.TryParse(DepthStrings[i].Substring(PosDash + 1), out BottomOfLayer))
                        throw new Exception("Invalid string for layer bottom: '" + DepthStrings[i].Substring(PosDash + 1) + "'");
                    Thickness[i] = (BottomOfLayer - TopOfLayer) * 10;
                }
            }
            return Thickness;
        }

        /// <summary>
        /// Plant available water for the specified crop. Will throw if crop not found. Units: mm/mm
        /// </summary>
        /// <param name="Thickness">The thickness.</param>
        /// <param name="LL">The ll.</param>
        /// <param name="DUL">The dul.</param>
        /// <param name="XF">The xf.</param>
        /// <returns></returns>
        public static double[] CalcPAWC(double[] Thickness, double[] LL, double[] DUL, double[] XF)
        {
            double[] PAWC = new double[Thickness.Length];
            if (LL == null || DUL == null)
                return PAWC;
            if (Thickness.Length != DUL.Length || Thickness.Length != LL.Length)
                throw new Exception("Number of soil layers in SoilWater is different to number of layers in SoilWater.Crop");

            for (int layer = 0; layer != Thickness.Length; layer++)
                if (DUL[layer] == MathUtilities.MissingValue ||
                    LL[layer] == MathUtilities.MissingValue)
                    PAWC[layer] = 0;
                else
                    PAWC[layer] = Math.Max(DUL[layer] - LL[layer], 0.0);

            bool ZeroXFFound = false;
            for (int layer = 0; layer != Thickness.Length; layer++)
                if (ZeroXFFound || XF != null && XF[layer] == 0)
                {
                    ZeroXFFound = true;
                    PAWC[layer] = 0;
                }
            return PAWC;
        }

    }
}
