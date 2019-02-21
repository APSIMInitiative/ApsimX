using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using Models.Soils.Nutrients;
using APSIM.Shared.Utilities;
using Models.Soils;

namespace Models.Functions
{
    /// <summary>Water effect on denitrification</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval Water effect on denitrification.
    [Serializable]
    [Description("Soil NO3 Denitrification water factor from CERES-Maize")]
    public class CERESDenitrificationWaterFactor : Model, IFunction, ICustomDocumentation
    {

        [Link]
        Soil soil = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES Nitrification Model");

            double WF = MathUtilities.Divide(soil.SoilWater.SW[arrayIndex] - soil.DUL[arrayIndex], soil.SAT[arrayIndex] - soil.DUL[arrayIndex], 0.0);     
            return MathUtilities.Bound(WF, 0, 1); 
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {

        }
    }
}