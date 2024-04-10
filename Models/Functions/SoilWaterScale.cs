using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Organs;
using Models.Soils;

namespace Models.Functions
{
    /// <summary>
    /// A simple scale to convert soil water content into a value between 0 and 2 where 0 = LL15, 1 = DUL and 2 = SAT
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("A simple scale to convert soil water content into a value between 0 and 2 where 0 = LL/LL15, 1 = DUL and 2 = SAT")]
    public class SoilWaterScale : Model, IFunction
    {

        /// <summary> </summary>
        [Description("SAT array internal variable: ")]
        [Display(Type = DisplayType.None)]
        public string SATModel { get; set; } = "[Physical].SAT";

        /// <summary> </summary>
        [Description("DUL array internal variable: ")]
        [Display(Type = DisplayType.None)]
        public string DULModel { get; set; } = "[Physical].DUL";

        /// <summary> </summary>
        [Description("Lower Limit array internal variable: ")]
        [Display(Type = DisplayType.None)]
        public string LLModel { get; set; } = "[Physical].LL15";

        [Link]
        private ISoilWater soilwater = null;

        private double[] sats = null;
        private double[] duls = null;
        private double[] lls = null;

        /// <summary>Gets the value of the function.</summary>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception($"Soil Water Scale ({Parent.Name}.{Name}) requires an array index for soil layer");

            if (arrayIndex >= soilwater.SW.Length)
                throw new Exception($"Soil Water Scale ({Parent.Name}.{Name}): ArrayIndex {arrayIndex} is more than SoilWater SW length {soilwater.SW.Length}.");

            if (sats == null && duls == null && lls == null) 
            {
                sats = Locator.Get(SATModel) as double[];
                duls = Locator.Get(DULModel) as double[];
                lls = Locator.Get(LLModel) as double[];
            }

            if (arrayIndex >= sats.Length)
                throw new Exception($"Soil Water Scale ({Parent.Name}.{Name}): ArrayIndex {arrayIndex} is invalid index for SAT array length {sats.Length}.");

            if (arrayIndex >= duls.Length)
                throw new Exception($"Soil Water Scale ({Parent.Name}.{Name}): ArrayIndex {arrayIndex} is invalid index for DUL array length {duls.Length}.");

            if (arrayIndex >= lls.Length)
                throw new Exception($"Soil Water Scale ({Parent.Name}.{Name}): ArrayIndex {arrayIndex} is invalid index for LL array length {lls.Length}.");

            double sw = soilwater.SW[arrayIndex];
            double sat = sats[arrayIndex];
            double dul = duls[arrayIndex];
            double ll = lls[arrayIndex];

            double sws;
            if (sw >= sat)               // saturated - 2
                sws = 2;
            else if (sw >= dul)          // draining - 1 to 2
                sws = 1.0 + MathUtilities.Divide(sw - dul, sat - dul, 0.0);
            else if (sw > ll)            // unsaturated - 0 to 1
                sws = MathUtilities.Divide(sw - ll, dul - ll, 0.0);
            else                         // dry  - 0
                sws = 0.0;
            return sws;
        }
    }
}