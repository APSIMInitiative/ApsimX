 using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Models.PMF;

namespace Models.Functions
{
    /// <summary>
    /// [DocumentMathFunction /]
    /// </summary>
    [Serializable]
    [Description("Calculate LightSenescence")]
    public class LightSenescenceFunction : Model, IFunction
    {
        [Link(Type = LinkType.Ancestor)]
        private SorghumLeaf Leaf = null;

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        virtual protected void OnPlantSowing(object sender, SowingParameters data)
        {
            totalLaiEqlbLight = 0;
            avgLaiEquilibLight = 0;
            laiEqlbLightTodayQ = new Queue<double>();

        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            totalLaiEqlbLight = 0;
            avgLaiEquilibLight = 0;
            laiEqlbLightTodayQ.Clear();
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            double critTransmission = MathUtilities.Divide(Leaf.SenRadnCrit, Leaf.metData.Radn, 1);
            /* TODO : Direct translation - needs cleanup */
            //            ! needs rework for row spacing
            double laiEqlbLightToday;
            if (critTransmission > 0.0)
            {
                laiEqlbLightToday = -Math.Log(critTransmission) / Leaf.extinctionCoefficientFunction.Value();
            }
            else
            {
                laiEqlbLightToday = Leaf.LAI;
            }
            // average of the last 10 days of laiEquilibLight
            avgLaiEquilibLight = UpdateAvLaiEquilibLight(laiEqlbLightToday, 10);//senLightTimeConst?

            // dh - In old apsim, we had another variable frIntcRadn which is always set to 0.
            // Set Plant::radnInt(void) in Plant.cpp.
            double radnInt = Leaf.metData.Radn * Leaf.CoverGreen;
            double radnTransmitted = Leaf.metData.Radn - radnInt;
            double dltSlaiLight = 0.0;
            if (radnTransmitted < Leaf.SenRadnCrit)
                dltSlaiLight = Math.Max(0.0, MathUtilities.Divide(Leaf.LAI - avgLaiEquilibLight, Leaf.SenLightTimeConst, 0.0));
            dltSlaiLight = Math.Min(dltSlaiLight, Leaf.LAI);
            return dltSlaiLight;
        }

        private double totalLaiEqlbLight;
        private double avgLaiEquilibLight;
        private Queue<double> laiEqlbLightTodayQ;
        private double UpdateAvLaiEquilibLight(double laiEqlbLightToday, int days)
        {
            totalLaiEqlbLight += laiEqlbLightToday;
            laiEqlbLightTodayQ.Enqueue(laiEqlbLightToday);
            if (laiEqlbLightTodayQ.Count > days)
            {
                totalLaiEqlbLight -= laiEqlbLightTodayQ.Dequeue();
            }
            return MathUtilities.Divide(totalLaiEqlbLight, laiEqlbLightTodayQ.Count, 0);
        }
    }

}
 
 