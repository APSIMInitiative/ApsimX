// -----------------------------------------------------------------------
// <copyright file="StockPresenter.cs" company="CSIRO">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using Interfaces;
    using Models.GrazPlan;
    using Views;

    /// <summary>
    /// A presenter class for the Stock model
    /// </summary>
    public class StockPresenter : IPresenter
    {
        /// <summary>
        /// The parent explorer presenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        private Stock stock;

        /// <summary>
        /// The initial Stock view;
        /// </summary>
        private IStockView stockView;

        /// <summary>
        /// Attach the model and view to this presenter.
        /// </summary>
        /// <param name="model">The initial stock model</param>
        /// <param name="view">The stock view to work with</param>
        /// <param name="explrPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explrPresenter)
        {
            stock = model as Stock;
            stockView = view as StockView;
            explorerPresenter = explrPresenter;

            stockView.GetGenoParams += OnGetGenoParams;

            ConnectViewEvents();
            PopulateView();
            
            explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Detach the model and view from this presenter.
        /// </summary>
        public void Detach()
        {
            DisconnectViewEvents();
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;

            stock.GenoTypes = stockView.Genotypes;  // copies back to the model

        }

        /// <summary>
        /// Populate the view object
        /// </summary>
        public void PopulateView()
        {
            PopulateGenotypes();
            stockView.SetValues();
        }

        /// <summary>
        /// Connect all events from the view.
        /// </summary>
        private void ConnectViewEvents()
        {

        }

        /// <summary>
        /// Disconnect all view events.
        /// </summary>
        private void DisconnectViewEvents()
        {

        }

        /// <summary>
        /// The model has changed. Update the view.
        /// </summary>
        /// <param name="changedModel">The model that has changed.</param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == stock)
            {
                PopulateView();
            }
        }

        /// <summary>
        /// Initialise the list of genotypes
        /// </summary>
        private void PopulateGenotypes()
        {
            stockView.Genotypes = stock.GenoTypes;  // copies the init value array into the View
        }

        private void OnGetGenoParams(object sender, GenotypeInitArgs e)
        {
            AnimalParamSet tempParams = stock.ParamsFromGenotypeInits(e.ParamSet, e.Genotypes, e.index);
            stockView.SetGenoParams(tempParams);
        }
    }
}
