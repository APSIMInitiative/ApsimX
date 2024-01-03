using Models.Core;

namespace UserInterface.Views
{
    /// <summary>An interface for a summary view.</summary>
    interface ISummaryView
    {
        /// <summary>
        /// Controls which types of messages are captured by the summary.
        /// </summary>
        EnumDropDownView<MessageType> VerbosityDropDown { get; }

        /// <summary>Drop down box which displays the simulation names.</summary>
        DropDownView SimulationDropDown { get; }

        /// <summary>View which displays the summary data.</summary>
        IMarkdownView SummaryDisplay { get; }

    }
}
