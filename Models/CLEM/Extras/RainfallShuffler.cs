using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using APSIM.Core;

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
    [Description("Shuffle rainfall years for reading pasture data as proxy for randomised rainfall")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/DataReaders/RainfallShuffler.htm")]

    public class RainfallShuffler: CLEMModel, IValidatableObject, IStructureDependency
    {
        [Link]
        private IClock clock = null;

        /// <summary>
        /// Month for the start of rainfall/growth season
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Description("Month for the start of rainfall season")]
        [Required, Month]
        public MonthsOfYear StartSeasonMonth { get; set; }

        /// <summary>
        /// The CLEMZone iteration number that will not perform any shuffle. Allows the base (natural) rainfall sequence to be included in experiments
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute(-1)]
        [Description("Iteration number where shuffle is ignored")]
        public int DoNotShuffleIteration { get; set; }

        /// <summary>
        /// List of shuffled years
        /// </summary>
        [JsonIgnore]
        public List<ShuffleYear> ShuffledYears { get; set; }

        /// <summary>
        /// List of shuffled years
        /// </summary>
        [JsonIgnore]
        public DateTime[] ShuffledYearsArray { get; set; }


        /// <summary>
        /// Constructor
        /// </summary>
        public RainfallShuffler()
        {
            this.SetDefaults();
            base.ModelSummaryStyle = HTMLSummaryStyle.Default;
        }

        /// <summary>An event handler to allow us to initialise resources</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            // shuffle years for proxy stochastic rainfall simulation

            // create year month list
            List<(int year, int month, int rndyear, int mthoffset)> storeYears = new List<(int, int, int, int)>();
            DateTime currentDate = new DateTime(clock.StartDate.Year, clock.StartDate.Month, 1);
            int startYear = (currentDate.Month >= (int)StartSeasonMonth)? clock.StartDate.Year - 1: clock.StartDate.Year;
            int currentYear = 0;
            List<int> yearOffset = new List<int>() { 0 };
            List<int> monthOffset = new List<int>();
            while (currentDate <= clock.EndDate)
            {
                if (currentDate.Month == (int)StartSeasonMonth)
                {
                    currentYear++;
                    yearOffset.Add(currentYear);
                }
                storeYears.Add( (currentDate.Year, currentDate.Month, currentYear, currentDate.Year - (startYear + currentYear)));
                currentDate = currentDate.AddMonths(1);
            }

            // shuffle years
            yearOffset = yearOffset.OrderBy(a => RandomNumberGenerator.Generator.NextDouble()).ToList();
            // create shuffled month/date
            ShuffledYears = storeYears.Select(a => new ShuffleYear() { Year = a.year, Month = a.month, RandomYear = startYear + yearOffset[a.rndyear] + a.mthoffset } ).ToList();
            ShuffledYearsArray = ShuffledYears.Select(a => new DateTime(a.RandomYear, a.Month, 1)).ToArray<DateTime>();
        }

        #region validation
        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Structure.Find<RandomNumberGenerator>() is null)
            {
                string[] memberNames = new string[] { "Missing random number generator" };
                results.Add(new ValidationResult($"The [RainfallShiffler] component [{NameWithParent}] requires access to a [RandomNumberGenerator] component in the simulation tree", memberNames));
            }
            return results;
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("\r\nThe rainfall year starts in ");
                if (StartSeasonMonth == MonthsOfYear.NotSet)
                    htmlWriter.Write("<span class=\"errorlink\">Not set");
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
        /// Month
        /// </summary>
        public int Month;
        /// <summary>
        /// Shuffled year
        /// </summary>
        public int RandomYear;
    }
}
