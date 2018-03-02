// ----------------------------------------------------------------------
// <copyright file="WeightedTemperatureFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using Models.Core;
    using Models.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// This Function calculates a mean daily temperature from Max and Min weighted toward Max according to the specified MaximumTemperatureWeighting factor.  This is then passed into the XY matrix as the x property and the function returns the y value
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class WeightedTemperatureFunction : BaseFunction, ICustomDocumentation
    {
        /// <summary>Gets the xy pairs.</summary>
        [ChildLink]
        private XYPairs xys = null;   // Temperature effect on Growth Interpolation Set

        /// <summary>The weather data</summary>
        [Link]
        private IWeather weatherData = null;

        /// <summary>The maximum temperature weighting</summary>
        [Description("MaximumTemperatureWeighting")]
        public double MaximumTemperatureWeighting { get; set; }

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            double tav = MaximumTemperatureWeighting * weatherData.MaxT + (1 - MaximumTemperatureWeighting) * weatherData.MinT;
            Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:" + xys.ValueIndexed(tav));
            return new double[] { xys.ValueIndexed(tav) };
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
                if (xys != null)
                {
                    tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " is calculated as a function of average daily temperature weighted toward max temperature according to the specified MaximumTemperatureWeighting factor.</i>", indent));
                    tags.Add(new AutoDocumentation.Paragraph("<i>MaximumTemperatureWeighting = " + MaximumTemperatureWeighting + "</i>", indent));

                    // write memos.
                    foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                        AutoDocumentation.DocumentModel(memo, tags, -1, indent);

                    tags.Add(new AutoDocumentation.GraphAndTable(xys, string.Empty, "Average temperature (oC)", Name, indent));
                }
            }
        }
    }
}
