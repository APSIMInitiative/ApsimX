using System;
using APSIM.Services.Documentation;
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
    public class CERESMineralisationTemperatureFactor : Model, IFunction
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

        /// <summary>
        /// Document the model.
        /// </summary>
        /// <param name="indent">Indentation level.</param>
        /// <param name="headingLevel">Heading level.</param>
        protected override IEnumerable<ITag> Document(int indent, int headingLevel)
        {

            // add a heading.
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));


            // write memos.
            foreach (IModel memo in this.FindAllChildren<Memo>())
                AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);


        }
    }
}