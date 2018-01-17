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
    class FactorControlPresenter : IPresenter
    {

        private Experiment model;
        private FactorControlView view;
        private ExplorerPresenter explorerPresenter;

        public void Attach(object experiment, object viewer, ExplorerPresenter explorerPresenter)
        {
            model = (Experiment)experiment;
            view = (FactorControlView)viewer;
            this.explorerPresenter = explorerPresenter;
            var simNames = model.GetSimulationNames().ToArray();


            List<List<Tuple<string, string, string>>> allCombinations = new List<List<Tuple<string, string, string>>>();
            int n = 0;
            foreach (List<FactorValue> factors in model.AllCombinations())
            {
                List<Tuple<string, string, string>> data = new List<Tuple<string, string, string>>();
                foreach (FactorValue factor in factors)
                {
                    object val = factor.Values[0];
                    string value = val.GetType() == typeof(string) ? (string)val : ((Model)val).Name;
                    string name = "";
                    foreach (FactorValue fv in factors) name += fv.Name;
                    Tuple<string, string, string> f = new Tuple<string, string, string>(factor.Factor.Name, value, name);
                    data.Add(f);
                    n++;
                }
                allCombinations.Add(data);
            }

            view.Initialise(allCombinations);

            
            
            
        }

        public void Detach()
        {

        }
    }
}
