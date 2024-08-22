using System;
using System.Collections.Generic;
using System.Drawing;
using APSIM.Shared.Utilities;
using Models;
using UserInterface.EventArguments;
using UserInterface.Interfaces;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// A presenter class for showing an operations model in an operations view.
    /// </summary>
    public class OperationsPresenter : IPresenter
    {
        /// <summary>
        /// The operations object
        /// </summary>
        private Operations operations;

        /// <summary>
        /// The view object
        /// </summary>
        private IEditorView view;

        /// <summary>
        /// The explorer presenter
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// The intellisense object.
        /// </summary>
        private IntellisensePresenter intellisense;

        /// <summary>
        /// Attach model to view.
        /// </summary>
        /// <param name="model">The model object</param>
        /// <param name="view">The view object</param>
        /// <param name="explorerPresenter">The explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.operations = model as Operations;
            this.view = view as IEditorView;
            this.explorerPresenter = explorerPresenter;
            this.intellisense = new IntellisensePresenter(view as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;

            this.PopulateEditorView();
            this.view.ContextItemsNeeded += this.OnContextItemsNeeded;
            this.view.TextHasChangedByUser += this.OnTextHasChangedByUser;
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Detach model from view.
        /// </summary>
        public void Detach()
        {
            this.view.ContextItemsNeeded -= this.OnContextItemsNeeded;
            this.view.TextHasChangedByUser -= this.OnTextHasChangedByUser;
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            this.intellisense.Cleanup();
        }

        /// <summary>
        /// Populate the editor view.
        /// </summary>
        private void PopulateEditorView()
        {
            this.view.Text = this.operations.OperationsAsString;
        }

        /// <summary>
        /// User has changed the text
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnTextHasChangedByUser(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.MainPresenter.ClearStatusPanel();
                this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

                string input = "";
                foreach (string line in this.view.Lines)
                {
                    input += line + Environment.NewLine;
                    if (line.Length > 0)                  
                        if (Operation.ParseOperationString(line.Trim()) == null)
                            explorerPresenter.MainPresenter.ShowMessage($"Warning: unable to parse operation '{line}'", Models.Core.Simulation.MessageType.Warning);
                }

                this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.operations, "OperationsAsString", input));
                this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Editor needs context items.
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnContextItemsNeeded(object sender, NeedContextItemsArgs e)
        {
            try
            {
                if (e.ControlShiftSpace)
                    intellisense.ShowMethodCompletion(operations, e.Code, e.Offset, new Point(e.Coordinates.X, e.Coordinates.Y));
                else if (intellisense.GenerateGridCompletions(e.Code, e.Offset, operations, true, true, false, false, e.ControlSpace))
                    intellisense.Show(e.Coordinates.X, e.Coordinates.Y);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// The mode has changed (probably via undo/redo).
        /// </summary>
        /// <param name="changedModel">The model with changes</param>
        private void OnModelChanged(object changedModel)
        {
            this.PopulateEditorView();
        }

        /// <summary>
        /// Invoked when the user selects an item in the intellisense.
        /// Inserts the selected item at the caret.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs args)
        {
            if (string.IsNullOrEmpty(args.TriggerWord))
                view.InsertAtCaret(args.ItemSelected);
            else
            {
                int position = view.Text.Substring(0, view.Offset).LastIndexOf(args.TriggerWord);
                view.InsertCompletionOption(args.ItemSelected, args.TriggerWord);
            }

            if (args.IsMethod)
            {
                Point cursor = view.GetPositionOfCursor();
                intellisense.ShowMethodCompletion(operations, view.Text, view.Offset, new Point(cursor.X, cursor.Y));
            }
                
        }
    }
}
