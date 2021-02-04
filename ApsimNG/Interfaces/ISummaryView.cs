using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UserInterface.EventArguments;

namespace UserInterface.Views
{
    /// <summary>An interface for a summary view.</summary>
    interface ISummaryView
    {
        /// <summary>Summary messages checkbox</summary>
        CheckBoxView SummaryCheckBox { get; }

        /// <summary>Warning messages checkbox</summary>
        CheckBoxView WarningCheckBox { get; }

        /// <summary>Warning messages checkboxsummary>
        CheckBoxView ErrorCheckBox { get; }

        /// <summary>Drop down box which displays the simulation names.</summary>
        DropDownView SimulationDropDown { get; }

        /// <summary>View which displays the summary data.</summary>
        IMarkdownView SummaryDisplay { get; }

        
        /// <summary>
        /// 
        /// </summary>
        bool ShowErrors { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        bool ShowWarnings { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        bool ShowInfo { get; set; }

        bool ShowInitialConditions { get; set; }

        /// <summary>Called when the user changes the filtering options.</summary>
        event EventHandler FiltersChanged;
    }
}
