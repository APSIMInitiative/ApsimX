using APSIM.Shared.Utilities;
using UserInterface.Commands;
using UserInterface.Interfaces;
using Models;
using Models.Core;
using Models.Core.Run;
using Models.Factorial;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    public class ListPresenter : IPresenter, ISubPresenter
    {
        /// <summary>The model</summary>
        private IModel _model;

        /// <summary>The attached view.</summary>
        private ExperimentView view;

        /// <summary>The explorer presenter controlling the tab's contents.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>By default, only display this many simulations (for performance reasons).</summary>
        private const int DefaultMaxSims = 50;

        /// <summary>
        /// Flag to record if Presenter is currently listening for events.
        /// Prevents event listeners from being doubled up when used as sub 
        /// presenter.
        /// </summary>
        private bool _eventsConnected = false;

        /// <summary>Attach the model to the view.</summary>
        /// <param name="model">The model.</param>
        /// <param name="viewObject">The view.</param>
        /// <param name="parentPresenter">The explorer presenter.</param>
        public void Attach(object model, object viewObject, ExplorerPresenter parentPresenter)
        {
            _model = model as IModel;
            view = viewObject as ExperimentView;
            explorerPresenter = parentPresenter;

            ConnectEvents();

            IListValues list = _model as IListValues;

            // Give the view the default maximum number of simulations to display.
            view.MaximumNumSimulations.Text = DefaultMaxSims.ToString();
            view.NumberSimulationsLabel.Text = $"Number of simulations: {list.Data.Rows.Count}";

            // Populate the view.
            PopulateView();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            DisconnectEvents();
        }

        /// <summary>Connect all widget events.</summary>
        public void ConnectEvents()
        {
            if (!_eventsConnected)
            {
                _eventsConnected = true;
            }
        }

        /// <summary>Disconnect all widget events.</summary>
        public void DisconnectEvents()
        {
            if (_eventsConnected)
            {
                _eventsConnected = false;
            }
        }

        /// <summary>Refresh the grid.</summary>
        public void Refresh()
        {
            DisconnectEvents();
            ConnectEvents();
        }

        /// <summary>Populate the view.</summary>
        private void PopulateView()
        {
            // Give the table to the view.
            view.List.DataSource = (_model as IListValues).Data;
        }
    }
}