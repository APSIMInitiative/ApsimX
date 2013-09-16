using Model.Components.Graph;
using UserInterface.Views;

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
        private CommandHistory CommandHistory;

        /// <summary>
        /// Attach the specified Model and View.
        /// </summary>
        public void Attach(object model, object view, CommandHistory commandHistory)
        {
            Axis = model as Axis;
            View = view as AxisView;
            CommandHistory = commandHistory;

            // Trap change event from the model.
            CommandHistory.ModelChanged += OnModelChanged;

            // Trap events from the view.
            View.OnTitleChanged += OnTitleChanged;

            // Tell the view to populate the axis.
            View.Populate(Axis.Title);
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            // Trap change event from the model.
            CommandHistory.ModelChanged -= OnModelChanged;

            // Trap events from the view.
            View.OnTitleChanged -= OnTitleChanged;
        }
        
        /// <summary>
        /// The 'Model' has changed so we need to update the 'View'. Usually the result of an 'Undo' or 'Redo'
        /// </summary>
        private void OnModelChanged(object Model)
        {
            if (Model == Axis)
                View.Populate(Axis.Title);
        }

        /// <summary>
        /// The user has changed the title field on the form. Need to tell the model this via
        /// executing a command.
        /// </summary>
        void OnTitleChanged(string NewText)
        {
            CommandHistory.Add(new Commands.ChangePropertyCommand(Axis, "Title", NewText));
        }
    }
}
