using System;
using System.Collections.Generic;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions
{
    /// <summary>Fraction of NH4 which nitrifies today</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval fraction of NH4 nitrified.
    [Serializable]
    [Description("Mineralisation Temperature Factor from CERES-Maize")]
    public class CERESMineralisationTemperatureFactor : Model, IFunction, ICustomDocumentation
    {

        [Link]
        ISoilTemperature soiltemperature = null;

   
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES mineralisation temperature factor Model");

            double TF = 0;
            double ST = soiltemperature.Value[arrayIndex];

            if (ST > 0)
                TF = (ST * ST) / (32 * 32);
            if (TF > 1) TF = 1;

            return TF;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {

            // add a heading.
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));


            // write memos.
            foreach (IModel memo in this.FindAllChildren<Memo>())
                AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);


        }
    }
}