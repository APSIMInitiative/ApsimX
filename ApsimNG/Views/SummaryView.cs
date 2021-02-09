namespace UserInterface.Views
{
    using Interfaces;
    using Gtk;
    using System;
    using Extensions;
    using System.Collections.Generic;
    using System.Linq;
    using Utility;

    /// <summary>A view for a summary file.</summary>
    public class SummaryView : ViewBase, ISummaryView
    {
        private HBox topBox;
        private HBox middleBox;
        private VBox mainControl;

        /// <summary>Summary messages checkbox</summary>
        public CheckBoxView SummaryCheckBox { get; private set; }

        /// <summary>Warning messages checkbox</summary>
        public CheckBoxView WarningCheckBox { get; private set; }

        /// <summary>Warning messages checkbox</summary>
        public CheckBoxView ErrorCheckBox { get; private set; }

        /// <summary>Drop down box which displays the simulation names.</summary>
        public DropDownView SimulationDropDown { get; private set; }

        /// <summary>View which displays the summary data.</summary>
        public IMarkdownView SummaryDisplay { get; }

        private Button btnJumpToSimLog;

        /// <summary>Initializes a new instance of the <see cref="SummaryView"/> class.</summary>
        public SummaryView(ViewBase owner) : base(owner)
        {
            topBox = new HBox();
            SummaryCheckBox = new CheckBoxView(this);
            SummaryCheckBox.TextOfLabel = "Capture summary?";
            WarningCheckBox = new CheckBoxView(this);
            WarningCheckBox.TextOfLabel = "Capture warning messages?";
            ErrorCheckBox = new CheckBoxView(this);
            ErrorCheckBox.TextOfLabel = "Capture error messages?";
            topBox.PackStart(SummaryCheckBox.MainWidget, false, false, 10);
            topBox.PackStart(WarningCheckBox.MainWidget, false, false, 10);
            topBox.PackStart(ErrorCheckBox.MainWidget, false, false, 10);

            middleBox = new HBox();
            SimulationDropDown = new DropDownView(this);
            middleBox.PackStart(new Label("Simulation:"), false, false, 10);
            middleBox.PackStart(SimulationDropDown.MainWidget, true, true, 10);

            btnJumpToSimLog = new Button("Jump to simulation log");
            HBox buttonContainer = new HBox();
            buttonContainer.PackStart(btnJumpToSimLog, false, false, 0);
            btnJumpToSimLog.Clicked += OnJumpToSimulationLog;

            mainControl = new VBox();
            mainWidget = mainControl;
            mainControl.PackStart(topBox, false, false, 0);
            mainControl.PackStart(middleBox, false, false, 0);
            mainControl.PackStart(buttonContainer, false, false, 0);
            SummaryDisplay = new MarkdownView(this);
            Widget summaryWidget = ((ViewBase)SummaryDisplay).MainWidget as Widget;
            if (summaryWidget is Container container && container.Children != null && container.Children.Length == 1 && container.Children[0] is TextView editor)
            {
                // If possible, we want to generate a summary view which uses a single
                // TextView widget, which will allow for easy selection/searching/copying
                // of the entire text all at once. If we are using a single textview, we
                // want to add it directly to a ScrolledWindow; if we add it to another
                // container (e.g. Box) then add the Box to the container, some of the
                // TextView functionality (scroll_to_iter()) will not work.
                summaryWidget = new ScrolledWindow();
                container.Remove(editor);
                container.Add(editor);
                editor.WrapMode = WrapMode.None;
                (SummaryDisplay as MarkdownView).SetMainWidget(summaryWidget);
            }
            mainControl.PackEnd(summaryWidget, true, true, 0);

            mainWidget.Destroyed += MainWidgetDestroyed;
            mainWidget.ShowAll();
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
                topBox.Cleanup();
                SummaryCheckBox.MainWidget.Cleanup();
                WarningCheckBox.MainWidget.Cleanup();
                ErrorCheckBox.MainWidget.Cleanup();
                middleBox.Cleanup();
                SimulationDropDown.MainWidget.Cleanup();
                mainControl.Cleanup();
                ((ViewBase)SummaryDisplay).MainWidget.Cleanup();
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