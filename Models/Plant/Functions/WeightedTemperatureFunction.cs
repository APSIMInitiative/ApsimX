using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.Interfaces;

namespace Models.PMF.Functions
{
    /// <summary>
    /// This Function calculates a mean daily temperature from Max and Min weighted toward Max according to the specified MaximumTemperatureWeighting factor.  This is then passed into the XY matrix as the x property and the function returns the y value
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class WeightedTemperatureFunction : Model, IFunction
    {
        #region Class Data Members
        /// <summary>Gets the xy pairs.</summary>
        /// <value>The xy pairs.</value>
        [Link]
        private XYPairs XYPairs = null;   // Temperature effect on Growth Interpolation Set

        /// <summary>The maximum temperature weighting</summary>
        [Description("MaximumTemperatureWeighting")]
        public double MaximumTemperatureWeighting { get; set; }

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;
        
        #endregion

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        [Units("0-1")]
        public double Value
        {
            get
            {
                double Tav = MaximumTemperatureWeighting * MetData.MaxT + (1 - MaximumTemperatureWeighting) * MetData.MinT;
                return XYPairs.ValueIndexed(Tav);
            }
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // add a heading.
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            // add graph and table.
            if (XYPairs != null)
            {
                tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " is calculated as a function of average daily temperature weighted toward max temperature according to the specified MaximumTemperatureWeighting factor.</i>", indent));
                tags.Add(new AutoDocumentation.Paragraph("<i>" + MaximumTemperatureWeighting + " = " + MaximumTemperatureWeighting + "</i>", indent));

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    memo.Document(tags, -1, indent);

                tags.Add(new AutoDocumentation.GraphAndTable(XYPairs, string.Empty, "Average temperature (oC)", Name, indent));
            }
        }
    }
}
