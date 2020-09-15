using System;
using System.Collections.Generic;
using Models.Core;
using APSIM.Shared.Utilities;
using Models.Soils;

namespace Models.Functions
{
    /// <summary>pH factor for daily nitrification of ammonium</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval pH factor for daily nitrification of ammonium
    [Serializable]
    [Description("Nitrification Water Factor from CERES-Maize")]
    public class CERESNitrificationpHFactor : Model, IFunction, ICustomDocumentation
    {
        [Link]
        Sample initial = null;


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES mineralisation water factor Model");
            double pHF = 0;

            if (initial.PH[arrayIndex] < 4.5)
                pHF = 0;
            else if (initial.PH[arrayIndex] < 6)
                pHF = MathUtilities.Divide(initial.PH[arrayIndex] - 4.5, 6.0 - 4.5, 0);
            else if (initial.PH[arrayIndex] < 8)
                pHF = 1;
            else if (initial.PH[arrayIndex] < 9)
                pHF = 1 - MathUtilities.Divide(initial.PH[arrayIndex] - 8.0, 9.0 - 8.0, 0.0);
            else
                pHF = 0;

            return pHF;
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