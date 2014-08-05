using Models.Graph;
using UserInterface.Views;
using System;

namespace UserInterface.Presenters
{
    /// <summary>
    /// This presenter connects an instance of a Model.Graph.Axis with a 
    /// UserInterface.Views.AxisView
    /// </summary>
    class AxisPresenter : IPresenter
    {
        private Axis Axis;
        private IAxisView View;
        private ExplorerPresenter ExplorerPresenter;

        /// <summary>
        /// Attach the specified Model and View.
        /// </summary>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            Axis = model as Axis;
            View = view as AxisView;
            ExplorerPresenter = explorerPresenter;

            // Trap change event from the model.
            ExplorerPresenter.CommandHistory.ModelChanged += OnModelChanged;

            // Trap events from the view.
            View.OnTitleChanged += OnTitleChanged;
            View.OnInvertedChanged += OnInvertedChanged;

            // Tell the view to populate the axis.
            View.Populate(Axis.Title, Axis.Inverted);
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            // Trap change event from the model.
            ExplorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;

            // Trap events from the view.
            View.OnTitleChanged -= OnTitleChanged;
        }
        
        /// <summary>
        /// The 'Model' has changed so we need to update the 'View'. Usually the result of an 'Undo' or 'Redo'
        /// </summary>
        private void OnModelChanged(object Model)
        {
            if (Model == Axis)
                View.Populate(Axis.Title, Axis.Inverted);
        }

        /// <summary>
        /// The user has changed the title field on the form. Need to tell the model this via
        /// executing a command.
        /// </summary>
        void OnTitleChanged(string NewText)
        {
            ExplorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(Axis, "Title", NewText));
        }

        /// <summary>
        /// User has clicked inverted - change the property in the model.
        /// </summary>
        void OnInvertedChanged(bool Inverted)
        {
            ExplorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(Axis, "Inverted", Inverted));
        }
    }
}
