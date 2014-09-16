using Models.Graph;
using UserInterface.Views;
using System;
using System.Collections.Generic;

namespace UserInterface.Presenters
{
    /// <summary>
    /// This presenter connects an instance of a Model.Graph.Axis with a 
    /// UserInterface.Views.AxisView
    /// </summary>
    class TitlePresenter : IPresenter
    {
        private Graph Graph;
        private ITitleView View;
        private ExplorerPresenter ExplorerPresenter;

        /// <summary>
        /// Gets or sets a value indicating whether the graph footer should be shown.
        /// </summary>
        public bool ShowCaption { get; set; }

        /// <summary>
        /// Attach the specified Model and View.
        /// </summary>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            Graph = model as Graph;
            View = view as ITitleView;
            ExplorerPresenter = explorerPresenter;

            // Trap change event from the model.
            ExplorerPresenter.CommandHistory.ModelChanged += OnModelChanged;

            // Trap events from the view.
            View.OnTitleChanged += OnTitleChanged;

            // Tell the view to populate the axis.
            PopulateView();
        }

        private void PopulateView()
        {
            if (ShowCaption)
                View.Populate(Graph.Caption);
            else
                View.Populate(Graph.Title);
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
            if (Model == Graph)
                PopulateView();
        }

        /// <summary>
        /// The user has changed the title field on the form. Need to tell the model this via
        /// executing a command.
        /// </summary>
        void OnTitleChanged(string NewText)
        {
            if (ShowCaption)
                ExplorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(Graph, "Caption", NewText));
            else
                ExplorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(Graph, "Title", NewText));
        }

    }
}
