// ----------------------------------------------------------------------
// <copyright file="BaseFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using Models.Core;
    using Models.Interfaces;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// # [Name]
    /// Returns the value of today's photoperiod calculated using the specified latitude and twilight sun angle threshold.  If a variable called ClimateControl.PhotoPeriod is found in the simulation, it will be used instead.
    /// </summary>
    /// <remarks>The day length is calculated with \ref MathUtilities.DayLength.</remarks>
    /// \pre A \ref Models.WeatherFile function has to exist.
    /// \pre A \ref Models.Clock function has to be existed to retrieve day of year
    /// \param Twilight The interval between sunrise or sunset and the time when the true centre of the sun is below the horizon as a specified angle.
    /// \retval The day length of a specified day and location. Variable "photoperiod" will be returned if simulation environment has a variable called ClimateControl.PhotoPeriod.
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PhotoperiodFunction : BaseFunction, ICustomDocumentation
    {
        /// <summary>The value being returned</summary>
        private double[] returnValue = new double[1];

        /// <summary>The met data</summary>
        [Link]
        private IWeather weatherData = null;

        /// <summary>The twilight</summary>
        [Description("Twilight angle")]
        [Units("degrees")]
        public double Twilight { get; set; }

        /// <summary>
        /// The value to return
        /// </summary>
        public double DayLength { get; set; }

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            returnValue[0] = DayLength;
            return returnValue;
        }

        [EventSubscribe("DoWeather")]
        private void OnDoWeather(object sender, EventArgs e)
        {
            if (weatherData != null)
                DayLength = weatherData.CalculateDayLength(Twilight);
            else
                DayLength = 0;
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

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, -1, indent);

                // get description of this class.
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                tags.Add(new AutoDocumentation.Paragraph("<i>Twilight = " + Twilight.ToString() + " (degrees)</i>", indent));
            }
        }
    }
}