// -----------------------------------------------------------------------
// <copyright file="RotBubbleChartPresenter.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EventArguments;
    using Models;
    using Models.Core;
    using Models.Interfaces;
    using Models.Management;
    using Views;



    /// <summary>
    /// Presenter for the rotation bubble chart component
    /// </summary>
    public class RugPlotPresenter : IPresenter 
    {
        /// <summary>
        /// The view for the manager
        /// </summary>
        private RugPlotView view;

        /// <summary>The explorer presenter used</summary>
        private ExplorerPresenter presenter;

        /// <summary>The model used</summary>
        private RotationRugplot model;

        /// <summary>
        /// Handles generation of completion options for the view.
        /// </summary>

        /// <summary>
        /// Attach the Manager model and ManagerView to this presenter.
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="presenter">The explorer presenter being used</param>
        public void Attach(object model, object view, ExplorerPresenter presenter)
        {
            this.view = view as RugPlotView;
            this.presenter = presenter;
            this.model = model as RotationRugplot;

            this.view.SimulationDropDown.Changed += OnSimulationNameChanged;

            RefreshView(true);
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.view.SimulationDropDown.Changed -= OnSimulationNameChanged;
        }

        /// <summary>
        /// The model has been changed. Refresh the view.
        /// </summary>
        /// <param name="changedModel"></param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == model)
                RefreshView(true);
        }

        /// <summary>
        /// Refresh the view with the model's current state.
        /// </summary>
        private void RefreshView(bool setSimulationName)
        {
            view.SetModel(model, setSimulationName);
        }
        /// <summary>Handles the SimulationNameChanged event of the view control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnSimulationNameChanged(object sender, EventArgs e)
        {
            model.SetSimulationName((sender as DropDownView).SelectedValue);
            RefreshView(false); 
        }
    }
}