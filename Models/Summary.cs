using Models.Core;
using System.IO;

namespace Models
{
    [ViewName("UserInterface.Views.HtmlView")]
    [PresenterName("UserInterface.Presenters.HtmlPresenter")]
    public class Summary : Model, ISummary
    {
        // Links
        [Link] private DataStore DataStore = null;
        [Link] private Simulation Simulation = null;
        [Link] private Clock Clock = null;

        /// <summary>
        /// Write a message to the summary
        /// </summary>
        public void WriteMessage(string Message)
        {
            DataStore.WriteMessage(Simulation.Name, Clock.Today, Message, DataStore.ErrorLevel.Information);
        }

        /// <summary>
        /// Write a property to the summary.
        /// </summary>
        public void WriteProperty(string Name, string Value)
        {
            DataStore.WriteProperty(Simulation.Name, Name, Value);
        }

        /// <summary>
        /// A HTML property that the presenter will use to get a HTML version of the summary.
        /// </summary>
        public string GetHtml(string apsimSummaryImageFileName)
        {
            StringWriter st = new StringWriter();
            DataStore.WriteSummary(st, Simulation.Name, true, apsimSummaryImageFileName);
            return st.ToString();
        }

    }
}
