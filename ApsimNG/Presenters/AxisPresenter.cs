namespace UserInterface.Presenters
{
    using System;
    using Models;
    using Views;
    using Interfaces;

    /// <summary>
    /// This presenter connects an instance of a Model.Graph.Axis with a 
    /// UserInterface.Views.AxisView
    /// </summary>
    public class AxisPresenter : IPresenter
    {
        /// <summary>
        /// The axis model
        /// </summary>
        private Axis axis;

        /// <summary>
        /// The axis view
        /// </summary>
        private IAxisView view;

        /// <summary>
        /// The parent explorer presenter
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attach the specified Model and View.
        /// </summary>
        /// <param name="model">The axis model</param>
        /// <param name="view">The axis view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            axis = model as Axis;
            this.view = view as AxisView;
            this.explorerPresenter = explorerPresenter;

            // Trap change event from the model.
            explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
            this.view.IsDateAxis = axis.DateTimeAxis;

            // Tell the view to populate the axis.
            PopulateView();

            // Trap events from the view.
            this.view.TitleChanged += OnTitleChanged;
            this.view.InvertedChanged += OnInvertedChanged;
            this.view.MinimumChanged += OnMinimumChanged;
            this.view.MaximumChanged += OnMaximumChanged;
            this.view.IntervalChanged += OnIntervalChanged;
            this.view.CrossesAtZeroChanged += OnCrossesAtZeroChanged;
        }


        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            // Trap change event from the model.
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;

            // Trap events from the view.
            view.TitleChanged -= OnTitleChanged;
            view.InvertedChanged -= OnInvertedChanged;
            view.MinimumChanged -= OnMinimumChanged;
            view.MaximumChanged -= OnMaximumChanged;
            view.IntervalChanged -= OnIntervalChanged;
            view.CrossesAtZeroChanged -= OnCrossesAtZeroChanged;
        }

        /// <summary>
        /// Populate the view.
        /// </summary>
        private void PopulateView()
        {
            view.Title = axis.Title;
            view.Inverted = axis.Inverted;
            view.CrossesAtZero = axis.CrossesAtZero;
            view.SetMinimum(axis.Minimum, axis.DateTimeAxis);
            view.SetMaximum(axis.Maximum, axis.DateTimeAxis);
            view.SetInterval(axis.Interval, axis.DateTimeAxis);
        }
        
        /// <summary>
        /// The 'Model' has changed so we need to update the 'View'. Usually the result of an 'Undo' or 'Redo'
        /// </summary>
        /// <param name="model">The model that was changed.</param>
        private void OnModelChanged(object model)
        {
            if (model == axis)
            {
                PopulateView();
            }
        }

        /// <summary>
        /// The user has changed the title field on the form. Need to tell the model this via
        /// executing a command.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnTitleChanged(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(axis, "Title", view.Title));
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has clicked inverted - change the property in the model.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnInvertedChanged(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(axis, "Inverted", view.Inverted));
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has changed the minimum
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnMinimumChanged(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(axis, "Minimum", view.Minimum));
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has changed the maximum
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnMaximumChanged(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(axis, "Maximum", view.Maximum));
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has changed the interval
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnIntervalChanged(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(axis, "Interval", view.Interval));
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has changed the crosses at zero checkbox,
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCrossesAtZeroChanged(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(axis, "CrossesAtZero", view.CrossesAtZero));
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }
    }
}
