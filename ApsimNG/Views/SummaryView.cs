using System;
using System.Linq;
using Gtk;
using Utility;
using MessageType = Models.Core.MessageType;

namespace UserInterface.Views
{

    /// <summary>A view for a summary file.</summary>
    public class SummaryView : ViewBase, ISummaryView
    {
        private Widget captureRules;
        private Widget simulationFilter;
        private Box mainControl;
        private Box settingsControl;

        /// <summary>Drop down box which displays the simulation names.</summary>
        public DropDownView SimulationDropDown { get; private set; }

        /// <summary>View which displays the summary data.</summary>
        public IMarkdownView SummaryDisplay { get; }

        private Button btnJumpToSimLog;

        public EnumDropDownView<Models.Core.MessageType> VerbosityDropDown { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="SummaryView"/> class.</summary>
        public SummaryView(ViewBase owner) : base(owner)
        {
            captureRules = CreateCaptureRules();
            simulationFilter = CreateSimulationFilter();

            Widget jumpToLogContainer = CreateJumpToLogContainer();

            mainControl = new Box(Orientation.Vertical, 0);
            settingsControl = new Box(Orientation.Horizontal, 0);
            mainWidget = mainControl;
            settingsControl.PackStart(captureRules, false, false, 0);
            settingsControl.PackStart(simulationFilter, false, false, 0);
            SummaryDisplay = new MarkdownView(this);
            ScrolledWindow scroller = new ScrolledWindow();
            scroller.Add(((ViewBase)SummaryDisplay).MainWidget);
            mainControl.PackStart(settingsControl, false, false, 0);
            mainControl.PackStart(jumpToLogContainer, false, false, 0);
            mainControl.PackEnd(scroller, true, true, 0);
            VerbosityDropDown.Changed += OnVerbosityDropDownChange;
            mainWidget.Destroyed += MainWidgetDestroyed;
            mainWidget.ShowAll();
        }

        private Widget CreateJumpToLogContainer()
        {
            btnJumpToSimLog = new Button("Jump to simulation log");
            btnJumpToSimLog.Clicked += OnJumpToSimulationLog;
            Box box = new Box(Orientation.Horizontal, 0);
            box.PackStart(btnJumpToSimLog, false, false, 0);
            box.Margin = 5;
            return box;
        }

        private Widget CreateSimulationFilter()
        {
            Box box = new Box(Orientation.Vertical, 0);
            Box hBox = new Box(Orientation.Horizontal, 0);
            SimulationDropDown = new DropDownView(this);
            hBox.PackStart(new Label("Simulation:"), false, false, 5);
            hBox.PackStart(SimulationDropDown.MainWidget, false, false, 5);
            box.PackStart(hBox, false, false, 5);
            box.MarginBottom = 5;
            Frame frame = new Frame("Simulation Filter");
            frame.Add(box);
            frame.Margin = 5;
            return frame;
        }

        private Widget CreateCaptureRules()
        {
            VerbosityDropDown = new EnumDropDownView<MessageType>(this);
            Label verbosity = new Label("Messages which should be saved when the simulation is run:");
            Box box = new Box(Orientation.Vertical, 0);
            box.PackStart(verbosity, false, false, 5);
            box.PackStart(VerbosityDropDown.MainWidget, false, false, 5);
            box.Margin = 5;
            Frame frame = new Frame("Capture Rules");
            frame.Add(box);
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

        private void OnVerbosityDropDownChange(object sender, EventArgs args)
        {
            try
            {
                if (VerbosityDropDown.SelectedEnumValue >= MessageType.Information)
                    btnJumpToSimLog.Visible = true;
                else btnJumpToSimLog.Visible = false;
            }
            catch (Exception err)
            {
                ShowError(err);
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
                VerbosityDropDown.Dispose();
                simulationFilter.Dispose();
                SimulationDropDown.Dispose();
                mainControl.Dispose();
                ((ViewBase)SummaryDisplay).Dispose();
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