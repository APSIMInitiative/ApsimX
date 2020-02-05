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


        /// <summary>Checks validity of soil water parameters.
        /// This method is called at "DoInitialSummary"
        /// So it has already been standardised, so just do the comparisons</summary>
        /// <returns>Error messages.</returns>
        public static string Check(Soil soil)
        {
            return Check(soil, soil);
        }




        /// <summary>Checks validity of soil water parameters.
        /// This method is called from the Context Menu by Selecting "Check Soil"
        /// So you need to standardise it before doing the comparisons</summary>
        /// <param name="soilToCheck">The soil to check.</param>
        /// <returns>Error messages.</returns>
        public static string CheckWithStandardisation(Soil soilToCheck)
        {
            var soil = Apsim.Clone(soilToCheck) as Soil;
            SoilStandardiser.Standardise(soil);

            return Check(soilToCheck, soil);
        }



        /// <summary>Checks validity of soil parameters</summary>
        /// <param name="original">The soil to check.</param>
        /// /// <param name="standardised">Standardised version of the soil to check.</param>
        /// <returns>Error messages.</returns>
        private static string Check(Soil original, Soil standardised)
        {
            const double min_sw = 0.0;
            const double specific_bd = 2.65; // (g/cc)
            string Msg = "";

            //Weirdo is an experimental soil water model that does not have the same soil water parameters
            //so don't do any of these tests if Weirdo is plugged into this simulation.
            if (original.Weirdo == null ) 
            {
                foreach (var soilCrop in original.Crops)
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
                            for (int layer = 0; layer != standardised.Thickness.Length; layer++)
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

                                else if (MathUtilities.LessThan(LL[layer], standardised.AirDry[layer], 3))
                                    Msg += soilCrop.Name + " LL of " + LL[layer].ToString("f3")
                                                 + " in layer " + RealLayerNumber.ToString() + " is below air dry value of " + standardised.AirDry[layer].ToString("f3")
                                               + "\r\n";

                                else if (MathUtilities.GreaterThan(LL[layer], standardised.DUL[layer], 3))
                                    Msg += soilCrop.Name + " LL of " + LL[layer].ToString("f3")
                                                 + " in layer " + RealLayerNumber.ToString() + " is above drained upper limit of " + standardised.DUL[layer].ToString("f3")
                                               + "\r\n";
                            }
                        }
                    }
                }
            


                // Check other profile variables.
                for (int layer = 0; layer != standardised.Thickness.Length; layer++)
                {
                    double max_sw = MathUtilities.Round(1.0 - standardised.BD[layer] / specific_bd, 3);
                    int RealLayerNumber = layer + 1;

                    if (standardised.AirDry[layer] == MathUtilities.MissingValue)
                        Msg += " Air dry value missing"
                                 + " in layer " + RealLayerNumber.ToString() + "\r\n";

                    else if (MathUtilities.LessThan(standardised.AirDry[layer], min_sw, 3))
                        Msg += " Air dry lower limit of " + standardised.AirDry[layer].ToString("f3")
                                           + " in layer " + RealLayerNumber.ToString() + " is below acceptable value of " + min_sw.ToString("f3")
                                   + "\r\n";

                    if (standardised.LL15[layer] == MathUtilities.MissingValue)
                        Msg += "15 bar lower limit value missing"
                                 + " in layer " + RealLayerNumber.ToString() + "\r\n";

                    else if (MathUtilities.LessThan(standardised.LL15[layer], standardised.AirDry[layer], 3))
                        Msg += "15 bar lower limit of " + standardised.LL15[layer].ToString("f3")
                                     + " in layer " + RealLayerNumber.ToString() + " is below air dry value of " + standardised.AirDry[layer].ToString("f3")
                                   + "\r\n";

                    if (standardised.DUL[layer] == MathUtilities.MissingValue)
                        Msg += "Drained upper limit value missing"
                                 + " in layer " + RealLayerNumber.ToString() + "\r\n";

                    else if (MathUtilities.LessThan(standardised.DUL[layer], standardised.LL15[layer], 3))
                        Msg += "Drained upper limit of " + standardised.DUL[layer].ToString("f3")
                                     + " in layer " + RealLayerNumber.ToString() + " is at or below lower limit of " + standardised.LL15[layer].ToString("f3")
                                   + "\r\n";

                    if (standardised.SAT[layer] == MathUtilities.MissingValue)
                        Msg += "Saturation value missing"
                                 + " in layer " + RealLayerNumber.ToString() + "\r\n";

                    else if (MathUtilities.LessThan(standardised.SAT[layer], standardised.DUL[layer], 3))
                        Msg += "Saturation of " + standardised.SAT[layer].ToString("f3")
                                     + " in layer " + RealLayerNumber.ToString() + " is at or below drained upper limit of " + standardised.DUL[layer].ToString("f3")
                                   + "\r\n";

                    else if (MathUtilities.GreaterThan(standardised.SAT[layer], max_sw, 3))
                    {
                        double max_bd = (1.0 - standardised.SAT[layer]) * specific_bd;
                        Msg += "Saturation of " + standardised.SAT[layer].ToString("f3")
                                     + " in layer " + RealLayerNumber.ToString() + " is above acceptable value of  " + max_sw.ToString("f3")
                                   + ". You must adjust bulk density to below " + max_bd.ToString("f3")
                                   + " OR saturation to below " + max_sw.ToString("f3")
                                   + "\r\n";
                    }

                    if (standardised.BD[layer] == MathUtilities.MissingValue)
                        Msg += "BD value missing"
                                 + " in layer " + RealLayerNumber.ToString() + "\r\n";

                    else if (MathUtilities.GreaterThan(standardised.BD[layer], 2.65, 3))
                        Msg += "BD value of " + standardised.BD[layer].ToString("f3")
                                     + " in layer " + RealLayerNumber.ToString() + " is greater than the theoretical maximum of 2.65"
                                   + "\r\n";
                }

                if (standardised.Initial.OC.Length == 0)
                    throw new Exception("Cannot find OC values in soil");

                for (int layer = 0; layer != standardised.Thickness.Length; layer++)
                {
                    int RealLayerNumber = layer + 1;
                    if (standardised.Initial.OC[layer] == MathUtilities.MissingValue)
                        Msg += "OC value missing"
                                 + " in layer " + RealLayerNumber.ToString() + "\r\n";

                    else if (MathUtilities.LessThan(standardised.Initial.OC[layer], 0.01, 3))
                        Msg += "OC value of " + standardised.Initial.OC[layer].ToString("f3")
                                      + " in layer " + RealLayerNumber.ToString() + " is less than 0.01"
                                      + "\r\n";

                    if (standardised.Initial.PH[layer] == MathUtilities.MissingValue)
                        Msg += "PH value missing"
                                 + " in layer " + RealLayerNumber.ToString() + "\r\n";

                    else if (MathUtilities.LessThan(standardised.Initial.PH[layer], 3.5, 3))
                        Msg += "PH value of " + standardised.Initial.PH[layer].ToString("f3")
                                      + " in layer " + RealLayerNumber.ToString() + " is less than 3.5"
                                      + "\r\n";
                    else if (MathUtilities.GreaterThan(standardised.Initial.PH[layer], 11, 3))
                        Msg += "PH value of " + standardised.Initial.PH[layer].ToString("f3")
                                      + " in layer " + RealLayerNumber.ToString() + " is greater than 11"
                                      + "\r\n";
                }

                if (!MathUtilities.ValuesInArray(standardised.Initial.SW))
                    Msg += "No starting soil water values found.\r\n";
                else
                    for (int layer = 0; layer != standardised.Thickness.Length; layer++)
                    {
                        int RealLayerNumber = layer + 1;

                        if (standardised.Initial.SW[layer] == MathUtilities.MissingValue)
                            Msg += "Soil water value missing"
                                        + " in layer " + RealLayerNumber.ToString() + "\r\n";

                        else if (MathUtilities.GreaterThan(standardised.Initial.SW[layer], standardised.SAT[layer], 3))
                            Msg += "Soil water of " + standardised.Initial.SW[layer].ToString("f3")
                                            + " in layer " + RealLayerNumber.ToString() + " is above saturation of " + standardised.SAT[layer].ToString("f3")
                                            + "\r\n";

                        else if (MathUtilities.LessThan(standardised.Initial.SW[layer], standardised.AirDry[layer], 3))
                            Msg += "Soil water of " + standardised.Initial.SW[layer].ToString("f3")
                                            + " in layer " + RealLayerNumber.ToString() + " is below air-dry value of " + standardised.AirDry[layer].ToString("f3")
                                            + "\r\n";
                    }

                if (!MathUtilities.ValuesInArray(standardised.Initial.NO3N))
                    Msg += "No starting NO3 values found.\r\n";
                if (!MathUtilities.ValuesInArray(standardised.Initial.NH4N))
                    Msg += "No starting NH4 values found.\r\n";


            }

            return Msg;
        }
    }
}
