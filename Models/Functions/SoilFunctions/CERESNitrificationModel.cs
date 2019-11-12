using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using Models.Soils.Nutrients;
using APSIM.Shared.Utilities;

namespace Models.Functions
{
    /// <summary>Fraction of NH4 which nitrifies today</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval fraction of NH4 nitrified.
    [Serializable]
    [Description("Soil NH4 Nitrification model from CERES-Maize")]
    public class CERESNitrificationModel : Model, IFunction, ICustomDocumentation
    {

        [Link(ByName = true)]
        Solute NH4 = null;

        [Link(Type = LinkType.Child)]
        CERESMineralisationTemperatureFactor CERESTF = null;

        [Link(Type = LinkType.Child)]
        CERESNitrificationWaterFactor CERESWF = null;

        [Link(Type = LinkType.Child)]
        CERESNitrificationpHFactor CERESpHF = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES Nitrification Model");

            double PotentialRate = 40 / (NH4.ppm[arrayIndex] + 90);

            double RateModifier = CERESTF.Value(arrayIndex);
            RateModifier = Math.Min(RateModifier, CERESWF.Value(arrayIndex));
            RateModifier = Math.Min(RateModifier, CERESpHF.Value(arrayIndex));
                       
            return PotentialRate * RateModifier;
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