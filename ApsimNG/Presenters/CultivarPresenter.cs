// -----------------------------------------------------------------------
// <copyright file="CultivarPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using EventArguments;
    using Models.PMF;
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

        /// <summary>The intellisense object.</summary>
        private IntellisensePresenter intellisense;

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
            intellisense = new IntellisensePresenter(this.view as ViewBase);
            intellisense.ItemSelected += (sender, e) => this.view.InsertCompletionOption(e.ItemSelected, e.TriggerWord);

            this.view.LeaveEditor += this.OnCommandsChanged;
            this.view.ContextItemsNeeded += this.OnContextItemsNeeded;
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>Detach the model from the view</summary>
        public void Detach()
        {
            this.OnCommandsChanged(this, new EventArgs());
            this.view.LeaveEditor -= this.OnCommandsChanged;
            this.view.ContextItemsNeeded -= this.OnContextItemsNeeded;
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
            intellisense.Cleanup();
        }

        /// <summary>The user has changed the commands</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCommandsChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.view.Lines != this.cultivar.Commands)
                {
                    this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

                    Commands.ChangeProperty command = new Commands.ChangeProperty(this.cultivar, "Commands", this.view.Lines);
                    this.explorerPresenter.CommandHistory.Add(command);

                    this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
                }
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>User has pressed a '.' in the commands window - supply context items.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnContextItemsNeeded(object sender, NeedContextItemsArgs e)
        {
            try
            {
                if (intellisense.GenerateGridCompletions(e.Code, e.Offset, cultivar, true, false, false, e.ControlSpace))
                    intellisense.Show(e.Coordinates.Item1, e.Coordinates.Item2);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
            
        }

        /// <summary>The cultivar model has changed probably because of an undo.</summary>
        /// <param name="changedModel">The model that was changed.</param>
        private void OnModelChanged(object changedModel)
        {
            this.view.Lines = this.cultivar.Commands;
        }
    }
}
