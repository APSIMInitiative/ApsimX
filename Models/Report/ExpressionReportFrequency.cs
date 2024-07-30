using System;
using Models.Core;
using Models.Functions;
using APSIM.Shared.Utilities;

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
            
            string clean_line = line;
            clean_line = clean_line.Replace("[", "");
            clean_line = clean_line.Replace("]", "");

            IBooleanFunction function = null;
            string errorMessages = "";

            bool compiled = CSharpExpressionFunction.Compile(clean_line, report, compiler, out function, out errorMessages);
            if (compiled)
            {
                new ExpressionReportFrequency(report, events, function);
                return true;
            }
            
            //If the line could not be evaluated as an expression, see if part of it was an event, in case we have an event with a condition

            string[] tokens = StringUtilities.SplitStringHonouringBrackets(line, " ", '[', ']');
            int eventIndex = -1;
            clean_line = "";
            //find which token is an event
            for(int i = 0; i < tokens.Length; i++) {
                if (eventIndex < 0)
                {
                    try
                    {
                        //If we can subscribe to the event, it is valid.
                        events.Subscribe(tokens[i], TestEventHandler);
                        events.Unsubscribe(tokens[i], TestEventHandler);
                        eventIndex = i;
                        clean_line += "true ";
                    }
                    catch {
                        //If this fails, it was not a valid event
                        clean_line += tokens[i] + " ";
                    }
                }
                else 
                {
                    clean_line += tokens[i] + " ";
                }
            }

            //if we didn't find an event, then this is just an invalid expression, return false.
            if (eventIndex < 0)
                return false;
            
            //if we did, recompile our condition with "true" where the event was
            clean_line = clean_line.Trim();
            clean_line = clean_line.Replace("[", "");
            clean_line = clean_line.Replace("]", "");
            compiled = CSharpExpressionFunction.Compile(clean_line, report, compiler, out function, out errorMessages);
            if (compiled)
            {
                new EventReportFrequency(report, events, tokens[eventIndex], function);
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

        /// <summary>An event handler for testing if a token is a valid event</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private static void TestEventHandler(object sender, EventArgs e)
        {
            //do nothing
        }
    }
}
