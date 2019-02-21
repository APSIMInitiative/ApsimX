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
    /// <summary>C:N Ratio factor for daily FOM Mineralisation</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval C:N Ratio factor for daily FOM Mineralisation
    [Serializable]
    [Description("C:N Ratio factor for daily FOM Mineralisation from CERES-Maize")]
    public class CERESMineralisationFOMCNRFactor : Model, IFunction, ICustomDocumentation
    {

        [Link]
        Nutrient nutrient = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES mineralisation water factor Model");

            double CNRF = Math.Exp(-0.693 * (nutrient.FOMCNR[arrayIndex] - 25) / 25);
            return MathUtilities.Bound(CNRF, 0, 1);
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