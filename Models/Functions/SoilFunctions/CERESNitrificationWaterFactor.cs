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
    /// <summary>Water factor for daily nitrification of ammonium</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval Water factor for daily nitrification of ammonium
    [Serializable]
    [Description("Nitrification Water Factor from CERES-Maize")]
    public class CERESNitrificationWaterFactor : Model, IFunction, ICustomDocumentation
    {

        [Link]
        Soil soil = null;

   
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES mineralisation water factor Model");
            double WF = 0;

            if (soil.SoilWater.SW[arrayIndex] < soil.LL15[arrayIndex])
                WF = 0;
            else if (soil.SoilWater.SW[arrayIndex] < soil.DUL[arrayIndex])
                WF = Math.Min(1, 4 * MathUtilities.Divide(soil.SoilWater.SW[arrayIndex] - soil.LL15[arrayIndex],soil.DUL[arrayIndex] - soil.LL15[arrayIndex],0.0));
            else
                WF = 1 - MathUtilities.Divide(soil.SoilWater.SW[arrayIndex] - soil.DUL[arrayIndex], soil.SAT[arrayIndex] - soil.DUL[arrayIndex],0.0);

            return WF;
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
            foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);


        }
    }
}