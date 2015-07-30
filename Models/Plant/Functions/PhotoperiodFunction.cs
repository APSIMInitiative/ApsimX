using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using APSIM.Shared.Utilities;
using Models.Interfaces;

namespace Models.PMF.Functions
{
    /// <summary>Returns the value of today's photoperiod calculated using the specified latitude and twilight sun angle threshold.  If variable called ClimateControl.PhotoPeriod can be found this will be used instead.</summary>
    /// <remarks>The day length is calculated with \ref MathUtilities.DayLength.</remarks>
    /// \pre A \ref Models.WeatherFile function has to exist.
    /// \pre A \ref Models.Clock function has to be existed to retrieve day of year
    /// \param Twilight The interval between sunrise or sunset and the time when the true centre of the sun is below the horizon as a specified angle.
    /// \retval The day length of a specified day and location. Variable "photoperiod" will be returned if simulation environment has a variable called ClimateControl.PhotoPeriod.
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PhotoperiodFunction : Model, IFunction
    {

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary>The clock</summary>
        [Link]
        protected Clock Clock = null;

        /// <summary>The twilight</summary>
        [Description("Twilight")]
        [Units("degrees")]
        public double Twilight { get; set; }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                object val = Apsim.Get(this, "ClimateControl.PhotoPeriod");
                //If simulation environment has a variable called ClimateControl.PhotoPeriod will use that other wise calculate from day and location
                if (val != null)  //FIXME.  If climatecontrol does not contain a variable called photoperiod it still returns a value of zero.
                {
                    double CCPP = Convert.ToDouble(val);
                    if (CCPP > 0.0)
                        return Convert.ToDouble(val);
                    else
                        return MathUtilities.DayLength(Clock.Today.DayOfYear, Twilight, MetData.Latitude);
                }
                else
                    return MathUtilities.DayLength(Clock.Today.DayOfYear, Twilight, MetData.Latitude);
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

            // write memos.
            foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                memo.Document(tags, -1, indent);

            // get description of this class.
            AutoDocumentation.GetClassDescription(this, tags, indent);

            tags.Add(new AutoDocumentation.Paragraph("<i>Twilight = " + Twilight.ToString() + " (degrees)</i>", indent));
        }

    }
}