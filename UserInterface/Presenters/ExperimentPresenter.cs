using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Factorial;
using UserInterface.Views;
using Models.Core;
namespace UserInterface.Presenters
{
    /// <summary>
    /// Connects a Experiment model with an readonly memo view.
    /// </summary>
    public class ExperimentPresenter : IPresenter
    {
        private Experiment Experiment;
        private IMemoView ListView;
        private ExplorerPresenter ExplorerPresenter;

        public void Attach(object Model, object View, ExplorerPresenter explorerPresenter)
        {
            Experiment = Model as Experiment;
            ListView = View as IMemoView;
            ExplorerPresenter = explorerPresenter;

            ListView.MemoLines = Experiment.Names();
            ListView.LabelText = "Listed below are names of the " + ListView.MemoLines.Length.ToString() + " simulations that this experiment will create";
            ListView.ReadOnly = true;
            ListView.AddContextAction("Run APSIM", OnRunApsimClick);
        }

        public void Detach()
        {

        }

        /// <summary>
        /// User has clicked RunAPSIM
        /// </summary>
        private void OnRunApsimClick(object sender, EventArgs e)
        {
            try
            {
                Simulation simulation = Experiment.CreateSpecificSimulation(ListView.MemoLines[ListView.CurrentPosition.Y]);
                Commands.RunCommand run = new Commands.RunCommand(Apsim.Parent(Experiment, typeof(Simulations)) as Simulations,
                                                                  simulation,
                                                                  ExplorerPresenter);
                run.Do(null);
            }
            catch (Exception err)
            {
                ExplorerPresenter.ShowMessage(err.Message, Models.DataStore.ErrorLevel.Error);
            }
        }

    }
}
