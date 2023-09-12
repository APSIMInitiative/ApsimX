using System;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models
{
    class DateReportFrequency
    {
        private readonly Report report;
        private readonly IEvent events;
        private readonly string dateString;

        /// <summary>
        /// Try and parse a frequency line and return an instance of a IReportFrequency.
        /// </summary>
        /// <param name="line">The line to parse.</param>
        /// <param name="report">An instance of a report model.</param>
        /// <param name="events">An instance of an events publish/subcribe interface.</param>
        /// <returns>true if line was able to be parsed.</returns>
        public static bool TryParse(string line, Report report, IEvent events)
        {
            if (DateUtilities.ValidateDateString(line) != null)
            {
                new DateReportFrequency(report, events, line);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Private constructor.
        /// </summary>
        /// <param name="report">An instance of the report model.</param>
        /// <param name="events">An instance of an event publish/subscribe engine.</param>
        /// <param name="dateString">A string representation of a date.</param>
        private DateReportFrequency(Report report, IEvent events, string dateString)
        {
            this.report = report;
            this.events = events;
            this.dateString = dateString;
            events.Subscribe("[Clock].DoReport", OnDoReport);
        }

        /// <summary>An event handler called at the end of each day.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnDoReport(object sender, EventArgs e)
        {
            IClock clock = sender as Clock;
            if (DateUtilities.DatesAreEqual(dateString, clock.Today))
                report.DoOutput();
        }
    }
}
