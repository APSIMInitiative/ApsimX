using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM
{
    ///<summary>
    /// SQLite database reader for access to GRASP data for other models.
    ///</summary>
    ///<remarks>
    ///</remarks>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")] //CLEMFileSQLiteGRASPView
    [PresenterName("UserInterface.Presenters.PropertyPresenter")] //CLEMFileSQLiteGRASPPresenter
    [ValidParent(ParentType = typeof(FileSQLiteGRASP))]
    [Description("This component shuffles rainfall years for reading pasture data as proxy for randomised rainfall")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/DataReaders/RainfallShuffler.htm")]

    public class RainfallShuffler: CLEMModel
    {
        /// <summary>
        /// A link to the clock model.
        /// </summary>
        [Link]
        private readonly Clock clock = null;

        /// <summary>
        /// Month for the start of rainfall/growth season
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("1")]
        [Description("Month for the start of rainfall season")]
        [Required, Month]
        public MonthsOfYear StartSeasonMonth { get; set; }

        /// <summary>
        /// List of shuffled years
        /// </summary>
        [XmlIgnore]
        public List<ShuffleYear> ShuffledYears { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RainfallShuffler()
        {
            this.SetDefaults();
            base.ModelSummaryStyle = HTMLSummaryStyle.Default;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            ShuffledYears = new List<ShuffleYear>();

            // shuffle years for proxy stochastic rainfall simulation
            int startYear = clock.StartDate.Year;
            int endYear = clock.EndDate.Year;

            for (int i = startYear; i <= endYear; i++)
            {
                ShuffledYears.Add(new ShuffleYear() { Year = i, RandomYear = i });
            }
            for (int i = 0; i < ShuffledYears.Count(); i++)
            {
                int randIndex = RandomNumberGenerator.Generator.Next(ShuffledYears.Count());
                int keepValue = ShuffledYears[i].RandomYear;
                ShuffledYears[i].RandomYear = ShuffledYears[randIndex].RandomYear;
                ShuffledYears[randIndex].RandomYear = keepValue;
            }
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activityentry\">";
            html += "\nThe rainfall year starts in ";
            if (StartSeasonMonth == MonthsOfYear.NotSet)
            {
                html += "<span class=\"errorlink\">Not set";
            }
            else
            {
                html += "<span class=\"setvalue\">";
                html += StartSeasonMonth.ToString();
            }
            html += "</class>";
            html += "\n</div>";

            html += "\n<div class=\"activityentry\">";
            html += "\n<div class=\"warningbanner\">WARNING: Rainfall years are being shuffled as a proxy for stochastic rainfall variation in this simulation.<br />This is an advance feature provided for particular projects.</div>";
            html += "\n</div>";
            return html;
        }

    }

    /// <summary>
    /// Shuffled year structure
    /// </summary>
    [Serializable]
    public class ShuffleYear
    {
        /// <summary>
        /// Actual year
        /// </summary>
        public int Year;
        /// <summary>
        /// Shuffled year
        /// </summary>
        public int RandomYear;
    }
}
