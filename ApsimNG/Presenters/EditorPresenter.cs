using System;
using System.Drawing;
using System.Linq;
using Models.Core;
using UserInterface.Commands;
using UserInterface.Interfaces;
using UserInterface.Views;
using UserInterface.EventArguments;

namespace UserInterface.Presenters
{


    /// <summary>
    /// A presenter class for showing a cultivar.
    /// </summary>
    public class EditorPresenter : IPresenter, ISubPresenter
    {
        /// <summary>The cultivar model</summary>
        private ILineEditor model;

        /// <summary>The cultivar view</summary>
        private IEditorView view;

        /// <summary>The parent explorer presenter</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The intellisense object.</summary>
        private IntellisensePresenter intellisense;

        /// <summary>
        /// Flag to record if Presenter is currently listening for events.
        /// Prevents event listeners from being doubled up when used as sub 
        /// presenter.
        /// </summary>
        private bool _eventsConnected = false;

        /// <summary>Attach the cultivar model to the cultivar view</summary>
        /// <param name="model">The mode</param>
        /// <param name="view">The view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as ILineEditor;
            this.view = view as IEditorView;
            (this.view as EditorView).Language = "c-sharp";
            this.explorerPresenter = explorerPresenter;

            this.view.Lines = this.model.Lines?.ToArray();
            intellisense = new IntellisensePresenter(this.view as ViewBase);
            ConnectEvents();
        }

        /// <summary>Detach the model from the view</summary>
        public void Detach()
        {
            DisconnectEvents();
            OnCommandsChanged(this, new EventArgs());
            intellisense.Cleanup();
        }

        /// <summary>Connect all widget events.</summary>
        public void ConnectEvents()
        {
            if (!_eventsConnected)
            {
                _eventsConnected = true;
                intellisense.ItemSelected += OnIntellisenseItemSelected;
                view.LeaveEditor += OnCommandsChanged;
                view.ContextItemsNeeded += OnContextItemsNeeded;
                explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
            }
        }

        /// <summary>Disconnect all widget events.</summary>
        public void DisconnectEvents()
        {
            if (_eventsConnected)
            {
                _eventsConnected = false;
                view.LeaveEditor -= OnCommandsChanged;
                view.ContextItemsNeeded -= OnContextItemsNeeded;
                explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
                intellisense.ItemSelected -= OnIntellisenseItemSelected;
            }
        }

        public void Refresh()
        {
            view.Show();
            view.Refresh();
        }

        /// <summary>The user has changed the commands</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCommandsChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.view.Lines != this.model.Lines)
                {
                    this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

                    if (model.Lines == null || !model.Lines.SequenceEqual(view.Lines))
                    {
                        ChangeProperty command = new ChangeProperty(model, nameof(model.Lines), this.view.Lines);
                        explorerPresenter.CommandHistory.Add(command);
                    }

                    explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
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
                    intellisense.ShowMethodCompletion(model, e.Code, e.Offset, new Point(e.Coordinates.X, e.Coordinates.Y));
                else if (intellisense.GenerateGridCompletions(e.Code, e.Offset, model, true, false, false, false, e.ControlSpace))
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
            if (changedModel is ILineEditor linesModel)
                if (linesModel.FullPath == model.FullPath)
                    view.Lines = linesModel.Lines.ToArray();
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
