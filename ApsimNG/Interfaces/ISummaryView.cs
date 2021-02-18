using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UserInterface.EventArguments;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    /// <summary>An interface for a summary view.</summary>
    interface ISummaryView
    {
        /// <summary>Summary messages checkbox</summary>
        CheckBoxView SummaryCheckBox { get; }

        /// <summary>Warning messages checkbox</summary>
        CheckBoxView WarningCheckBox { get; }

        /// <summary>Warning messages checkbox</summary>
        CheckBoxView ErrorCheckBox { get; }

        /// <summary>Drop down box which displays the simulation names.</summary>
        DropDownView SimulationDropDown { get; }

        /// <summary>View which displays the summary data.</summary>
        IMarkdownView SummaryDisplay { get; }
    }
}
