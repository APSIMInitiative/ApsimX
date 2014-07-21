// -----------------------------------------------------------------------
// <copyright file="InitialWaterPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using Interfaces;
    using Models.Soils;
    using Models.Core;
    using Models.Graph;

    /// <summary>
    /// The presenter class for populating an InitialWater view with an InitialWater model.
    /// </summary>
    public class InitialWaterPresenter : IPresenter
    {
        /// <summary>
        /// The initial water model.
        /// </summary>
        private InitialWater initialWater;

        /// <summary>
        /// The initial water view;
        /// </summary>
        private IInitialWaterView initialWaterView;

        /// <summary>
        /// The parent explorer presenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// A reference to the 'graphPresenter' responsible for our graph.
        /// </summary>
        private GraphPresenter graphPresenter;

        /// <summary>
        /// Our graph.
        /// </summary>
        private Graph graph;

        /// <summary>
        /// Attach the view to the model.
        /// </summary>
        /// <param name="model">The initial water model</param>
        /// <param name="view">The initial water view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.initialWater = model as InitialWater;
            this.initialWaterView = view as IInitialWaterView;
            this.explorerPresenter = explorerPresenter as ExplorerPresenter;

            this.initialWaterView.RelativeToCrops = this.initialWater.RelativeToCrops;
            this.ConnectViewEvents();
            this.PopulateView();

            this.explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;

            // Populate the graph.
            this.graph = Utility.Graph.CreateGraphFromResource(model.GetType().Name + "Graph");
            this.initialWater.Children.Add(this.graph);
            this.graphPresenter = new GraphPresenter();
            this.graphPresenter.Attach(this.graph, this.initialWaterView.Graph, this.explorerPresenter);
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            this.DisconnectViewEvents();
            this.initialWater.Children.Remove(this.graph);
        }

        /// <summary>
        /// Populate all controls on the view.
        /// </summary>
        private void PopulateView()
        {
            this.DisconnectViewEvents();
            this.initialWaterView.PercentFull = Convert.ToInt32((this.initialWater.FractionFull * 100));
            this.initialWaterView.PAW = (int)this.initialWater.PAW;
            if (double.IsNaN(this.initialWater.DepthWetSoil))
                this.initialWaterView.DepthOfWetSoil = int.MinValue;
            else
                this.initialWaterView.DepthOfWetSoil = (int)this.initialWater.DepthWetSoil / 10; // mm to cm
            this.initialWaterView.FilledFromTop = this.initialWater.PercentMethod == InitialWater.PercentMethodEnum.FilledFromTop;
            this.initialWaterView.RelativeTo = this.initialWater.RelativeTo;
            if (this.initialWaterView.RelativeTo == string.Empty)
                this.initialWaterView.RelativeTo = "LL15";
            this.ConnectViewEvents();

            // Refresh the graph.
            if (this.graph != null)
                this.graphPresenter.DrawGraph();
        }

        /// <summary>
        /// Connect all events from the view.
        /// </summary>
        private void ConnectViewEvents()
        {
            this.initialWaterView.OnDepthWetSoilChanged += this.OnDepthWetSoilChanged;
            this.initialWaterView.OnFilledFromTopChanged += this.OnFilledFromTopChanged;
            this.initialWaterView.OnPAWChanged += this.OnPAWChanged;
            this.initialWaterView.OnPercentFullChanged += this.OnPercentFullChanged;
            this.initialWaterView.OnRelativeToChanged += this.OnRelativeToChanged;
        }

        /// <summary>
        /// Disconnect all view events.
        /// </summary>
        private void DisconnectViewEvents()
        {
            this.initialWaterView.OnDepthWetSoilChanged -= this.OnDepthWetSoilChanged;
            this.initialWaterView.OnFilledFromTopChanged -= this.OnFilledFromTopChanged;
            this.initialWaterView.OnPAWChanged -= this.OnPAWChanged;
            this.initialWaterView.OnPercentFullChanged -= this.OnPercentFullChanged;
            this.initialWaterView.OnRelativeToChanged -= this.OnRelativeToChanged;
        }

        /// <summary>
        /// The relative to field in the view has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnRelativeToChanged(object sender, System.EventArgs e)
        {
            Commands.ChangePropertyCommand command = new Commands.ChangePropertyCommand(
                this.initialWater, "RelativeTo", this.initialWaterView.RelativeTo);

            this.explorerPresenter.CommandHistory.Add(command);
        }

        /// <summary>
        /// The percent full field in the view has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnPercentFullChanged(object sender, System.EventArgs e)
        {
            double fractionFull = (this.initialWaterView.PercentFull * 1.0) / 100;
            Commands.ChangePropertyCommand command = new Commands.ChangePropertyCommand(
                this.initialWater, "FractionFull", fractionFull);

            this.explorerPresenter.CommandHistory.Add(command);
        }

        /// <summary>
        /// The PAW field in the view has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnPAWChanged(object sender, System.EventArgs e)
        {
            Commands.ChangePropertyCommand command = new Commands.ChangePropertyCommand(
                this.initialWater, "PAW", Convert.ToDouble(this.initialWaterView.PAW));

            this.explorerPresenter.CommandHistory.Add(command);
        }

        /// <summary>
        /// The filled from top field in the view has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnFilledFromTopChanged(object sender, System.EventArgs e)
        {
            InitialWater.PercentMethodEnum percentMethod;
            if (this.initialWaterView.FilledFromTop)
            {
                percentMethod = InitialWater.PercentMethodEnum.FilledFromTop;
            }
            else
            {
                percentMethod = InitialWater.PercentMethodEnum.EvenlyDistributed;
            }

            Commands.ChangePropertyCommand command = new Commands.ChangePropertyCommand(
                this.initialWater, "PercentMethod", percentMethod);

            this.explorerPresenter.CommandHistory.Add(command);
        }

        /// <summary>
        /// The depth of wet soil field in the view has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnDepthWetSoilChanged(object sender, System.EventArgs e)
        {
            double depthOfWetSoil = Convert.ToDouble(this.initialWaterView.DepthOfWetSoil) * 10; // cm to mm
            Commands.ChangePropertyCommand command = new Commands.ChangePropertyCommand(
                this.initialWater, "DepthWetSoil", depthOfWetSoil);

            this.explorerPresenter.CommandHistory.Add(command);
        }

        /// <summary>
        /// The model has changed. Update the view.
        /// </summary>
        /// <param name="changedModel">The model that has changed.</param>
        void OnModelChanged(object changedModel)
        {
            if (changedModel == this.initialWater)
                this.PopulateView();
        }
    }
}
