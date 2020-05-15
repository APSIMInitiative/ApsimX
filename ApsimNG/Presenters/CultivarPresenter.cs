namespace UserInterface.Presenters
{
    using System;
    using System.Drawing;
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

            this.view.Lines = this.cultivar.Command;
            intellisense = new IntellisensePresenter(this.view as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;

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
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            intellisense.Cleanup();
        }

        /// <summary>The user has changed the commands</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCommandsChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.view.Lines != this.cultivar.Command)
                {
                    this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

                    Commands.ChangeProperty command = new Commands.ChangeProperty(this.cultivar, "Command", this.view.Lines);
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
                if (e.ControlShiftSpace)
                    intellisense.ShowMethodCompletion(cultivar, e.Code, e.Offset, new Point(e.Coordinates.X, e.Coordinates.Y));
                else if (intellisense.GenerateGridCompletions(e.Code, e.Offset, cultivar, true, false, false, e.ControlSpace))
                    intellisense.Show(e.Coordinates.X, e.Coordinates.Y);
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
            this.view.Lines = this.cultivar.Command;
        }

        /// <summary>
        /// Invoked when the user selects an item in the intellisense.
        /// Inserts the selected item at the caret.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs args)
        {
            view.InsertCompletionOption(args.ItemSelected, args.TriggerWord);
        }
    }
}
