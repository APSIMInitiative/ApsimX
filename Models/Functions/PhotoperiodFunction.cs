using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using APSIM.Shared.Utilities;
using Models.Interfaces;

namespace Models.Functions
{
    /// <summary>
    /// Returns the duration of the day, or photoperiod, in hours.  This is calculated using the specified latitude (given in the weather file)
    /// and twilight sun angle threshold.  If a variable called ClimateControl.PhotoPeriod is found in the simulation, it will be used instead.
    /// </summary>
    /// <remarks>The day length is calculated with \ref MathUtilities.DayLength.</remarks>
    /// \pre A \ref Models.WeatherFile function has to exist.
    /// \pre A \ref Models.Clock function has to be existed to retrieve day of year
    /// \param Twilight The interval between sunrise or sunset and the time when the true centre of the sun is below the horizon as a specified angle.
    /// \retval The day length of a specified day and location. Variable "photoperiod" will be returned if simulation environment has a variable called ClimateControl.PhotoPeriod.
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PhotoperiodFunction : Model, IFunction, ICustomDocumentation
    {

        /// <summary>The met data.</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary>The clock.</summary>
        [Link]
        protected Clock Clock = null;

        /// <summary>The twilight angle.</summary>
        [Description("Twilight angle")]
        [Units("degrees")]
        public double Twilight { get; set; }

        /// <summary>The daylight length.</summary>
        [Units("hours")]
        public double DayLength { get; set; }

        /// <summary>Gets the main output of this function.</summary>
        /// <param name="arrayIndex">Not expected for this function.</param>
        /// <returns>The daylight duration (hours).</returns>
        public double Value(int arrayIndex = -1)
        {
            return DayLength;
        }

        [EventSubscribe("DoWeather")]
        private void OnDoWeather(object sender, EventArgs e)
        {
            if (MetData != null)
                DayLength = MetData.CalculateDayLength(Twilight);
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
                // add a heading
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write memos
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // get description of this class
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                tags.Add(new AutoDocumentation.Paragraph("<i>Twilight = " + Twilight.ToString() + " (degrees)</i>", indent));
            }
        }
    }
}
