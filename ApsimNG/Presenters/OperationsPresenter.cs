// -----------------------------------------------------------------------
// <copyright file="OperationsPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using APSIM.Shared.Utilities;
    using EventArguments;
    using Models;
    using Views;

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
        private EditorView view;

        /// <summary>
        /// The explorer presenter
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attach model to view.
        /// </summary>
        /// <param name="model">The model object</param>
        /// <param name="view">The view object</param>
        /// <param name="explorerPresenter">The explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.operations = model as Operations;
            this.view = view as EditorView;
            this.explorerPresenter = explorerPresenter;

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
        }

        /// <summary>
        /// Populate the editor view.
        /// </summary>
        private void PopulateEditorView()
        {
            string st = string.Empty;
            foreach (Operation operation in this.operations.Schedule)
            {
                // st += operation.Date.ToString("yyyy-MM-dd") + " " + operation.Action + Environment.NewLine;
                string dateStr = DateUtilities.validateDateString(operation.Date);
                st += dateStr + " " + operation.Action + Environment.NewLine;
            }

            this.view.Text = st;
        }

        /// <summary>
        /// User has changed the text
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnTextHasChangedByUser(object sender, EventArgs e)
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
            List<Operation> operations = new List<Operation>();
            foreach (string line in this.view.Lines)
            {
                int pos = line.IndexOf(' ');
                if (pos != -1)
                {
                    Operation operation = new Operation();
                    operation.Date = DateUtilities.validateDateString(line.Substring(0, pos));
                    operation.Action = line.Substring(pos + 1);
                    operations.Add(operation);
                }
            }

            this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.operations, "Schedule", operations));
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Editor needs context items.
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnContextItemsNeeded(object sender, NeedContextItemsArgs e)
        {
            e.AllItems.AddRange(NeedContextItemsArgs.ExamineModelForNames(this.operations, e.ObjectName, true, true, false));
        }

        /// <summary>
        /// The mode has changed (probably via undo/redo).
        /// </summary>
        /// <param name="changedModel">The model with changes</param>
        private void OnModelChanged(object changedModel)
        {
            this.PopulateEditorView();
        }
    }
}
