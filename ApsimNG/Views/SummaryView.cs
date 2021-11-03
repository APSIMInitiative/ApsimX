namespace UserInterface.Views
{
    using Interfaces;
    using Gtk;
    using System;
    using Extensions;
    using System.Collections.Generic;
    using System.Linq;
    using Utility;
    using MessageType = Models.Core.MessageType;

    /// <summary>A view for a summary file.</summary>
    public class SummaryView : ViewBase, ISummaryView
    {
        private Widget captureRules;
        private Widget simulationFilter;
        private VBox mainControl;

        /// <summary>Drop down box which displays the simulation names.</summary>
        public DropDownView SimulationDropDown { get; private set; }

        /// <summary>View which displays the summary data.</summary>
        public IMarkdownView SummaryDisplay { get; }

        private Button btnJumpToSimLog;

        /// <summary>
        /// An expander widget which holds all of the widgets
        /// controlling summary message filtering.
        /// </summary>
        private Widget messageFilters;

        // /// <summary>
        // /// An expander widget which holds all of the widgets controlling
        // /// summary message sorting.
        // /// </summary>
        // private Widget messageSorting;

        private CheckButton chkShowInitialConditions;

        public bool ShowInitialConditions { get => chkShowInitialConditions.Active; set => chkShowInitialConditions.Active = value; }

        public EnumDropDownView<Models.Core.MessageType> VerbosityDropDown { get; private set; }

        /// <summary>
        /// Allows the user to select which message types to view.
        /// </summary>
        public EnumDropDownView<Models.Core.MessageType> MessagesFilter { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="SummaryView"/> class.</summary>
        public SummaryView(ViewBase owner) : base(owner)
        {
            captureRules = CreateCaptureRules();
            simulationFilter = CreateSimulationFilter();

            Widget jumpToLogContainer = CreateJumpToLogContainer();
            messageFilters = CreateFilteringWidgets();
            // messageSorting = CreateSortingWidgets();


            mainControl = new VBox();
            mainWidget = mainControl;
            mainControl.PackStart(captureRules, false, false, 0);
            mainControl.PackStart(messageFilters, false, false, 0);
            // mainControl.PackStart(messageSorting, false, false, 0);
            mainControl.PackStart(simulationFilter, false, false, 0);
            mainControl.PackStart(jumpToLogContainer, false, false, 0);
            SummaryDisplay = new MarkdownView(this);
            ScrolledWindow scroller = new ScrolledWindow();
            scroller.Add(((ViewBase)SummaryDisplay).MainWidget);
            mainControl.PackEnd(scroller, true, true, 0);

            mainWidget.Destroyed += MainWidgetDestroyed;
            mainWidget.ShowAll();
        }

        private Widget CreateJumpToLogContainer()
        {
            btnJumpToSimLog = new Button("Jump to simulation log");
            btnJumpToSimLog.Clicked += OnJumpToSimulationLog;
            HBox box = new HBox();
            box.PackStart(btnJumpToSimLog, false, false, 0);
            box.Margin = 5;
            return box;
        }

        private Widget CreateSimulationFilter()
        {
            HBox box = new HBox();
            SimulationDropDown = new DropDownView(this);
            box.PackStart(new Label("Simulation:"), false, false, 10);
            box.PackStart(SimulationDropDown.MainWidget, true, true, 10);
            box.MarginBottom = 5;
            Frame frame = new Frame("Simulation Filter");
            frame.Add(box);
            frame.Margin = 5;
            return frame;
        }

        private Widget CreateCaptureRules()
        {
            VerbosityDropDown = new EnumDropDownView<MessageType>(this);
            HBox box = new HBox();
            box.PackStart(VerbosityDropDown.MainWidget, false, false, 10);
            Frame frame = new Frame("Capture Rules");
            frame.Add(box);
            frame.Margin = 5;
            return frame;
        }

        private Widget CreateSortingWidgets()
        {
            Expander container = new Expander("Sorting");
            
            container.Margin = 5;
            return container;
        }

        private Widget CreateFilteringWidgets()
        {
            chkShowInitialConditions = new CheckButton("Initial Conditions");

            MessagesFilter = new EnumDropDownView<MessageType>(this);

            Box filtersBox = new VBox();
            filtersBox.PackStart(chkShowInitialConditions, false, false, 0);
            filtersBox.PackStart(MessagesFilter.MainWidget, false, false, 0);

            Frame frame = new Frame("Message Filters");
            frame.Add(filtersBox);
            frame.Margin = 5;
            return frame;
        }

        private void OnJumpToSimulationLog(object sender, EventArgs e)
        {
            try
            {
                TextView target = mainWidget.Descendants().OfType<TextView>().FirstOrDefault(l => l.Buffer.Text.Contains("Simulation log"));
                if (target != null)
                {
                    TextIter iter = target.Buffer.GetIterAtOffset(target.Buffer.Text.IndexOf("Simulation log", StringComparison.CurrentCultureIgnoreCase));
                    target.ScrollToIter(iter, 0, true, 0, 0);
                }
            }
            catch (Exception error)
            {
                ShowError(error);
            }
        }

        /// <summary>Main widget destroyed handler</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWidgetDestroyed(object sender, EventArgs e)
        {
            try
            {
                btnJumpToSimLog.Clicked -= OnJumpToSimulationLog;
                captureRules.Dispose();
                VerbosityDropDown.MainWidget.Dispose();
                MessagesFilter.MainWidget.Dispose();
                simulationFilter.Dispose();
                SimulationDropDown.MainWidget.Dispose();
                mainControl.Dispose();
                ((ViewBase)SummaryDisplay).MainWidget.Dispose();
                mainWidget.Destroyed -= MainWidgetDestroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}