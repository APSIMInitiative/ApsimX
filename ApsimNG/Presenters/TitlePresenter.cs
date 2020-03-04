namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using Models;
    using Views;

    /// <summary>
    /// This presenter connects an instance of a Model.Graph.Axis with a 
    /// UserInterface.Views.AxisView
    /// </summary>
    public class TitlePresenter : IPresenter
    {
        /// <summary>
        /// The graph object
        /// </summary>
        private Graph graph;

        /// <summary>
        /// The view object
        /// </summary>
        private ITitleView view;

        /// <summary>
        /// The explorer presenter used
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Gets or sets a value indicating whether the graph footer should be shown.
        /// </summary>
        public bool ShowCaption { get; set; }

        /// <summary>
        /// Attach the specified Model and View.
        /// </summary>
        /// <param name="model">The model to use</param>
        /// <param name="view">The view for this presenter</param>
        /// <param name="explorerPresenter">The explorer presenter used</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.graph = model as Graph;
            this.view = view as ITitleView;
            this.explorerPresenter = explorerPresenter;

            // Trap change event from the model.
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;

            // Trap events from the view.
            this.view.OnTitleChanged += this.OnTitleChanged;

            // Tell the view to populate the axis.
            this.PopulateView();
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            // Trap change event from the model.
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

            // Trap events from the view.
            this.view.OnTitleChanged -= this.OnTitleChanged;
        }

        /// <summary>
        /// The user has changed the title field on the form. Need to tell the model this via
        /// executing a command. 
        /// </summary>
        /// <param name="newText">The new title</param>
        public void OnTitleChanged(string newText)
        {
            try
            {
                if (ShowCaption && newText != "Double click to add a caption")
                    explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(graph, "Caption", newText));
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Populate the view object
        /// </summary>
        private void PopulateView()
        {
            if (ShowCaption)
                if (graph.Caption != "Double click to add a caption")
                    view.Populate(graph.Caption);
            else
                view.Populate(graph.Name);
        }

        /// <summary>
        /// The 'Model' has changed so we need to update the 'View'. Usually the result of an 'Undo' or 'Redo'
        /// </summary>
        /// <param name="model">The model object</param>
        private void OnModelChanged(object model)
        {
            if (model == graph)
                PopulateView();
        }
    }
}
