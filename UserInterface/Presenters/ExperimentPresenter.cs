using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Factorial;
using UserInterface.Views;
namespace UserInterface.Presenters
{
    /// <summary>
    /// Connects a Experiment model with an readonly memo view.
    /// </summary>
    public class ExperimentPresenter : IPresenter
    {
        private Experiment Experiment;
        private IMemoView ListView;

        public void Attach(object Model, object View, ExplorerPresenter explorerPresenter)
        {
            Experiment = Model as Experiment;
            ListView = View as IMemoView;

            ListView.MemoLines = Experiment.Names();
            ListView.LabelText = "Listed below are names of the " + ListView.MemoLines.Length.ToString() + " simulations that this experiment will create";
            ListView.ReadOnly = true;
        }

        public void Detach()
        {

        }
    }
}
