namespace Models.Soils.Standardiser
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A class for checking a soil for errors.
    /// </summary>
    public class SoilChecker
    {
        /// <summary>
        /// Checks validity of soil parameters. Throws if soil is invalid.
        /// Standardises the soil before performing tests.
        /// </summary>
        /// <param name="soilToCheck">The soil to check.</param>
        public static void CheckWithStandardisation(Soil soilToCheck)
        {
            var soil = Apsim.Clone(soilToCheck) as Soil;
            SoilStandardiser.Standardise(soil);

            Check(soil);
        }

        /// <summary>
        /// Checks validity of soil parameters. Throws if soil is invalid.
        /// Does not standardise the soil before performing tests.
        /// </summary>
        /// <param name="soil">The soil to check.</param>
        public static void Check(Soil soil)
        {
            var weirdo = soil.FindChild<WEIRDO>();
            var initial = soil.FindChild<Sample>();
            var physical = soil.FindChild<IPhysical>();
            const double min_sw = 0.0;
            const double specific_bd = 2.65; // (g/cc)
            StringBuilder message = new StringBuilder();

            //Weirdo is an experimental soil water model that does not have the same soil water parameters
            //so don't do any of these tests if Weirdo is plugged into this simulation.
            if (weirdo == null) 
            {
                var crops = soil.FindAllDescendants<SoilCrop>();
                foreach (var soilCrop in crops)
                {
                    if (soilCrop != null)
                    {
                        double[] LL = soilCrop.LL;
                        double[] KL = soilCrop.KL;
                        double[] XF = soilCrop.XF;

                        if (!MathUtilities.ValuesInArray(LL) || !MathUtilities.ValuesInArray(KL) || !MathUtilities.ValuesInArray(XF))
                            message.AppendLine($"Values for LL, KL or XF are missing for crop {soilCrop.Name}");
                        else
                        {
                            for (int layer = 0; layer < physical.Thickness.Length; layer++)
                            {
                                int layerNumber = layer + 1;

                                if (KL[layer] == MathUtilities.MissingValue || double.IsNaN(KL[layer]))
                                    message.AppendLine($"{soilCrop.Name} KL value missing in layer {layerNumber}");
                                else if (MathUtilities.GreaterThan(KL[layer], 1, 3))
                                    message.AppendLine($"{soilCrop.Name} KL value of {KL[layer].ToString("f3")} in layer {layerNumber} is greater than 1");

                                if (XF[layer] == MathUtilities.MissingValue || double.IsNaN(XF[layer]))
                                    message.AppendLine($"{soilCrop.Name} XF value missing in layer {layerNumber}");
                                else if (MathUtilities.GreaterThan(XF[layer], 1, 3))
                                    message.AppendLine($"{soilCrop.Name} XF value of {XF[layer].ToString("f3")} in layer {layerNumber} is greater than 1");

                                if (LL[layer] == MathUtilities.MissingValue || double.IsNaN(LL[layer]))
                                    message.AppendLine($"{soilCrop.Name} LL value missing in layer {layerNumber}");
                                else if (MathUtilities.LessThan(LL[layer], physical.AirDry[layer], 3))
                                    message.AppendLine($"{soilCrop.Name} LL of {LL[layer].ToString("f3")} in layer {layerNumber} is below air dry value of {physical.AirDry[layer].ToString("f3")}");
                                else if (MathUtilities.GreaterThan(LL[layer], physical.DUL[layer], 3))
                                    message.AppendLine($"{soilCrop.Name} LL of {LL[layer].ToString("f3")} in layer {layerNumber} is above drained upper limit of {physical.DUL[layer].ToString("f3")}");
                            }
                        }
                    }
                }

                // Check other profile variables.
                for (int layer = 0; layer < physical.Thickness.Length; layer++)
                {
                    double max_sw = MathUtilities.Round(1.0 - physical.BD[layer] / specific_bd, 3);
                    int layerNumber = layer + 1;

                    if (physical.AirDry[layer] == MathUtilities.MissingValue || double.IsNaN(physical.AirDry[layer]))
                        message.AppendLine($"Air dry value missing in layer {layerNumber}");
                    else if (MathUtilities.LessThan(physical.AirDry[layer], min_sw, 3))
                        message.AppendLine($"Air dry lower limit of {physical.AirDry[layer].ToString("f3")} in layer {layerNumber} is below acceptable value of {min_sw.ToString("f3")}");

                    if (physical.LL15[layer] == MathUtilities.MissingValue || double.IsNaN(physical.LL15[layer]))
                        message.AppendLine($"15 bar lower limit value missing in layer {layerNumber}");
                    else if (MathUtilities.LessThan(physical.LL15[layer], physical.AirDry[layer], 3))
                        message.AppendLine($"15 bar lower limit of {physical.LL15[layer].ToString("f3")} in layer {layerNumber} is below air dry value of {physical.AirDry[layer].ToString("f3")}");

                    if (physical.DUL[layer] == MathUtilities.MissingValue || double.IsNaN(physical.DUL[layer]))
                        message.AppendLine($"Drained upper limit value missing in layer {layerNumber}");
                    else if (MathUtilities.LessThan(physical.DUL[layer], physical.LL15[layer], 3))
                        message.AppendLine($"Drained upper limit of {physical.DUL[layer].ToString("f3")} in layer {layerNumber} is at or below lower limit of {physical.LL15[layer].ToString("f3")}");

                    if (physical.SAT[layer] == MathUtilities.MissingValue || double.IsNaN(physical.SAT[layer]))
                        message.AppendLine($"Saturation value missing in layer {layerNumber}");
                    else if (MathUtilities.LessThan(physical.SAT[layer], physical.DUL[layer], 3))
                        message.AppendLine($"Saturation of {physical.SAT[layer].ToString("f3")} in layer {layerNumber} is at or below drained upper limit of {physical.DUL[layer].ToString("f3")}");
                    else if (MathUtilities.GreaterThan(physical.SAT[layer], max_sw, 3))
                    {
                        double max_bd = (1.0 - physical.SAT[layer]) * specific_bd;
                        message.AppendLine($"Saturation of {physical.SAT[layer].ToString("f3")} in layer {layerNumber} is above acceptable value of {max_sw.ToString("f3")}. You must adjust bulk density to below {max_bd.ToString("f3")} OR saturation to below {max_sw.ToString("f3")}");
                    }

                    if (physical.BD[layer] == MathUtilities.MissingValue || double.IsNaN(physical.BD[layer]))
                        message.AppendLine($"BD value missing in layer {layerNumber}");
                    else if (MathUtilities.GreaterThan(physical.BD[layer], specific_bd, 3))
                        message.AppendLine($"BD value of {physical.BD[layer].ToString("f3")} in layer {layerNumber} is greater than the theoretical maximum of 2.65");
                }

                if (initial.OC.Length == 0)
                    message.AppendLine("Cannot find OC values in soil");
                else
                    for (int layer = 0; layer != physical.Thickness.Length; layer++)
                    {
                        int layerNumber = layer + 1;
                        if (initial.OC[layer] == MathUtilities.MissingValue || double.IsNaN(initial.OC[layer]))
                            message.AppendLine($"OC value missing in layer {layerNumber}");
                        else if (MathUtilities.LessThan(initial.OC[layer], 0.01, 3))
                            message.AppendLine($"OC value of {initial.OC[layer].ToString("f3")} in layer {layerNumber} is less than 0.01");

                        if (initial.PH[layer] == MathUtilities.MissingValue || double.IsNaN(initial.PH[layer]))
                            message.AppendLine($"PH value missing in layer {layerNumber}");
                        else if (MathUtilities.LessThan(initial.PH[layer], 3.5, 3))
                            message.AppendLine($"PH value of {initial.PH[layer].ToString("f3")} in layer {layerNumber} is less than 3.5");
                        else if (MathUtilities.GreaterThan(initial.PH[layer], 11, 3))
                            message.AppendLine($"PH value of {initial.PH[layer].ToString("f3")} in layer {layerNumber} is greater than 11");
                    }

                if (!MathUtilities.ValuesInArray(initial.SW))
                    message.AppendLine("No starting soil water values found.");
                else
                    for (int layer = 0; layer != physical.Thickness.Length; layer++)
                    {
                        int layerNumber = layer + 1;

                        if (initial.SW[layer] == MathUtilities.MissingValue || double.IsNaN(initial.SW[layer]))
                            message.AppendLine($"Soil water value missing in layer {layerNumber}");
                        else if (MathUtilities.GreaterThan(initial.SW[layer], physical.SAT[layer], 3))
                            message.AppendLine($"Soil water of {initial.SW[layer].ToString("f3")} in layer {layerNumber} is above saturation of {physical.SAT[layer].ToString("f3")}");
                        else if (MathUtilities.LessThan(initial.SW[layer], physical.AirDry[layer], 3))
                            message.AppendLine($"Soil water of {initial.SW[layer].ToString("f3")} in layer {layerNumber} is below air-dry value of {physical.AirDry[layer].ToString("f3")}");
                    }

                if (!MathUtilities.ValuesInArray(initial.NO3))
                    message.AppendLine("No starting NO3 values found.");
                if (!MathUtilities.ValuesInArray(initial.NH4))
                    message.AppendLine("No starting NH4 values found.");
            }

            if (message.Length > 0)
                throw new Exception(message.ToString());
        }
    }
}
