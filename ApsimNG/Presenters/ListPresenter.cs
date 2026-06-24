using System;
using System.Data;
using Models.Core;
using UserInterface.EventArguments;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    public class ListPresenter : IPresenter, ISubPresenter
    {
        /// <summary>Default number of rows to show (for performance reasons).</summary>
        private const int DEFAULT_MAX = 100;

        /// <summary>The model</summary>
        private IModel _model;

        /// <summary>The attached view.</summary>
        private ExperimentView _view;

        /// <summary>The explorer presenter controlling the tab's contents.</summary>
        private ExplorerPresenter _explorerPresenter;

        /// <summary>
        /// Flag to record if Presenter is currently listening for events.
        /// Prevents event listeners from being doubled up when used as sub 
        /// presenter.
        /// </summary>
        private bool _eventsConnected = false;

        /// <summary>
        /// Max number of rows to show
        /// </summary>
        private int _maxEntries = DEFAULT_MAX;

        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler<EventArgsValue> SelectionChanged;

        /// <summary>Attach the model to the view.</summary>
        /// <param name="model">The model.</param>
        /// <param name="view">The view.</param>
        /// <param name="parentPresenter">The explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter parentPresenter)
        {
            _model = model as IModel;
            _view = view as ExperimentView;
            _explorerPresenter = parentPresenter;

            IListValues list = _model as IListValues;

            // Give the view the default maximum number of simulations to display.
            _view.MaximumNumSimulations.Text = DEFAULT_MAX.ToString();

            ConnectEvents();

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
                _view.List.Changed += OnSelectionChanged;
                _view.MaximumNumSimulations.Leave += OnMaxRowCountChanged;
                _eventsConnected = true;
            }
        }

        /// <summary>Disconnect all widget events.</summary>
        public void DisconnectEvents()
        {
            if (_eventsConnected)
            {
                _view.List.Changed -= OnSelectionChanged;
                _view.MaximumNumSimulations.Leave -= OnMaxRowCountChanged;
                _eventsConnected = false;
            }
        }

        /// <summary>Refresh the grid.</summary>
        public void Refresh()
        {
            DisconnectEvents();
            PopulateView();
            ConnectEvents();
        }

        /// <summary>Populate the view.</summary>
        private void PopulateView()
        {

            // Give the table to the view.
            DataTable data = (_model as IListValues).Rows;

            //Trim the data table down to the max entries to show
            DataTable dataTrimmed = data.Clone();
            for(int i = 0; i < data.Rows.Count && i < _maxEntries; i++)
                dataTrimmed.ImportRow(data.Rows[i]);

            //pass to the view
            _view.List.DataSource = dataTrimmed;

            //update number
            try
            {
                _view.NumberSimulationsLabel.Text = $"Number of Rows: {data.Rows.Count}";
            }
            catch
            {
                _view.NumberSimulationsLabel.Text = $"Number of Rows: 0";
            }
        }

        /// <summary>
        /// Event handler for changing the selected row of the list
        /// </summary>
        private void OnSelectionChanged(object sender, EventArgsValue e)
        {
            SelectionChanged?.Invoke(sender, e);
        }

        /// <summary>
        /// Sets the maximum number of simulations (rows in the view's table) allowed to be displayed at once, then updates the view.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnMaxRowCountChanged(object sender, EventArgs args)
        {
            string input = _view.MaximumNumSimulations.Text;

            bool success = int.TryParse(input, out _maxEntries);
            if (!success || _maxEntries < 0)
                _maxEntries = DEFAULT_MAX;

            PopulateView();
        }
    }
}