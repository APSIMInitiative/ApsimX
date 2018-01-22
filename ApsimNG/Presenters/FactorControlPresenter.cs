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

        private Experiment model;
        private FactorControlView view;
        public ExplorerPresenter explorerPresenter;
        private List<string> headers;
        private List<Tuple<string, List<string>, bool>> simulations;

        public void Attach(object experiment, object viewer, ExplorerPresenter explorerPresenter)
        {
            model = (Experiment)experiment;
            view = (FactorControlView)viewer;
            view.Presenter = this;

            this.explorerPresenter = explorerPresenter;
            var simNames = model.GetSimulationNames().ToArray();

            var allCombinations = model.AllCombinations();
            headers = GetHeaderNames(allCombinations);
            simulations = GetTableData(allCombinations);
            var eView = explorerPresenter.GetView() as ExplorerView;
            
            
            view.Initialise(headers);
            view.Populate(simulations);            
            
            
            
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
            view.Populate(simulations);
        }

        public void Detach()
        {

        }

        private List<string> GetHeaderNames(List<List<FactorValue>> allSims)
        {
            List<string> headers = new List<string> { "Simulation Name" };
            foreach (Factor factor in allSims[0].Select(x => x.Factor)) headers.Add(factor.Name);
            headers.Add("Enabled");

            return headers;
        }
        
        /// <summary>
        /// Formats a 2 dimensional list of FactorValue into a list that may be passed into the view.
        /// </summary>
        /// <param name="allSims"></param>
        /// <returns></returns>
        private List<Tuple<string, List<string>, bool>> GetTableData(List<List<FactorValue>> allSims)
        {
            List<Tuple<string, List<string>, bool>> sims = new List<Tuple<string, List<string>, bool>>();
            foreach (List<FactorValue> factors in model.AllCombinations())
            {
                string name = "";

                // pack all factor levels for the simulation into a list
                List<string> levels = new List<string>();
                foreach (FactorValue factor in factors)
                {
                    // generate simulation name
                    // TODO : figure out if there's a method somewhere else in Apsim which does this for me
                    name += factor.Name;

                    object val = factor.Values[0];
                    string value = val.GetType() == typeof(string) ? (string)val : ((Model)val).Name;
                    levels.Add(value);
                }

                // enable all simulations by default
                sims.Add(new Tuple<string, List<string>, bool>(name, levels, true));
            }
            return sims;
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
        public void GenerateCsv(string path = "")
        {
            StringBuilder csv = new StringBuilder();
            string newLine = headers[0];
            for (int i = 1; i < headers.Count; i++)
            {
                newLine += "," + headers[i];
            }

            csv.AppendLine(newLine);

            foreach (Tuple<string, List<string>, bool> sim in simulations)
            {
                newLine = sim.Item1;
                foreach (string value in sim.Item2)
                {
                    newLine += "," + value;
                }
                newLine += "," + sim.Item3.ToString();
                csv.AppendLine(newLine);
            }
            if (path == "") path = ApsimNG.Properties.Settings.Default["OutputDir"] + "\\" + model.Name + ".csv";
            File.WriteAllText(path, csv.ToString());
            explorerPresenter.MainPresenter.ShowMessage("Successfully generated CSV file.", Simulation.ErrorLevel.Information);            
        }

        /// <summary>
        /// Imports factor information from a csv file, saves the data to this.simulations, then updates the TreeView.
        /// </summary>
        /// <param name="path">Path to the csv file.</param>
        public void ImportCsv(string path = "")
        {
            explorerPresenter.MainPresenter.ShowMessage("", Simulation.ErrorLevel.Error);
            
            if (path == "") path = ApsimNG.Properties.Settings.Default["OutputDir"] + "\\factors.csv";
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
                view.Populate(simulations);
                explorerPresenter.MainPresenter.ShowMessage("Successfully imported data from " + path, Simulation.ErrorLevel.Information);
            }
            catch (Exception e)
            {
                explorerPresenter.MainPresenter.ShowMessage(e.ToString(), Simulation.ErrorLevel.Error);
            }
        }
    }
}
