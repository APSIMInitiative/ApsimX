namespace UserInterface.Views
{
    using Gtk;
    using System;

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
        public HTMLView HtmlView { get; }

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

            mainControl = new VBox();
            mainWidget = mainControl;
            mainControl.PackStart(topBox, false, false, 0);
            mainControl.PackStart(middleBox, false, false, 0);
            HtmlView = new HTMLView(this);
            mainControl.PackEnd(HtmlView.MainWidget, true, true, 0);

            mainWidget.Destroyed += MainWidgetDestroyed;
        }

        /// <summary>Main widget destroyed handler</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWidgetDestroyed(object sender, EventArgs e)
        {
            try
            {
                topBox.Destroy();
                SummaryCheckBox.MainWidget.Destroy();
                WarningCheckBox.MainWidget.Destroy();
                ErrorCheckBox.MainWidget.Destroy();
                middleBox.Destroy();
                SimulationDropDown.MainWidget.Destroy();
                mainControl.Destroy();
                HtmlView.MainWidget.Destroy();
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