using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace Models.CLEM
{
    ///<summary>
    /// Randomises the years of grown provided in pasture reader.
    ///</summary>
    ///<remarks>
    ///</remarks>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")] 
    [PresenterName("UserInterface.Presenters.PropertyPresenter")] 
    [ValidParent(ParentType = typeof(FileSQLitePasture))]
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
        [JsonIgnore]
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

        #region descriptive summary
        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("\r\nThe rainfall year starts in ");
                if (StartSeasonMonth == MonthsOfYear.NotSet)
                {
                    htmlWriter.Write("<span class=\"errorlink\">Not set");
                }
                else
                {
                    htmlWriter.Write("<span class=\"setvalue\">");
                    htmlWriter.Write(StartSeasonMonth.ToString());
                }
                htmlWriter.Write("</span>");
                htmlWriter.Write("\r\n</div>");

                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("\r\n<div class=\"warningbanner\">WARNING: Rainfall years are being shuffled as a proxy for stochastic rainfall variation in this simulation.<br />This is an advance feature provided for particular projects.</div>");
                htmlWriter.Write("\r\n</div>");
                return htmlWriter.ToString(); 
            }
        }

        #endregion
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
