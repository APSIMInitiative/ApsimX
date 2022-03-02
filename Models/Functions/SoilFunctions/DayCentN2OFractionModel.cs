using System;
using Models.Core;
using Models.Soils.Nutrients;
using APSIM.Shared.Utilities;
using Models.Soils;
using Models.Interfaces;

namespace Models.Functions
{
    /// <summary>Fraction of N denitrified which is N2O</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval Fraction of N denitrified which is N2O.
    [Serializable]
    [Description("Denitrification N2O fraction model from DayCent")]
    public class DayCentN2OFractionModel : Model, IFunction
    {
        [Link]
        IPhysical soilPhysical = null;
        
        /// <summary>The water balance model</summary>
        [Link]
        ISoilWater waterBalance = null;

        [Link(ByName = true)]
        Solute NO3 = null;

        [Link]
        Nutrient Nutrient = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES Nitrification Model");

            double WFPS = MathUtilities.Divide(waterBalance.SW[arrayIndex], soilPhysical.SAT[arrayIndex], 0)*100;
            double WF = 0;
            if (WFPS > 21.3)
                WF = 1.18 * (WFPS - 21.3) / (100 - 21.3);
            double CO2Factor = Math.Max(0.16, Math.Exp(-0.8 * MathUtilities.Divide(NO3.kgha[arrayIndex], Nutrient.Catm[arrayIndex], 0)));
            double N2N2ORatio = Math.Max(0,25.1 * WF * CO2Factor);
            double N2OFraction = 1 / (N2N2ORatio + 1);

            return N2OFraction;
        }
    }
}