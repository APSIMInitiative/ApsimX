using System;
using Models.Core;
using Models.Functions;

namespace Models
{
    class ExpressionReportFrequency
    {
        private readonly Report report;
        private readonly IEvent events;
        private readonly IBooleanFunction expressionFunction;

        /// <summary>
        /// Try and parse a frequency line and return an instance of a IReportFrequency.
        /// </summary>
        /// <param name="line">The line to parse.</param>
        /// <param name="report">An instance of a report model.</param>
        /// <param name="events">An instance of an events publish/subcribe interface.</param>
        /// <param name="compiler">An instance of a c# compiler.</param>
        /// <returns>true if line was able to be parsed.</returns>
        public static bool TryParse(string line, Report report, IEvent events, ScriptCompiler compiler)
        {
            line = line.Replace("[", "")
                       .Replace("]", "");

            if (CSharpExpressionFunction.Compile(line, report, compiler, out IBooleanFunction function, out string errorMessages))
            {
                new ExpressionReportFrequency(report, events, function);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Private constructor.
        /// </summary>
        /// <param name="report">An instance of the report model.</param>
        /// <param name="events">An instance of an event publish/subscribe engine.</param>
        /// <param name="expressionFunction">The expression to evaluate</param>
        private ExpressionReportFrequency(Report report, IEvent events, IBooleanFunction expressionFunction)
        {
            this.report = report;
            this.events = events;
            this.expressionFunction = expressionFunction;
            events.Subscribe("[Clock].DoReport", OnDoReport);
        }

        /// <summary>An event handler called at the end of each day.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnDoReport(object sender, EventArgs e)
        {
            if (expressionFunction.Value())
                report.DoOutput();
        }
    }
}
