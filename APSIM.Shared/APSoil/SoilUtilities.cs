namespace APSIM.Shared.APSoil
{
    using APSIM.Shared.Utilities;
    using System;
    using System.IO;
    using System.Xml.Serialization;

    /// <summary>Various soil utilities.</summary>
    public class SoilUtilities
    {
        /// <summary>Create a soil object from the XML passed in.</summary>
        /// <param name="Xml">The XML.</param>
        /// <returns></returns>
        public static Soil FromXML(string Xml)
        {
            XmlSerializer x = new XmlSerializer(typeof(Soil));
            StringReader F = new StringReader(Xml);
            return x.Deserialize(F) as Soil;
        }

        /// <summary>Write soil to XML</summary>
        /// <param name="soil">The soil.</param>
        /// <returns></returns>
        public static string ToXML(Soil soil)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            XmlSerializer x = new XmlSerializer(typeof(Soil));

            StringWriter Out = new StringWriter();
            x.Serialize(Out, soil, ns);
            string st = Out.ToString();
            if (st.Length > 5 && st.Substring(0, 5) == "<?xml")
            {
                // remove the first line: <?xml version="1.0"?>/n
                int posEol = st.IndexOf("\n");
                if (posEol != -1)
                    return st.Substring(posEol + 1);
            }
            return st;
        }

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
        /// <param name="soil">The soil</param>
        /// <param name="depth">The depth to search for.</param>
        /// <returns></returns>
        static public int FindLayerIndex(Soil soil, double depth)
        {
            return Array.FindIndex(ToCumThickness(soil.Water.Thickness), d => d >= depth);
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

    }
}
