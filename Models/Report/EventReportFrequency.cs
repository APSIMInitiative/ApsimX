using System;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models
{
    class EventReportFrequency
    {
        private readonly Report report;
        private readonly IEvent events;
        private readonly string eventName;

        /// <summary>
        /// Try and parse a frequency line and return an instance of a IReportFrequency.
        /// </summary>
        /// <param name="line">The line to parse.</param>
        /// <param name="report">An instance of a report model.</param>
        /// <param name="events">An instance of an events publish/subcribe interface.</param>
        /// <returns>true if line was able to be parsed.</returns>
        public static bool TryParse(string line, Report report, IEvent events)
        {
            string[] tokens = StringUtilities.SplitStringHonouringBrackets(line, " ", '[', ']');
            if (tokens.Length == 1)
            {
                new EventReportFrequency(report, events, tokens[0]);
                return true;
            }
            else 
                if (tokens.Length > 1 && line.IndexOfAny(new char[] { '=', '<', '>', '&', '|' }) == 0)
            {
                new EventReportFrequency(report, events, line);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Private constructor.
        /// </summary>
        /// <param name="report">An instance of the report model.</param>
        /// <param name="events">An instance of an event publish/subscribe engine.</param>
        /// <param name="eventName">The name of the event to subscribe to.</param>
        private EventReportFrequency(Report report, IEvent events, string eventName)
        {
            this.report = report;
            this.events = events;
            this.eventName = eventName;
            events.Subscribe(eventName, OnEvent);
        }

        /// <summary>Called when the event is published.</summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event data.</param>
        private void OnEvent(object sender, EventArgs e)
        {
            report.DoOutput();
        }
    }
}
