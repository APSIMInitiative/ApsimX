using Models.Core;
using System.IO;
using System.Diagnostics;
using System.Reflection;

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
        public void WriteMessage(string FullPath, string Message)
        {
            DataStore.WriteMessage(FullPath, Simulation.Name, Clock.Today, Message, DataStore.ErrorLevel.Information);
        }

        /// <summary>
        /// Write a warning message to the summary
        /// </summary>
        public void WriteWarning(string FullPath, string Message)
        {
            DataStore.WriteMessage(FullPath, Simulation.Name, Clock.Today, Message, DataStore.ErrorLevel.Warning);
        }

        /// <summary>
        /// Write an error message to the summary
        /// </summary>
        public void WriteError(string Message)
        {
            DataStore.WriteMessage(FullPath, Simulation.Name, Clock.Today, Message, DataStore.ErrorLevel.Error);
        }

        /// <summary>
        /// Write a property to the summary.
        /// </summary>
        public void WriteProperty(string Name, string Value)
        {
            DataStore.WriteProperty(Simulation.Name, Name, Value);
        }

        /// <summary>
        /// A property that the presenter will use to get the summary.
        /// </summary>
        public string GetSummary(string apsimSummaryImageFileName, bool html)
        {
            StringWriter st = new StringWriter();
            DataStore.WriteSummary(st, Simulation.Name, html, apsimSummaryImageFileName);
            return st.ToString();
        }

    }
}
