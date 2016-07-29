// -----------------------------------------------------------------------
// <copyright file="CultivarPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using EventArguments;
    using Models.Core;
    using Models.PMF;
    using System;
    using System.Reflection;
    using Views;

    /// <summary>
    /// A presenter class for showing a cultivar.
    /// </summary>
    public class CultivarPresenter : IPresenter
    {
        /// <summary>The cultivar model</summary>
        private Cultivar cultivar;

        /// <summary>The cultivar view</summary>
        private IEditorView view;

        /// <summary>The parent explorer presenter</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>Attach the cultivar model to the cultivar view</summary>
        /// <param name="model">The mode</param>
        /// <param name="view">The view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.cultivar = model as Cultivar;
            this.view = view as IEditorView;
            this.explorerPresenter = explorerPresenter;

            this.view.Lines = this.cultivar.Commands;

            this.view.LeaveEditor += this.OnCommandsChanged;
            this.view.ContextItemsNeeded += this.OnContextItemsNeeded;
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>Detach the model from the view</summary>
        public void Detach()
        {
            this.view.LeaveEditor -= this.OnCommandsChanged;
            this.view.ContextItemsNeeded -= this.OnContextItemsNeeded;
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
        }

        /// <summary>The user has changed the commands</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCommandsChanged(object sender, EventArgs e)
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

            Commands.ChangeProperty command = new Commands.ChangeProperty(this.cultivar, "Commands", this.view.Lines);
            this.explorerPresenter.CommandHistory.Add(command);

            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>User has pressed a '.' in the commands window - supply context items.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnContextItemsNeeded(object sender, NeedContextItemsArgs e)
        {
            e.AllItems.AddRange(NeedContextItemsArgs.ExamineModelForNames(this.cultivar, e.ObjectName, true, true, false));
        }

        /// <summary>The cultivar model has changed probably because of an undo.</summary>
        /// <param name="changedModel">The model that was changed.</param>
        private void OnModelChanged(object changedModel)
        {
            this.view.Lines = this.cultivar.Commands;
        }
    }
}
