using System;
using System.Collections.Generic;
using Models.Core;
using Models.Soils.Nutrients;

namespace Models.Functions
{
    /// <summary>Fraction of NO3 which denitrifies today</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval fraction of NO3 denitrified.
    [Serializable]
    [Description("Soil NO3 Denitrification model from CERES-Maize")]
    public class CERESDenitrificationModel : Model, IFunction, ICustomDocumentation
    {
        [Link]
        Soils.IPhysical soilPhysical = null;
        [Link(ByName = true)]
        INutrientPool Humic = null;
        [Link(ByName = true)]
        INutrientPool Inert = null;
        [Link(ByName = true)]
        INutrientPool FOMCarbohydrate = null;
        [Link(ByName = true)]
        INutrientPool FOMCellulose = null;
        [Link(ByName = true)]
        INutrientPool FOMLignin = null;


        [Link(Type = LinkType.Child)]
        CERESDenitrificationTemperatureFactor CERESTF = null;

        [Link(Type = LinkType.Child)]
        CERESDenitrificationWaterFactor CERESWF = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES Denitrification Model");

            double ActiveC = Humic.C[arrayIndex] + Inert.C[arrayIndex]+FOMCarbohydrate.C[arrayIndex]+FOMCellulose.C[arrayIndex]+FOMLignin.C[arrayIndex];
            double ActiveCppm = ActiveC/(soilPhysical.BD[arrayIndex] * soilPhysical.Thickness[arrayIndex] / 100);
            double CarbonModifier = 0.0031 * ActiveCppm + 24.5;
            double PotentialRate = 0.0006 * CarbonModifier;
             
            return PotentialRate * CERESTF.Value(arrayIndex) * CERESWF.Value(arrayIndex);
        }

        /// <summary>
        /// Get the values for all soil layers.
        /// </summary>
        public double[] Values
        {
            get
            {
                if (soilPhysical == null)
                    return null;
                double[] result = new double[soilPhysical.Thickness.Length];
                for (int i = 0; i < result.Length; i++)
                    result[i] = Value(i);
                return result;
            }
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