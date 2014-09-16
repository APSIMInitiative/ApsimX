// -----------------------------------------------------------------------
// <copyright file="AxisPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using Models.Graph;
    using Views;

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
            this.axis = model as Axis;
            this.view = view as AxisView;
            this.explorerPresenter = explorerPresenter;

            // Trap change event from the model.
            explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;

            // Trap events from the view.
            this.view.TitleChanged += this.OnTitleChanged;
            this.view.InvertedChanged += this.OnInvertedChanged;
            this.view.MinimumChanged += this.OnMinimumChanged;
            this.view.MaximumChanged += this.OnMaximumChanged;
            this.view.IntervalChanged += this.OnIntervalChanged;

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
            this.view.TitleChanged -= this.OnTitleChanged;
            this.view.InvertedChanged -= this.OnInvertedChanged;
            this.view.MinimumChanged -= this.OnMinimumChanged;
            this.view.MaximumChanged -= this.OnMaximumChanged;
            this.view.IntervalChanged -= this.OnIntervalChanged;
        }

        /// <summary>
        /// Populate the view.
        /// </summary>
        private void PopulateView()
        {
            this.view.Title = this.axis.Title;
            this.view.Inverted = this.axis.Inverted;
            this.view.Minimum = this.axis.Minimum;
            this.view.Maximum = this.axis.Maximum;
            this.view.Interval = this.axis.Interval;
        }
        
        /// <summary>
        /// The 'Model' has changed so we need to update the 'View'. Usually the result of an 'Undo' or 'Redo'
        /// </summary>
        /// <param name="model">The model that was changed.</param>
        private void OnModelChanged(object model)
        {
            if (model == this.axis)
                this.PopulateView();
        }

        /// <summary>
        /// The user has changed the title field on the form. Need to tell the model this via
        /// executing a command.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnTitleChanged(object sender, EventArgs e)
        {
            this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.axis, "Title", this.view.Title));
        }

        /// <summary>
        /// User has clicked inverted - change the property in the model.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnInvertedChanged(object sender, EventArgs e)
        {
            this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.axis, "Inverted", this.view.Inverted));
        }

        /// <summary>
        /// User has changed the minimum
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnMinimumChanged(object sender, EventArgs e)
        {
            this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.axis, "Minimum", this.view.Minimum));
        }

        /// <summary>
        /// User has changed the maximum
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnMaximumChanged(object sender, EventArgs e)
        {
            this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.axis, "Maximum", this.view.Maximum));
        }

        /// <summary>
        /// User has changed the interval
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnIntervalChanged(object sender, EventArgs e)
        {
            this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.axis, "Interval", this.view.Interval));
        }
    }
}
