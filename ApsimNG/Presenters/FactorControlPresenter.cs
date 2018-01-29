using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Views;
using Models.Core;
using Models.Factorial;
using System.IO;

namespace UserInterface.Presenters
{
    public class FactorControlPresenter : IPresenter
    {
        /// <summary>
        /// The explorer presenter controlling the tab's contents.
        /// </summary>
        public ExplorerPresenter explorerPresenter { get; set; }

        /// <summary>
        /// By default, only display this many simulations are displayed (for performance reasons).
        /// </summary>
        public static int DEFAULT_MAX_SIMS = 50;

        /// <summary>
        /// The Experiment node that was clicked on.
        /// </summary>
        private Experiment model;

        /// <summary>
        /// The view responsible for displaying the factor information in a table.
        /// </summary>
        private FactorControlView view;

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

        private readonly string[] MONTH_NAMES = { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };
        public void Attach(object experiment, object viewer, ExplorerPresenter explorerPresenter)
        {
            model = (Experiment)experiment;
            view = (FactorControlView)viewer;
            view.Presenter = this;
            this.explorerPresenter = explorerPresenter;            
            maxSimsToDisplay = DEFAULT_MAX_SIMS;

            var simNames = model.GetSimulationNames().ToArray();
            var allCombinations = model.AllCombinations();
            headers = GetHeaderNames(allCombinations);
            simulations = GetTableData(allCombinations);               
            
            view.Initialise(headers);
            UpdateView();
        }


        public void Detach()
        {
            headers = null;
            simulations = null;
            view.Detach();
            view = null;
        }

        /// <summary>
        /// Sets the enabled status of a given list of simulations.
        /// </summary>
        /// <param name="names">Names of the simulations to modify.</param>
        /// <param name="flag">If true, the selected simulations will be enabled. If false they will be disabled.</param>
        public void ToggleSims(List<string> names, bool flag)
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
            model.DisabledSimNames = GetDisabledSimNames();
        }

        /// <summary>
        /// Sets the maximum number of simulations (rows in the view's table) allowed to be displayed at once, then updates the view.
        /// </summary>
        /// <param name="str">Max number of simulations allowed to be displayed.</param>
        public void SetMaxNumSims(string str)
        {
            if (str == null || str == "")
            {
                maxSimsToDisplay = DEFAULT_MAX_SIMS;
                UpdateView();
            }
            else if (Int32.TryParse(str, out int n))
            {
                if (n > 1000 && explorerPresenter.MainPresenter.AskQuestion("Displaying more than 1000 rows of data is not recommended! Are you sure you wish to do this?") != QuestionResponseEnum.Yes)
                {                    
                    return; // if user has changed their mind (because of the warning) then do nothing
                } else if (n < 0)
                {
                    explorerPresenter.MainPresenter.ShowMessage("Max number of simulations must be a positive number.", Simulation.ErrorLevel.Error); // don't allow users to specify a negative number (0 is acceptable)                
                    return;
                }
                maxSimsToDisplay = n;
                simulations = GetTableData(model.AllCombinations());
                UpdateView();
            } else
            {
                explorerPresenter.MainPresenter.ShowMessage("Unable to parse max number of simulations " + str + " to int", Simulation.ErrorLevel.Error);
            }
        }

        /// <summary>
        /// Updates the view's table of simulations.
        /// </summary>
        public void UpdateView()
        {
            if (maxSimsToDisplay < 0) maxSimsToDisplay = DEFAULT_MAX_SIMS; // doesn't hurt to double check
            view.Populate(simulations.GetRange(0, Math.Min(simulations.Count, maxSimsToDisplay)));
            view.NumSims = model.AllCombinations().Count.ToString();
        }
        
        /// <summary>
        /// Gets the name of a simulation (list of factors levels).
        /// </summary>
        /// <param name="factors"></param>
        /// <returns></returns>
        private string GetName(List<FactorValue> factors)
        {
            string str = "";
            for (int i = 0; i < factors.Count; i++)
            {
                str += factors[i].Name;
            }
            return str;
        }

        /// <summary>
        /// Generates a list of column headers to be displayed.
        /// </summary>
        /// <param name="allSims">List of 'simulations', where each simulation is a list of factor values.</param>
        /// <returns></returns>
        private List<string> GetHeaderNames(List<List<FactorValue>> allSims)
        {
            List<string> headers = new List<string> { "Simulation Name" };
            foreach (Factor factor in allSims[0].Select(x => x.Factor))
            {
                string name = factor.Parent.Name;
                if (factor.Parent is Factors) headers.Add(factor.Name);
                else headers.Add(factor.Parent.Name);
                //if (name.ToLower() == "factors") name = factor.Name;
                //headers.Add(factor.Name);
            }
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
            List<Tuple<string, List<string>, bool>> sims = new List<Tuple<string, List<string>, bool>>();
            int i = 0;
            try
            {
                foreach (List<FactorValue> factors in model.AllCombinations())
                {
                    if (!getAllData && i > maxSimsToDisplay) break;
                    string name = "";
                    List<string> values = new List<string>();
                    List<string> names = new List<string>();
                    Experiment.GetFactorNamesAndValues(factors, names, values);
                    // pack all factor levels for the current simulation into a list
                    foreach (FactorValue factor in factors)
                    {
                        name += factor.Name;
                    }
                    bool flag = !model.DisabledSimNames.Contains(name);
                    sims.Add(new Tuple<string, List<string>, bool>(name, values, flag));
                    i++;
                }
                return sims;
            } catch (Exception e)
            {
                explorerPresenter.MainPresenter.ShowMessage(e.ToString(), Simulation.ErrorLevel.Error);
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
            // names of the enabled simulations
            List<string> disabledSimNames = GetDisabledSimNames();

            // to generate this list, a full factorial experiment is generated, and the results filtered based on the name of the simulation
            List<List<FactorValue>> enabledSims = model.AllCombinations().Where(x => (disabledSimNames.IndexOf(GetName(x)) < 0)).ToList();
            return enabledSims;
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
            for (int i = 0; i < simulations.Count; i++)
            {
                if (simulations[i].Item1 == name)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Generates a CSV file containing the factor information displayed in the TreeView.
        /// The user can edit this file to more efficiently enable or disable factors in bulk.
        /// </summary>
        /// <param name="path">Directory for the csv file to be saved to.</param>
        public void GenerateCsv(string path)
        {
            if (path == null || path == "") return;
            StringBuilder csv = new StringBuilder();
            if (headers == null || headers.Count < 1)
            {
                explorerPresenter.MainPresenter.ShowMessage("Nothing to Export", Simulation.ErrorLevel.Error);
                return;
            }
            
            // column headers
            string newLine = headers[0];
            for (int i = 1; i < headers.Count; i++)
            {
                newLine += "," + headers[i];
            }
            csv.AppendLine(newLine);

            // factor information
            foreach (Tuple<string, List<string>, bool> sim in GetTableData(model.AllCombinations(), true))
            {
                newLine = sim.Item1; // simulation name
                foreach (string value in sim.Item2)  // factor values
                {
                    newLine += "," + value;
                }
                newLine += "," + sim.Item3.ToString(); // boolean - is the simulation active/enabled
                csv.AppendLine(newLine);
            }

            
            try
            {
                File.WriteAllText(path, csv.ToString());
                explorerPresenter.MainPresenter.ShowMessage("Successfully generated " + path + ".", Simulation.ErrorLevel.Information);
            } catch (Exception e)
            {
                explorerPresenter.MainPresenter.ShowMessage(e.ToString(), Simulation.ErrorLevel.Error);
            }
        }

        /// <summary>
        /// Imports factor information from a csv file, saves the data to this.simulations, then updates the TreeView.
        /// </summary>
        /// <param name="path">Path to the csv file.</param>
        public void ImportCsv(string path = "")
        {
            explorerPresenter.MainPresenter.ShowMessage("", Simulation.ErrorLevel.Error);

            if (path == null || path == "") return;
            try
            {
                using (StreamReader file = new StreamReader(path))
                {
                    string line = file.ReadLine();
                    List<string> data = line.Split(',').ToList();
                    if (!data.SequenceEqual(headers))
                    {
                        throw new Exception("Column Headers in " + path + " do not match current headers. Are you sure you selected the correct .csv file?");
                    }

                    simulations = new List<Tuple<string, List<string>, bool>>();

                    int i = 2;
                    while ((line = file.ReadLine()) != null)
                    {
                        data = line.Split(',').ToList();

                        string name = data[0];
                        if (data.Count == headers.Count)
                        {
                            if (!bool.TryParse(data[data.Count - 1], out bool enabled)) throw new Exception("Unable to parse " + data[data.Count - 1] + " to bool on line " + i + ".");
                            simulations.Add(new Tuple<string, List<string>, bool>(data[0], data.Skip(1).Take(data.Count - 2).ToList(), enabled));
                        }
                        else if (data.Count > headers.Count) throw new Exception("Too many elements in row " + i + ".");
                        else throw new Exception("Too few elements in row " + i + ".");
                    }
                }
                UpdateView();
                model.DisabledSimNames = GetDisabledSimNames();
                explorerPresenter.MainPresenter.ShowMessage("Successfully imported data from " + path, Simulation.ErrorLevel.Information);
            }
            catch (Exception e)
            {
                explorerPresenter.MainPresenter.ShowMessage(e.ToString(), Simulation.ErrorLevel.Error);
            }
        }

        public void Sobol()
        {
            explorerPresenter.MainPresenter.ShowMessage("This feature is currently under development.", Simulation.ErrorLevel.Information);
        }


        public void Morris()
        {
            explorerPresenter.MainPresenter.ShowMessage("This feature is currently under development.", Simulation.ErrorLevel.Information);
        }
    }
}
