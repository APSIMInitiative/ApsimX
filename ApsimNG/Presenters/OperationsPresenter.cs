using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UserInterface.Views;
using Models;
using System.Reflection;
using UserInterface.EventArguments;
using Models.Core;
using Models.Factorial;

namespace UserInterface.Presenters
{

    /// <summary>
    /// A presenter class for showing an operations model in an operations view.
    /// </summary>
    public class OperationsPresenter : IPresenter
    {
        private Operations Operations;
        private EditorView View;
        private ExplorerPresenter ExplorerPresenter;

        /// <summary>
        /// Attach model to view.
        /// </summary>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            Operations = model as Operations;
            View = view as EditorView;
            ExplorerPresenter = explorerPresenter;

            PopulateEditorView();
            View.ContextItemsNeeded += OnContextItemsNeeded;
            View.TextHasChangedByUser += OnTextHasChangedByUser;
            ExplorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Detach model from view.
        /// </summary>
        public void Detach()
        {
            View.ContextItemsNeeded -= OnContextItemsNeeded;
            View.TextHasChangedByUser -= OnTextHasChangedByUser;
            ExplorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>
        /// Populate the editor view.
        /// </summary>
        private void PopulateEditorView()
        {
            string st = "";
            foreach (Operation operation in Operations.Schedule)
            {
                st += operation.Date.ToString("yyyy-MM-dd") + " " + operation.Action + Environment.NewLine;
            }
            View.Text = st;
        }

        /// <summary>
        /// User has changed the text
        /// </summary>
        private void OnTextHasChangedByUser(object sender, EventArgs e)
        {
            ExplorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            List<Operation> operations = new List<Operation>();
            foreach (string line in View.Lines)
            {
                int Pos = line.IndexOf(' ');
                if (Pos != -1)
                {
                    Operation operation = new Operation();
                    DateTime d;
                    if (DateTime.TryParse(line.Substring(0, Pos), out d))
                        operation.Date = d;
                    operation.Action = line.Substring(Pos + 1);
                    operations.Add(operation);
                }
            }
            ExplorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(Operations, "Schedule", operations));
            ExplorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Editor needs context items.
        /// </summary>
        private void OnContextItemsNeeded(object sender, NeedContextItemsArgs e)
        {
            e.AllItems.AddRange(NeedContextItemsArgs.ExamineModelForNames(Operations, e.ObjectName, true, true, false));
        }

        /// <summary>
        /// The mode has changed (probably via undo/redo).
        /// </summary>
        private void OnModelChanged(object changedModel)
        {
            PopulateEditorView();
        }

    }
}
