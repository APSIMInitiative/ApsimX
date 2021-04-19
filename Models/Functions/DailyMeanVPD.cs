using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.Functions
{
    /// <summary>
    /// This Function calculates a mean daily VPD from Max and Min weighted toward Max according to the specified MaximumVPDWeight factor.  
    /// This is then passed into the XY matrix as the x property and the function returns the y value
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class DailyMeanVPD : Model, IFunction, ICustomDocumentation
    {
        #region Class Data Members

        /// <summary>The maximum temperature weighting</summary>
        [Description("The weight of 'VPD at daily maximum temperature' in daily mean VPD")]
        public double MaximumVPDWeight { get; set; } = 0.66;

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;
        
        #endregion

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            double VPDmint = MetUtilities.svp((float)MetData.MinT) - MetData.VP;
            VPDmint = Math.Max(VPDmint, 0.0);

            double VPDmaxt = MetUtilities.svp((float)MetData.MaxT) - MetData.VP;
            VPDmaxt = Math.Max(VPDmaxt, 0.0);

            return MaximumVPDWeight * VPDmaxt + (1 - MaximumVPDWeight) * VPDmint;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // add graph and table.
                tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " is calculated as a function of daily min and max temperatures, these are weighted toward VPD at max temperature according to the specified MaximumVPDWeight factor.  A value equal to 1.0 means it will use VPD at max temperature, a value of 0.5 means average VPD.</i>", indent));
                tags.Add(new AutoDocumentation.Paragraph("<i>MaximumVPDWeight = " + MaximumVPDWeight + "</i>", indent));

                // write memos.
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);
            }
        }
    }
}
