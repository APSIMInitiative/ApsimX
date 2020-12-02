namespace UserInterface.Presenters
{
    using APSIM.Shared.Utilities;
    using Commands;
    using Interfaces;
    using Models.Core;
    using Models.Core.Run;
    using Models.Factorial;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Views;

    public class ExperimentPresenter : IPresenter
    {
        /// <summary>The Experiment node.</summary>
        private Experiment experiment;

        /// <summary>The attached view.</summary>
        private IExperimentView view;

        /// <summary>The explorer presenter controlling the tab's contents.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>List of all experiment simulations.</summary>
        private List<SimulationDescription> simulationDescriptions;

        /// <summary>By default, only display this many simulations (for performance reasons).</summary>
        private const int DefaultMaxSims = 50;

        /// <summary>The list of columns that we will hide from user.</summary>
        private string[] hiddenColumns = new string[] { "Experiment", "Zone", "FolderName" };

        /// <summary>Attach the model to the view.</summary>
        /// <param name="modelObject">The model.</param>
        /// <param name="viewObject">The view.</param>
        /// <param name="parentPresenter">The explorer presenter.</param>
        public void Attach(object modelObject, object viewObject, ExplorerPresenter parentPresenter)
        {
            experiment = modelObject as Experiment;
            view = viewObject as ExperimentView;
            explorerPresenter = parentPresenter;


            // Once the simulation is finished, we will need to reset the disabled simulation names.
            //runner.Finished += OnSimulationsCompleted;

            view.EnableAction.Clicked += OnEnable;
            view.DisableAction.Clicked += OnDisable;
            view.ExportToCSVAction.Clicked += OnExportCsv;
            view.ImportFromCSVAction.Clicked += OnImportCsv;
            view.RunAPSIMAction.Clicked += OnRunSims;
            view.MaximumNumSimulations.Leave += OnSetMaxNumSims;

            // Give the view the default maximum number of simulations to display.
            view.MaximumNumSimulations.Text = DefaultMaxSims.ToString();

            // Get a list of all simulation descriptions (even disabled ones).
            GetAllSimulationDescriptionsFromExperiment();

            // Populate the view.
            PopulateView();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            //runner.Finished -= OnSimulationsCompleted;
            view.EnableAction.Clicked -= OnEnable;
            view.DisableAction.Clicked -= OnDisable;
            view.ExportToCSVAction.Clicked -= OnExportCsv;
            view.ImportFromCSVAction.Clicked -= OnImportCsv;
            view.RunAPSIMAction.Clicked -= OnRunSims;
            view.MaximumNumSimulations.Leave -= OnSetMaxNumSims;
        }

        /// <summary>Populate the view.</summary>
        private void PopulateView()
        {
            // Create a table to give to the grid control.
            var table = new DataTable();
            if (simulationDescriptions.Count > 0)
            {
                // Using the first simulation description, create a column in the table
                // for each descriptor.
                foreach (var simulationDescription in simulationDescriptions)
                    foreach (var descriptor in simulationDescription.Descriptors)
                    {
                        if (!hiddenColumns.Contains(descriptor.Name) &&
                            !table.Columns.Contains(descriptor.Name))
                            table.Columns.Add(descriptor.Name, typeof(string));
                    }
            }

            // Add all simulations to table up to the maximum number of sims to display.
            var maximumNumberOfSimulations = Convert.ToInt32(view.MaximumNumSimulations.Text, CultureInfo.InvariantCulture);
            var cellRenderDetails = new List<CellRendererDescription>();
            for (int i = 0; i < Math.Min(simulationDescriptions.Count, maximumNumberOfSimulations); i++)
            {
                // If this is a disabled sim then store the index for later.
                if (experiment.DisabledSimNames != null && experiment.DisabledSimNames.Contains(simulationDescriptions[i].Name))
                    cellRenderDetails.Add(
                        new CellRendererDescription()
                        {
                            RowIndex = i,
                            ColumnIndex = 0,
                            StrikeThrough = true
                        });

                var row = table.NewRow();

                foreach (var descriptor in simulationDescriptions[i].Descriptors)
                {
                    if (!hiddenColumns.Contains(descriptor.Name))
                        row[descriptor.Name] = descriptor.Value;
                }
                table.Rows.Add(row);
            }
            // Give the table to the view.
            view.List.DataSource = table;

            // Give the disabled simulations to the view as strikethroughs.
            view.List.CellRenderDetails = cellRenderDetails;

            // Populate the number of simulations label.
            view.NumberSimulationsLabel.Text = "Number of simulations: " + simulationDescriptions.Count;
        }

        /// <summary>Get a list of all simulation descriptions (even disabled ones).</summary>
        private void GetAllSimulationDescriptionsFromExperiment()
        {
            List<string> savedDisabledSimulationNames = experiment.DisabledSimNames;
            experiment.DisabledSimNames = null;
            simulationDescriptions = experiment.GenerateSimulationDescriptions();
            experiment.DisabledSimNames = savedDisabledSimulationNames;
        }

        /// <summary>Get the list of selected simulation names from view.</summary>
        private List<string> GetSelectedSimulationNamesFromView()
        {
            var selectedSimulationNames = new List<string>();
            foreach (var row in view.List.SelectedIndicies)
                selectedSimulationNames.Add(view.List.DataSource.Rows[row]["SimulationName"].ToString());
            return selectedSimulationNames;
        }

        /// <summary>User has clicked enable in view.</summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnEnable(object sender, EventArgs args)
        {
            var selectedSimulations = GetSelectedSimulationNamesFromView();
            if (experiment.DisabledSimNames != null)
                experiment.DisabledSimNames.RemoveAll(s => selectedSimulations.Contains(s));
            PopulateView();
        }

        /// <summary>User has clicked disable in view.</summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnDisable(object sender, EventArgs args)
        {
            var selectedSimulations = GetSelectedSimulationNamesFromView();
            if (experiment.DisabledSimNames == null)
                experiment.DisabledSimNames = selectedSimulations;
            else
                experiment.DisabledSimNames = selectedSimulations.Union(experiment.DisabledSimNames).ToList();
            PopulateView();
        }
        
        /// <summary>
        /// Generates a .csv file containing the factor information displayed in the grid.
        /// The user can edit this file to more efficiently enable or disable factors in bulk.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnExportCsv(object sender, EventArgs args)
        {
            try
            {
                var path = explorerPresenter.MainPresenter.AskUserForSaveFileName("*.csv", null);
                if (path != null)
                {
                    // Clone the datatable and add an enabled column.
                    var data = view.List.DataSource.Copy();
                    data.Columns.Add("Enabled?", typeof(bool));
                    foreach (DataRow row in data.Rows)
                    {
                        var simulationName = row[0].ToString();
                        row["Enabled"] = !experiment.DisabledSimNames.Contains(simulationName);
                    }

                    // Convert datatable to csv and write to path.
                    using (var writer = new StreamWriter(path))
                        DataTableUtilities.DataTableToText(data, 0, ",", true, writer);

                    explorerPresenter.MainPresenter.ShowMessage(string.Format("Successfully generated {0}.", path), Simulation.MessageType.Information);
                }
            }
            catch (Exception e)
            {
                explorerPresenter.MainPresenter.ShowError(e);
            }
        }

        /// <summary>Imports factor information from a csv file.</summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnImportCsv(object sender, EventArgs args)
        {
            ApsimTextFile textFile = new ApsimTextFile();
            try
            {
                var path = explorerPresenter.MainPresenter.AskUserForOpenFileName("*.csv");
                if (path != null)
                {
                    textFile.Open(path);
                    var disabledSimsTable = new DataView(textFile.ToTable());
                    disabledSimsTable.RowFilter = "Enabled = False";
                    experiment.DisabledSimNames = DataTableUtilities.GetColumnAsStrings(disabledSimsTable, "SimulationName").ToList();
                }
                PopulateView();
                explorerPresenter.MainPresenter.ShowMessage("Successfully imported data from " + path, Simulation.MessageType.Information);
            }
            catch (Exception e)
            {
                explorerPresenter.MainPresenter.ShowError(e);
            }
            finally
            {
                textFile.Close();
            }
        }

        /// <summary>Runs the selected simulations.</summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnRunSims(object sender, EventArgs args)
        {
            var savedDisabledSimulationNames = experiment.DisabledSimNames;

            try
            {
                var selectedSimulations = GetSelectedSimulationNamesFromView();

                // Before running the simulations, disable all simulations except for those which are selected.
                var runner = new Runner(experiment, simulationNamesToRun: selectedSimulations, wait: false);
                RunCommand runCmd = new RunCommand(experiment.Name, runner, explorerPresenter);
                runCmd.Do();
            }
            catch (Exception e)
            {
                explorerPresenter.MainPresenter.ShowError(e);
            }
            finally
            {
                experiment.DisabledSimNames = savedDisabledSimulationNames;
            }
        }

        /// <summary>
        /// Sets the maximum number of simulations (rows in the view's table) allowed to be displayed at once, then updates the view.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnSetMaxNumSims(object sender, EventArgs args)
        {
            try
            {
                var maxNumSimsString = view.MaximumNumSimulations.Text;
                int n;
                if (string.IsNullOrEmpty(maxNumSimsString))
                    view.MaximumNumSimulations.Text = DefaultMaxSims.ToString();
                else if (Int32.TryParse(maxNumSimsString, out n))
                {
                    if (n > 1000 && explorerPresenter.MainPresenter.AskQuestion("Displaying more than 1000 rows of data is not recommended! Are you sure you wish to do this?") != QuestionResponseEnum.Yes)
                        return;
                    else if (n < 0)
                        throw new Exception("Unable to display a negative number of simulations.");
                }
                else
                    throw new Exception(string.Format("Unable to parse max number of simulations: {0}", maxNumSimsString));

                PopulateView();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }
    }
}