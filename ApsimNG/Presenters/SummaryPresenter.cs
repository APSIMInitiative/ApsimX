namespace UserInterface.Presenters
{
    using EventArguments;
    using System;
    using System.IO;
    using System.Linq;
    using Models;
    using Models.Core;
    using Models.Factorial;
    using global::UserInterface.Views;
    using global::EventArguments;
    using global::UserInterface.Commands;

    /// <summary>Presenter class for working with HtmlView</summary>
    public class SummaryPresenter : IPresenter
    {
        /// <summary>The summary model to work with.</summary>
        private Summary summaryModel;

        /// <summary>The view model to work with.</summary>
        private ISummaryView summaryView;

        /// <summary>The explorer presenter which manages this presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>Our data store</summary>
        [Link]
        private IStorageReader dataStore = null;

        /// <summary>Attach the model to the view.</summary>
        /// <param name="model">The model to work with</param>
        /// <param name="view">The view to attach to</param>
        /// <param name="parentPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter parentPresenter)
        {
            summaryModel = model as Summary;
            this.explorerPresenter = parentPresenter;
            summaryView = view as ISummaryView;
            // populate the simulation names in the view.
            Simulation parentSimulation = Apsim.Parent(this.summaryModel, typeof(Simulation)) as Simulation;
            if (parentSimulation != null)
            {
                if (parentSimulation.Parent is Experiment)
                {
                    Experiment experiment = parentSimulation.Parent as Experiment;
                    string[] simulationNames = experiment.GetSimulationNames().ToArray();
                    summaryView.SimulationDropDown.Values = simulationNames;
                    if (simulationNames.Length > 0)
                    {
                        summaryView.SimulationDropDown.SelectedValue = simulationNames[0];
                    }
                }
                else
                {
                    summaryView.SimulationDropDown.Values = new string[] { parentSimulation.Name };
                    summaryView.SimulationDropDown.SelectedValue = parentSimulation.Name;
                }

                // Populate the view.
                this.SetHtmlInView();

                summaryView.SummaryCheckBox.IsChecked = summaryModel.CaptureSummaryText;
                summaryView.SummaryCheckBox.Changed += OnSummaryCheckBoxChanged;
                summaryView.WarningCheckBox.IsChecked = summaryModel.CaptureWarnings;
                summaryView.WarningCheckBox.Changed += OnWarningCheckBoxChanged;
                summaryView.ErrorCheckBox.IsChecked = summaryModel.CaptureErrors;
                summaryView.ErrorCheckBox.Changed += OnErrorCheckBoxChanged;

                // Subscribe to the simulation name changed event.
                summaryView.SimulationDropDown.Changed += this.OnSimulationNameChanged;

                // Subscribe to the view's copy event.
                summaryView.HtmlView.Copy += OnCopy;
            }
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            summaryView.SimulationDropDown.Changed -= this.OnSimulationNameChanged;
            summaryView.HtmlView.Copy -= OnCopy;
            summaryView.SummaryCheckBox.Changed -= OnSummaryCheckBoxChanged;
            summaryView.WarningCheckBox.Changed -= OnWarningCheckBoxChanged;
            summaryView.ErrorCheckBox.Changed -= OnErrorCheckBoxChanged;
        }

        /// <summary>Populate the summary view.</summary>
        private void SetHtmlInView()
        {
            StringWriter writer = new StringWriter();
            Summary.WriteReport(this.dataStore, summaryView.SimulationDropDown.SelectedValue, writer, Utility.Configuration.Settings.SummaryPngFileName, outtype: Summary.OutputType.html);
            summaryView.HtmlView.SetContents(writer.ToString(), false);
            writer.Close();
        }

        /// <summary>Handles the SimulationNameChanged event of the view control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnSimulationNameChanged(object sender, EventArgs e)
        {
            SetHtmlInView();
        }

        private void OnSummaryCheckBoxChanged(object sender, EventArgs e)
        {
            ChangeProperty command = new ChangeProperty(summaryModel, "CaptureSummaryText", summaryView.SummaryCheckBox.IsChecked);
            explorerPresenter.CommandHistory.Add(command);
        }

        private void OnWarningCheckBoxChanged(object sender, EventArgs e)
        {
            ChangeProperty command = new ChangeProperty(summaryModel, "CaptureWarnings", summaryView.WarningCheckBox.IsChecked);
            explorerPresenter.CommandHistory.Add(command);
        }

        private void OnErrorCheckBoxChanged(object sender, EventArgs e)
        {
            ChangeProperty command = new ChangeProperty(summaryModel, "CaptureErrors", summaryView.ErrorCheckBox.IsChecked);
            explorerPresenter.CommandHistory.Add(command);
        }

        /// <summary>
        /// Event handler for the view's copy event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnCopy(object sender, CopyEventArgs e)
        {
            this.explorerPresenter.SetClipboardText(e.Text, "CLIPBOARD");
        }
    }
}