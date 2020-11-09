using System;
using System.Collections.Generic;
using Models.Core;
using APSIM.Shared.Utilities;
using Models.Soils;
using Models.Interfaces;

namespace Models.Functions
{
    /// <summary>Fraction of urea that hydrolyses per day</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval fraction of Urea hydrolysed.
    [Serializable]
    [Description("Urea hydrolysis model from CERES-Maize")]
    public class CERESUreaHydrolysisModel : Model, IFunction, ICustomDocumentation
    {
        [Link]
        Sample initial = null;

        [Link]
        ISoilTemperature soilTemperature = null;

        [Link(Type = LinkType.Child)]
        CERESMineralisationWaterFactor CERESWF = null;

   
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES Urea Hydrolysis Model");

            double potentialRate = -1.12 + 1.31 * initial.OC[arrayIndex] + 0.203 * initial.PH[arrayIndex] - 0.155 * initial.OC[arrayIndex] * initial.PH[arrayIndex];
            potentialRate = MathUtilities.Bound(potentialRate, 0, 1);

            double WF = MathUtilities.Bound(CERESWF.Value(arrayIndex) + 0.2,0,1);
            double TF = MathUtilities.Bound(soilTemperature.Value[arrayIndex] / 40 + 0.2,0,1);
            double rateModifer = Math.Min(WF, TF);

            return potentialRate * rateModifer;
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