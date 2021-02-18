namespace UserInterface.Presenters
{
    using System;
    using Interfaces;
    using Models;
    using Models.Soils;
    using Commands;
    using System.Globalization;

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

            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;

            // Populate the graph.
            this.graph = Utility.Graph.CreateGraphFromResource("ApsimNG.Resources.InitialWaterGraph.xml");
            this.graph.Parent = this.initialWater.Parent;
            this.graphPresenter = new GraphPresenter();
            this.graphPresenter.Attach(this.graph, this.initialWaterView.Graph, this.explorerPresenter);
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            graphPresenter.Detach();
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
            this.DisconnectViewEvents();
        }

        /// <summary>
        /// Populate all controls on the view.
        /// </summary>
        private void PopulateView()
        {
            DisconnectViewEvents();
            initialWaterView.PercentFull = Convert.ToInt32(initialWater.FractionFull * 100, CultureInfo.InvariantCulture);
            initialWaterView.PAW = (int)Math.Round(initialWater.PAW);
            if (double.IsNaN(initialWater.DepthWetSoil))
            {
                initialWaterView.FilledByDepth = false;
                initialWaterView.DepthOfWetSoil = int.MinValue;
            }
            else
            {
                initialWaterView.FilledByDepth = true;
                initialWaterView.DepthOfWetSoil = (int)Math.Round(initialWater.DepthWetSoil / 10); // mm to cm
            }

            initialWaterView.FilledFromTop = initialWater.PercentMethod == InitialWater.PercentMethodEnum.FilledFromTop;
            initialWaterView.RelativeTo = initialWater.RelativeTo;
            if (initialWaterView.RelativeTo == string.Empty)
            {
                initialWaterView.RelativeTo = "LL15";
            }

            ConnectViewEvents();

            // Refresh the graph.
            if (graph != null)
            {
                graphPresenter.DrawGraph();
            }
        }

        /// <summary>
        /// Connect all events from the view.
        /// </summary>
        private void ConnectViewEvents()
        {
            initialWaterView.OnDepthWetSoilChanged += OnDepthWetSoilChanged;
            initialWaterView.OnFilledFromTopChanged += OnFilledFromTopChanged;
            initialWaterView.OnPAWChanged += OnPAWChanged;
            initialWaterView.OnPercentFullChanged += OnPercentFullChanged;
            initialWaterView.OnRelativeToChanged += OnRelativeToChanged;
            initialWaterView.OnSpecifierChanged += OnSpecifierChanged;
        }

        /// <summary>
        /// Disconnect all view events.
        /// </summary>
        private void DisconnectViewEvents()
        {
            initialWaterView.OnDepthWetSoilChanged -= OnDepthWetSoilChanged;
            initialWaterView.OnFilledFromTopChanged -= OnFilledFromTopChanged;
            initialWaterView.OnPAWChanged -= OnPAWChanged;
            initialWaterView.OnPercentFullChanged -= OnPercentFullChanged;
            initialWaterView.OnRelativeToChanged -= OnRelativeToChanged;
            initialWaterView.OnSpecifierChanged -= OnSpecifierChanged;
        }

        /// <summary>
        /// The relative to field in the view has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnRelativeToChanged(object sender, EventArgs e)
        {
            try
            {
                ChangeProperty command = new ChangeProperty(initialWater, "RelativeTo", initialWaterView.RelativeTo);
                explorerPresenter.CommandHistory.Add(command);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// The percent full field in the view has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnPercentFullChanged(object sender, EventArgs e)
        {
            try
            {
                double fractionFull = (initialWaterView.PercentFull * 1.0) / 100;
                ChangeProperty command = new ChangeProperty(initialWater, "FractionFull", fractionFull);
                explorerPresenter.CommandHistory.Add(command);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// The PAW field in the view has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnPAWChanged(object sender, EventArgs e)
        {
            try
            {
                ChangeProperty command = new ChangeProperty(initialWater, "PAW", Convert.ToDouble(initialWaterView.PAW, System.Globalization.CultureInfo.InvariantCulture));
                explorerPresenter.CommandHistory.Add(command);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// The filled from top field in the view has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnFilledFromTopChanged(object sender, EventArgs e)
        {
            try
            {
                InitialWater.PercentMethodEnum percentMethod;
                if (initialWaterView.FilledFromTop)
                {
                    percentMethod = InitialWater.PercentMethodEnum.FilledFromTop;
                }
                else
                {
                    percentMethod = InitialWater.PercentMethodEnum.EvenlyDistributed;
                }

                ChangeProperty command = new ChangeProperty(initialWater, "PercentMethod", percentMethod);

                explorerPresenter.CommandHistory.Add(command);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// The depth of wet soil field in the view has changed.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnDepthWetSoilChanged(object sender, EventArgs e)
        {
            try
            {
                double depthOfWetSoil;
                if (initialWaterView.DepthOfWetSoil == int.MinValue)
                    depthOfWetSoil = Double.NaN;
                else
                    depthOfWetSoil = Convert.ToDouble(initialWaterView.DepthOfWetSoil, System.Globalization.CultureInfo.InvariantCulture) * 10; // cm to mm
                ChangeProperty command = new ChangeProperty(initialWater, "DepthWetSoil", depthOfWetSoil);
                explorerPresenter.CommandHistory.Add(command);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// The method used to specify initial water has changed
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnSpecifierChanged(object sender, EventArgs e)
        {
            try
            {
                double depthOfWetSoil;
                if (initialWaterView.DepthOfWetSoil == int.MinValue)
                    depthOfWetSoil = Double.NaN;
                else
                    depthOfWetSoil = initialWater.TotalSoilDepth() * Math.Min(1.0, initialWater.FractionFull);

                // The InitialWater model uses the value of DepthWetSoil as a flag
                // to inidicate whether specification is by depth or by fraction
                ChangeProperty command = new ChangeProperty(initialWater, "DepthWetSoil", depthOfWetSoil);

                explorerPresenter.CommandHistory.Add(command);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// The model has changed. Update the view.
        /// </summary>
        /// <param name="changedModel">The model that has changed.</param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == this.initialWater)
            {
                PopulateView();
            }
        }
    }
}
