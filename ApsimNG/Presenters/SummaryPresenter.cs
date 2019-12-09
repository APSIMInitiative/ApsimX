namespace UserInterface.Presenters
{
    using EventArguments;
    using System;
    using System.IO;
    using System.Linq;
    using Models;
    using Models.Core;
    using Models.Factorial;
    using Views;
    using Commands;
    using Utility;
    using Models.Storage;
    using System.Collections.Generic;
    using Models.Core.Run;

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
        private IDataStore dataStore = null;

        /// <summary>Attach the model to the view.</summary>
        /// <param name="model">The model to work with</param>
        /// <param name="view">The view to attach to</param>
        /// <param name="parentPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter parentPresenter)
        {
            summaryModel = model as Summary;
            this.explorerPresenter = parentPresenter;
            summaryView = view as ISummaryView;

            // Populate the view.
            SetSimulationNamesInView();
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

        private void SetSimulationNamesInView()
        {
            // populate the simulation names in the view.
            IModel scopedParent = ScopingRules.FindScopedParentModel(summaryModel);

            if (scopedParent is Simulation parentSimulation)
            {
                if (scopedParent.Parent is Experiment)
                    scopedParent = scopedParent.Parent;
                else
                {
                    summaryView.SimulationDropDown.Values = new string[] { parentSimulation.Name };
                    summaryView.SimulationDropDown.SelectedValue = parentSimulation.Name;
                    return;
                }
            }

            if (scopedParent is Experiment experiment)
            {
                string[] simulationNames = experiment.GenerateSimulationDescriptions().Select(s => s.Name).ToArray();
                summaryView.SimulationDropDown.Values = simulationNames;
                if (simulationNames != null && simulationNames.Count() > 0)
                    summaryView.SimulationDropDown.SelectedValue = simulationNames.First();
            }
            else
            {
                List<ISimulationDescriptionGenerator> simulations = Apsim.FindAll(summaryModel, typeof(ISimulationDescriptionGenerator)).Cast<ISimulationDescriptionGenerator>().ToList();
                simulations.RemoveAll(s => s is Simulation && (s as IModel).Parent is Experiment);
                string[] simulationNames = simulations.SelectMany(m => m.GenerateSimulationDescriptions()).Select(m => m.Name).ToArray();
                summaryView.SimulationDropDown.Values = simulationNames;
                if (simulationNames != null && simulationNames.Length > 0)
                    summaryView.SimulationDropDown.SelectedValue = simulationNames[0];
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
            Summary.WriteReport(dataStore, summaryView.SimulationDropDown.SelectedValue, writer, Configuration.Settings.SummaryPngFileName, outtype: Summary.OutputType.html, darkTheme : Configuration.Settings.DarkTheme);
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