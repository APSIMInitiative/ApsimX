using System;
using Models.Core;
using UserInterface.EventArguments;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    public class UpdatePresenter : IPresenter, ISubPresenter
    {
        /// <summary>The model</summary>
        private IModel _model;

        /// <summary>The attached view.</summary>
        private ButtonView _view;

        /// <summary>The explorer presenter controlling the tab's contents.</summary>
        private ExplorerPresenter _explorerPresenter;

        /// <summary>
        /// Flag to record if Presenter is currently listening for events.
        /// Prevents event listeners from being doubled up when used as sub 
        /// presenter.
        /// </summary>
        private bool _eventsConnected = false;

        /// <summary>Invoked when the user clicks the button</summary>
        public event EventHandler<EventArgsValue> Click;

        /// <summary>Attach the model to the view.</summary>
        /// <param name="model">The model.</param>
        /// <param name="view">The view.</param>
        /// <param name="parentPresenter">The explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter parentPresenter)
        {
            _model = model as IModel;
            _view = view as ButtonView;
            _explorerPresenter = parentPresenter;
            _view.Text = "Update";
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
                _view.Clicked += OnClick;
                _eventsConnected = true;
            }
        }

        /// <summary>Disconnect all widget events.</summary>
        public void DisconnectEvents()
        {
            if (_eventsConnected)
            {
                _view.Clicked -= OnClick;
                _eventsConnected = false;
            }
        }

        /// <summary>Refresh the presenter</summary>
        public void Refresh()
        {
            //does nothing - here for interface
            //button has nothing to update
        }

        /// <summary>
        /// Event handler for changing the selected row of the list
        /// </summary>
        private void OnClick(object sender, EventArgs e)
        {
            if (_model is IGenerateNodes generator)
            {
                generator.DeleteNodes();
                generator.CreateNodes();
                _explorerPresenter.RebuildTree();
                if (Click != null)
                    Click.Invoke(this, new EventArgsValue(0));
            }
        }
    }
}