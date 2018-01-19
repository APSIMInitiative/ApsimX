using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Views;
using Models.Core;
using Models.Factorial;
using System.Collections;

namespace UserInterface.Presenters
{
    public class FactorControlPresenter : IPresenter
    {

        private Experiment model;
        private FactorControlView view;
        private ExplorerPresenter explorerPresenter;
        private List<string> headers;
        private List<Tuple<string, List<string>, bool>> simulations;

        public void Attach(object experiment, object viewer, ExplorerPresenter explorerPresenter)
        {
            model = (Experiment)experiment;
            view = (FactorControlView)viewer;
            view.Presenter = this;

            this.explorerPresenter = explorerPresenter;
            var simNames = model.GetSimulationNames().ToArray();


            headers = GetHeaderNames(model.AllCombinations());
            simulations = GetTableData(model.AllCombinations());

            view.Initialise(headers, simulations);

            
            
            
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
            view.Initialise(headers, simulations);
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
    }
}
