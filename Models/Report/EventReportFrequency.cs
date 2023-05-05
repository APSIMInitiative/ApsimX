using APSIM.Shared.Utilities;
using Models.Core;
using System;
using System.Linq.Expressions;

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
            } 
            else if (tokens.Length > 1) 
            {
                try
                {
                    new EventReportFrequency(report, events, tokens[0]);
                }
                catch
                {   //if trying with only first token failed, try again with entire line
                    try
                    {
                        new EventReportFrequency(report, events, line);
                    }
                    catch
                    {
                        return false;
                    }
                }
            } 
            else
            {
                return false;
            }

            return true;
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
