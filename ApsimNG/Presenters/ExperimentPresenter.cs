namespace UserInterface.Presenters
{
    using Commands;
    using EventArguments;
    using Interfaces;
    using Models.Core;
    using Models.Factorial;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Views;

    public class ExperimentPresenter : IPresenter
    {
        /// <summary>
        /// The storage writer.
        /// </summary>
        [Link]
        private IStorageWriter storage = null;

        /// <summary>
        /// Command to handle the running of simulations from this view.
        /// </summary>
        private RunCommand runner;

        /// <summary>
        /// The Experiment node that was clicked on.
        /// </summary>
        private Experiment model;

        /// <summary>
        /// The view responsible for displaying the factor information in a table.
        /// </summary>
        private IExperimentView view;

        /// <summary>
        /// List of the view's column headers. The first one is 'Simulation Name', then the rest are the factor names. The final header is 'Enabled'.
        /// </summary>
        private List<string> headers;

        /// <summary>
        /// List of tuples, where each tuple contains the name of the simulations, the factor levels/values, and a boolean indicating whether it should be run or not.
        /// </summary>
        private List<Tuple<string, List<string>, bool>> simulations;

        /// <summary>
        /// Maximum number of simulations to display. Defaults to this.DEFAULT_MAX_SIMS, but the user can modify this.
        /// </summary>
        private int maxSimsToDisplay;

        /// <summary>
        /// The explorer presenter controlling the tab's contents.
        /// </summary>
        private ExplorerPresenter presenter;

        /// <summary>
        /// By default, only display this many simulations are displayed (for performance reasons).
        /// </summary>
        public const int DefaultMaxSims = 50;

        /// <summary>Attach the model to the view.</summary>
        /// <param name="model">The model.</param>
        /// <param name="view">The view.</param>
        /// <param name="parentPresenter">The explorer presenter.</param>
        public void Attach(object modelObject, object viewObject, ExplorerPresenter parentPresenter)
        {
            model = modelObject as Experiment;
            view = viewObject as ExperimentView;
            presenter = parentPresenter;

            runner = new RunCommand(model, presenter, false, storage);
            // Once the simulation is finished, we will need to reset the disabled simulation names.
            runner.Finished += OnSimulationsCompleted;

            view.EnableSims += OnEnable;
            view.DisableSims += OnDisable;
            view.ExportCsv += OnExportCsv;
            view.ImportCsv += OnImportCsv;
            view.RunSims += OnRunSims;
            view.SetMaxSims += OnSetMaxNumSims;

            maxSimsToDisplay = DefaultMaxSims;
            List<List<FactorValue>> allCombinations = model.AllCombinations();
            if (allCombinations == null || !allCombinations.Any())
                throw new Exception(string.Format("Unable to generate a list of factors for experiment {0}.", model.Name));

            headers = GetHeaderNames(allCombinations.First());
            simulations = GetTableData(allCombinations);

            view.Initialise(headers);
            UpdateView();
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            view.Detach();
            view.SetMaxSims -= OnSetMaxNumSims;
            view.EnableSims -= OnEnable;
            view.DisableSims -= OnDisable;
            view.ExportCsv -= OnExportCsv;
            view.ImportCsv -= OnImportCsv;
            view.RunSims -= OnRunSims;
            runner.Finished -= OnSimulationsCompleted;
        }

        /// <summary>
        /// Enables all of the selected simulations.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnEnable(object sender, EventArgs args)
        {
            ToggleSims(view.SelectedItems, true);
        }

        /// <summary>
        /// Disables all of the selected simulations.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnDisable(object sender, EventArgs args)
        {
            ToggleSims(view.SelectedItems, false);
        }

        /// <summary>
        /// Sets the enabled status of a given list of simulations.
        /// </summary>
        /// <param name="names">Names of the simulations to modify.</param>
        /// <param name="flag">If true, the selected simulations will be enabled. If false, they will be disabled.</param>
        private void ToggleSims(List<string> names, bool flag)
        {
            foreach (string name in names)
            {
                int index = GetSimFromName(name);
                if (simulations[index].Item3 != flag)
                {
                    List<string> data = simulations[index].Item2;
                    simulations[index] = new Tuple<string, List<string>, bool>(name, data, flag);
                }
            }
            UpdateView();
            ChangeProperty changeDisabledSims = new ChangeProperty(model, "DisabledSimNames", GetDisabledSimNames());
            changeDisabledSims.Do(presenter.CommandHistory);
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
                string newMaxSims = view.MaxSimsToDisplay;
                int n;
                if (string.IsNullOrEmpty(newMaxSims))
                {
                    maxSimsToDisplay = DefaultMaxSims;
                    UpdateView();
                }
                else if (Int32.TryParse(newMaxSims, out n))
                {
                    if (n > 1000 && presenter.MainPresenter.AskQuestion("Displaying more than 1000 rows of data is not recommended! Are you sure you wish to do this?") != QuestionResponseEnum.Yes)
                        return;
                    else if (n < 0)
                        throw new Exception("Unable to display a negative number of simulations.");
                    maxSimsToDisplay = n;
                    simulations = GetTableData(model.AllCombinations());
                    UpdateView();
                }
                else
                    throw new Exception(string.Format("Unable to parse max number of simulations: {0}", newMaxSims));
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Updates the view's table of simulations.
        /// </summary>
        private void UpdateView()
        {
            if (maxSimsToDisplay < 0) maxSimsToDisplay = DefaultMaxSims; // doesn't hurt to double check
            view.Populate(simulations.GetRange(0, Math.Min(simulations.Count, maxSimsToDisplay)));
            view.NumSims = model.AllCombinations().Count.ToString();
        }

        /// <summary>
        /// Runs a list of simulations.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnRunSims(object sender, EventArgs args)
        {
            try
            {
                // Before running the simulations, disable all simulations except for those which are selected.
                model.DisabledSimNames = model.GetSimulationNames().Where(s => !view.SelectedItems.Contains(s)).ToList();
                runner.Do(presenter.CommandHistory);
            }
            catch (Exception e)
            {
                presenter.MainPresenter.ShowError(e);
            }
        }

        /// <summary>
        /// Runs when the simulations have finished. Resets the experiment's disabled simulations to 
        /// their state before the simulations were run.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnSimulationsCompleted(object sender, EventArgs args)
        {
            try
            {
                // We don't use a ChangeProperty command here, because this action was not initiated by the user,
                // and should not be undo-able.
                model.DisabledSimNames = simulations.Where(s => !s.Item3).Select(s => s.Item1).ToList();
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Gets the name of a simulation (list of factors levels).
        /// </summary>
        /// <param name="factors"></param>
        /// <returns></returns>
        private string GetName(List<FactorValue> factors)
        {
            return factors.Select(f => f.Name).Aggregate((a, b) => a + b);
        }

        /// <summary>
        /// Generates a list of column headers to be displayed.
        /// </summary>
        /// <param name="simulation">A single simulation's factors.</param>
        /// <returns>List containing the column header names.</returns>
        private List<string> GetHeaderNames(List<FactorValue> simulation)
        {
            // First column will always be simulation name.
            List<string> headers = new List<string> { "Simulation Name" };

            // The next columns will contain the factor names.
            foreach (Factor factor in simulation.Select(x => x.Factor))
            {
                string name = factor.Parent is Factors ? factor.Name : factor.Parent.Name;
                headers.Add(name);
            }

            // The final column shows whether the specific simulation is enabled.
            headers.Add("Enabled");

            return headers;
        }
        
        /// <summary>
        /// Formats a 2 dimensional list of FactorValue into a list of tuples (containing only the data relevant to the view) that may be passed into the view.
        /// </summary>
        /// <param name="allSims">List of 'simulations', where each simulation is a list of factor values. Typically, this.model.AllCombinations() is passed in here.</param>
        /// <returns>List of tuples, where each tuple contains the name of the simulations, the factor levels/values, and a boolean indicating whether it should be run or not.</returns>
        private List<Tuple<string, List<string>, bool>> GetTableData(List<List<FactorValue>> allSims, bool getAllData = false)
        {
            try
            {
                List<Tuple<string, List<string>, bool>> sims = new List<Tuple<string, List<string>, bool>>();
                int i = 0;
                foreach (List<FactorValue> factors in model.AllCombinations())
                {
                    if (!getAllData && i > maxSimsToDisplay)
                        break;
                    List<string> values = new List<string>();
                    List<string> names = new List<string>();
                    Experiment.GetFactorNamesAndValues(factors, names, values);
                    // Pack all factor levels for the current simulation into a list.
                    string name = model.Name + GetName(factors);
                    bool flag = !model.DisabledSimNames.Contains(name);
                    sims.Add(new Tuple<string, List<string>, bool>(name, values, flag));
                    i++;
                }
                return sims;
            }
            catch (Exception e)
            {
                presenter.MainPresenter.ShowError(e);
                return new List<Tuple<string, List<string>, bool>>();
            }
        }

        /// <summary>
        /// Generates a list of 'simulations', where each simulation is a list of factor values.
        /// This function is currently unused, but may be useful in the future.
        /// </summary>
        /// <returns></returns>
        private List<List<FactorValue>> GetEnabledSimulations()
        {
            // Names of the disabled simulations.
            List<string> disabledSimNames = GetDisabledSimNames();

            // To generate this list, a full factorial experiment is generated, and the results filtered based on the name of the simulation.
            return model.AllCombinations().Where(x => (!disabledSimNames.Contains(GetName(x)))).ToList();
        }

        /// <summary>
        /// Gets a list of the names of all disabled simulations.
        /// </summary>
        /// <returns></returns>
        private List<string> GetDisabledSimNames()
        {
            return simulations.Where(x => !x.Item3).Select(x => x.Item1).ToList();
        }

        /// <summary>
        /// Returns the index of a simulation with a given name in the global list of simulations.
        /// </summary>
        /// <param name="name">Name of the simulation.</param>
        /// <returns></returns>
        private int GetSimFromName(string name)
        {
            return simulations.Select(s => s.Item1).ToList().IndexOf(name);
        }

        /// <summary>
        /// Generates a .csv file containing the factor information displayed in the grid.
        /// The user can edit this file to more efficiently enable or disable factors in bulk.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnExportCsv(object sender, FileActionArgs args)
        {
            try
            {
                if (string.IsNullOrEmpty(args.Path))
                    throw new ArgumentNullException("Unable to generate csv file: path is null.");

                StringBuilder data = new StringBuilder();
                if (headers == null || !headers.Any())
                    throw new Exception("No data to export.");

                // The first line contains the column headers.
                string currentLine = headers.Aggregate((a, b) => a + "," + b);
                data.AppendLine(currentLine);

                // The rest of the file contains the factor information.
                foreach (Tuple<string, List<string>, bool> sim in GetTableData(model.AllCombinations(), true))
                {
                    // The first item on each line is the simulation name.
                    currentLine = sim.Item1 + ",";
                    // The rest of the line (except for the last item) contains the factor levels.
                    currentLine += sim.Item2.Aggregate((a, b) => a + "," + b);
                    // The final item on each line is the enabled status of the simulation.
                    currentLine += "," + sim.Item3.ToString();
                    data.AppendLine(currentLine);
                }

                File.WriteAllText(args.Path, data.ToString());
                presenter.MainPresenter.ShowMessage(string.Format("Successfully generated {0}.", args.Path), Simulation.MessageType.Information);
            }
            catch (Exception e)
            {
                presenter.MainPresenter.ShowError(e);
            }
        }

        /// <summary>
        /// Imports factor information from a csv file, saves the data to this.simulations, then updates the TreeView.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnImportCsv(object sender, FileActionArgs args)
        {
            try
            {
                if (string.IsNullOrEmpty(args.Path))
                    throw new ArgumentNullException("Unable to import csv file: path is null.");
                if (!File.Exists(args.Path))
                    throw new ArgumentException("Unable to import {0}: file does not exist.");

                using (StreamReader file = new StreamReader(args.Path))
                {
                    string line = file.ReadLine();
                    List<string> data = line.Split(',').ToList();
                    if (!data.SequenceEqual(headers))
                    {
                        throw new Exception("Column Headers in " + args.Path + " do not match current headers. Are you sure you selected the correct .csv file?");
                    }

                    simulations = new List<Tuple<string, List<string>, bool>>();

                    int i = 2;
                    while ((line = file.ReadLine()) != null)
                    {
                        data = line.Split(',').ToList();

                        string name = data[0];
                        if (data.Count == headers.Count)
                        {
                            bool enabled;
                            if (!bool.TryParse(data[data.Count - 1], out enabled))
                                throw new Exception("Unable to parse " + data[data.Count - 1] + " to bool on line " + i + ".");
                            simulations.Add(new Tuple<string, List<string>, bool>(data[0], data.Skip(1).Take(data.Count - 2).ToList(), enabled));
                        }
                        else if (data.Count > headers.Count)
                            throw new Exception("Too many elements in row " + i + ".");
                        else
                            throw new Exception("Too few elements in row " + i + ".");
                    }
                }
                UpdateView();
                model.DisabledSimNames = GetDisabledSimNames();
                presenter.MainPresenter.ShowMessage("Successfully imported data from " + args.Path, Simulation.MessageType.Information);
            }
            catch (Exception e)
            {
                presenter.MainPresenter.ShowError(e);
            }
        }
    }
}
