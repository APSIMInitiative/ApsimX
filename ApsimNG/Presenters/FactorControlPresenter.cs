using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Views;
using Models.Core;
using Models.Factorial;
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

            //List<Factors> factors = ((IModel)model).Children[0]);
            List<string> factorNames = model.GetSimulationNames().ToList();
            List<string> experiments = new List<string> { model.Name };

            view.Initialise(experiments, factorNames);

            
            Factors f = (Factors)((IModel)experiment).Children[0];
        }

        public void Detach()
        {

        }
    }
}
