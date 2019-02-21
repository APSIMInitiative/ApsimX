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
    /// <summary>Temperature effect on denitrification</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval Temperature effect on denitrification.
    [Serializable]
    [Description("Soil NO3 Denitrification temperature factor from CERES-Maize")]
    public class CERESDenitrificationTemperatureFactor : Model, IFunction, ICustomDocumentation
    {

        [Link]
        Soil soil = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES Denitrification Temperature Factor");

            return MathUtilities.Bound(0.1 * Math.Exp(0.046 * soil.Temperature[arrayIndex]), 0, 1);
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