namespace Models.Soils.Standardiser
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// A class for checking a soil for errors.
    /// </summary>
    public class SoilChecker
    {       
        /// <summary>Checks validity of soil water parameters</summary>
        /// <param name="soilToCheck">The soil to check.</param>
        /// <returns>Error messages.</returns>
        public static string Check(Soil soilToCheck)
        {
            const double min_sw = 0.0;
            const double specific_bd = 2.65; // (g/cc)
            string Msg = "";

            var soil = Apsim.Clone(soilToCheck) as Soil;
            SoilStandardiser.Standardise(soil);

            foreach (var soilCrop in soilToCheck.Crops)
            {
                if (soilCrop != null)
                {
                    double[] LL = soilCrop.LL;
                    double[] KL = soilCrop.KL;
                    double[] XF = soilCrop.XF;

                    if (!MathUtilities.ValuesInArray(LL) ||
                        !MathUtilities.ValuesInArray(KL) ||
                        !MathUtilities.ValuesInArray(XF))
                        Msg += "Values for LL, KL or XF are missing for crop " + soilCrop.Name + "\r\n";

                    else
                    {
                        for (int layer = 0; layer != soil.Thickness.Length; layer++)
                        {
                            int RealLayerNumber = layer + 1;

                            if (KL[layer] == MathUtilities.MissingValue)
                                Msg += soilCrop.Name + " KL value missing"
                                         + " in layer " + RealLayerNumber.ToString() + "\r\n";

                            else if (MathUtilities.GreaterThan(KL[layer], 1, 3))
                                Msg += soilCrop.Name + " KL value of " + KL[layer].ToString("f3")
                                         + " in layer " + RealLayerNumber.ToString() + " is greater than 1"
                                         + "\r\n";

                            if (XF[layer] == MathUtilities.MissingValue)
                                Msg += soilCrop.Name + " XF value missing"
                                         + " in layer " + RealLayerNumber.ToString() + "\r\n";

                            else if (MathUtilities.GreaterThan(XF[layer], 1, 3))
                                Msg += soilCrop.Name + " XF value of " + XF[layer].ToString("f3")
                                         + " in layer " + RealLayerNumber.ToString() + " is greater than 1"
                                         + "\r\n";

                            if (LL[layer] == MathUtilities.MissingValue)
                                Msg += soilCrop.Name + " LL value missing"
                                         + " in layer " + RealLayerNumber.ToString() + "\r\n";

                            else if (MathUtilities.LessThan(LL[layer], soil.AirDry[layer], 3))
                                Msg += soilCrop.Name + " LL of " + LL[layer].ToString("f3")
                                             + " in layer " + RealLayerNumber.ToString() + " is below air dry value of " + soil.AirDry[layer].ToString("f3")
                                           + "\r\n";

                            else if (MathUtilities.GreaterThan(LL[layer], soil.DUL[layer], 3))
                                Msg += soilCrop.Name + " LL of " + LL[layer].ToString("f3")
                                             + " in layer " + RealLayerNumber.ToString() + " is above drained upper limit of " + soil.DUL[layer].ToString("f3")
                                           + "\r\n";
                        }
                    }
                }
            }

            // Check other profile variables.
            for (int layer = 0; layer != soil.Thickness.Length; layer++)
            {
                double max_sw = MathUtilities.Round(1.0 - soil.BD[layer] / specific_bd, 3);
                int RealLayerNumber = layer + 1;

                if (soil.AirDry[layer] == MathUtilities.MissingValue)
                    Msg += " Air dry value missing"
                             + " in layer " + RealLayerNumber.ToString() + "\r\n";

                else if (MathUtilities.LessThan(soil.AirDry[layer], min_sw, 3))
                    Msg += " Air dry lower limit of " + soil.AirDry[layer].ToString("f3")
                                       + " in layer " + RealLayerNumber.ToString() + " is below acceptable value of " + min_sw.ToString("f3")
                               + "\r\n";

                if (soil.LL15[layer] == MathUtilities.MissingValue)
                    Msg += "15 bar lower limit value missing"
                             + " in layer " + RealLayerNumber.ToString() + "\r\n";

                else if (MathUtilities.LessThan(soil.LL15[layer], soil.AirDry[layer], 3))
                    Msg += "15 bar lower limit of " + soil.LL15[layer].ToString("f3")
                                 + " in layer " + RealLayerNumber.ToString() + " is below air dry value of " + soil.AirDry[layer].ToString("f3")
                               + "\r\n";

                if (soil.DUL[layer] == MathUtilities.MissingValue)
                    Msg += "Drained upper limit value missing"
                             + " in layer " + RealLayerNumber.ToString() + "\r\n";

                else if (MathUtilities.LessThan(soil.DUL[layer], soil.LL15[layer], 3))
                    Msg += "Drained upper limit of " + soil.DUL[layer].ToString("f3")
                                 + " in layer " + RealLayerNumber.ToString() + " is at or below lower limit of " + soil.LL15[layer].ToString("f3")
                               + "\r\n";

                if (soil.SAT[layer] == MathUtilities.MissingValue)
                    Msg += "Saturation value missing"
                             + " in layer " + RealLayerNumber.ToString() + "\r\n";

                else if (MathUtilities.LessThan(soil.SAT[layer], soil.DUL[layer], 3))
                    Msg += "Saturation of " + soil.SAT[layer].ToString("f3")
                                 + " in layer " + RealLayerNumber.ToString() + " is at or below drained upper limit of " + soil.DUL[layer].ToString("f3")
                               + "\r\n";

                else if (MathUtilities.GreaterThan(soil.SAT[layer], max_sw, 3))
                {
                    double max_bd = (1.0 - soil.SAT[layer]) * specific_bd;
                    Msg += "Saturation of " + soil.SAT[layer].ToString("f3")
                                 + " in layer " + RealLayerNumber.ToString() + " is above acceptable value of  " + max_sw.ToString("f3")
                               + ". You must adjust bulk density to below " + max_bd.ToString("f3")
                               + " OR saturation to below " + max_sw.ToString("f3")
                               + "\r\n";
                }

                if (soil.BD[layer] == MathUtilities.MissingValue)
                    Msg += "BD value missing"
                             + " in layer " + RealLayerNumber.ToString() + "\r\n";

                else if (MathUtilities.GreaterThan(soil.BD[layer], 2.65, 3))
                    Msg += "BD value of " + soil.BD[layer].ToString("f3")
                                 + " in layer " + RealLayerNumber.ToString() + " is greater than the theoretical maximum of 2.65"
                               + "\r\n";
            }

            if (soil.Initial.OC.Length == 0)
                throw new Exception("Cannot find OC values in soil");

            for (int layer = 0; layer != soil.Thickness.Length; layer++)
            {
                int RealLayerNumber = layer + 1;
                if (soil.Initial.OC[layer] == MathUtilities.MissingValue)
                    Msg += "OC value missing"
                             + " in layer " + RealLayerNumber.ToString() + "\r\n";

                else if (MathUtilities.LessThan(soil.Initial.OC[layer], 0.01, 3))
                    Msg += "OC value of " + soil.Initial.OC[layer].ToString("f3")
                                  + " in layer " + RealLayerNumber.ToString() + " is less than 0.01"
                                  + "\r\n";

                if (soil.Initial.PH[layer] == MathUtilities.MissingValue)
                    Msg += "PH value missing"
                             + " in layer " + RealLayerNumber.ToString() + "\r\n";

                else if (MathUtilities.LessThan(soil.Initial.PH[layer], 3.5, 3))
                    Msg += "PH value of " + soil.Initial.PH[layer].ToString("f3")
                                  + " in layer " + RealLayerNumber.ToString() + " is less than 3.5"
                                  + "\r\n";
                else if (MathUtilities.GreaterThan(soil.Initial.PH[layer], 11, 3))
                    Msg += "PH value of " + soil.Initial.PH[layer].ToString("f3")
                                  + " in layer " + RealLayerNumber.ToString() + " is greater than 11"
                                  + "\r\n";
            }

            if (!MathUtilities.ValuesInArray(soil.Initial.SW))
                Msg += "No starting soil water values found.\r\n";
            else
                for (int layer = 0; layer != soil.Thickness.Length; layer++)
                {
                    int RealLayerNumber = layer + 1;

                    if (soil.Initial.SW[layer] == MathUtilities.MissingValue)
                        Msg += "Soil water value missing"
                                    + " in layer " + RealLayerNumber.ToString() + "\r\n";

                    else if (MathUtilities.GreaterThan(soil.Initial.SW[layer], soil.SAT[layer], 3))
                        Msg += "Soil water of " + soil.Initial.SW[layer].ToString("f3")
                                        + " in layer " + RealLayerNumber.ToString() + " is above saturation of " + soil.SAT[layer].ToString("f3")
                                        + "\r\n";

                    else if (MathUtilities.LessThan(soil.Initial.SW[layer], soil.AirDry[layer], 3))
                        Msg += "Soil water of " + soil.Initial.SW[layer].ToString("f3")
                                        + " in layer " + RealLayerNumber.ToString() + " is below air-dry value of " + soil.AirDry[layer].ToString("f3")
                                        + "\r\n";
                }

            if (!MathUtilities.ValuesInArray(soil.Initial.NO3N))
                Msg += "No starting NO3 values found.\r\n";
            if (!MathUtilities.ValuesInArray(soil.Initial.NH4N))
                Msg += "No starting NH4 values found.\r\n";

            return Msg;
        }
    }
}
