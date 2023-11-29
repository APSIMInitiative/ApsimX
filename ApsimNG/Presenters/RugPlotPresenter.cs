// -----------------------------------------------------------------------
// <copyright file="RotBubbleChartPresenter.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;
    using APSIM.Shared.Utilities;
    using Commands;
    using EventArguments;
    using EventArguments.DirectedGraph;
    using Models;
    using Models.Core;
    using Models.Interfaces;
    using Models.Management;
    using Views;
    using Interfaces;
    using APSIM.Shared.Graphing;

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
        private rotationRugplot model;

        /// <summary>
        /// Handles generation of completion options for the view.
        /// </summary>

        /// <summary>
        /// Used by the intellisense to keep track of which editor the user is currently using.
        /// Without this, it's difficult to know which editor (variables or events) to
        /// insert an intellisense item into.
        /// </summary>
        private PropertyPresenter propertiesPresenter = new PropertyPresenter();

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
            this.model = model as rotationRugplot;

            propertiesPresenter.Attach(this.model, this.view.PropertiesView, presenter);
            RefreshView();
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            propertiesPresenter.Detach();
        }

        /// <summary>
        /// The model has been changed. Refresh the view.
        /// </summary>
        /// <param name="changedModel"></param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == model)
                RefreshView();
        }

        /// <summary>
        /// Refresh the view with the model's current state.
        /// </summary>
        private void RefreshView()
        {
            view.SetModel(model);
            propertiesPresenter.RefreshView(model);
        }
    }
}