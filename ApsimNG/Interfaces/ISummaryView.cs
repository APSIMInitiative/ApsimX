using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UserInterface.Views
{
    /// <summary>An interface for a summary view.</summary>
    interface ISummaryView
    {
        /// <summary>Occurs when the name of the simulation is changed by the user</summary>
        event EventHandler SimulationNameChanged;

        /// <summary>Gets or sets the currently selected simulation name.</summary>
        string SimulationName { get; set; }

        /// <summary>Gets or sets the simulation names.</summary>
        IEnumerable<string> SimulationNames { get; set; }

        /// <summary>Sets the content of the summary window.</summary>
        /// <param name="content">The html content</param>
        void SetSummaryContent(string content);
    }
}
