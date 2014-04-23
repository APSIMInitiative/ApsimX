using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UserInterface.Views;
using Models;
using System.Reflection;

namespace UserInterface.Presenters
{

    /// <summary>
    /// A presenter class for showing an operations model in an operations view.
    /// </summary>
    public class OperationsPresenter : IPresenter
    {
        private Operations Operations;
        private OperationsView View;
        private ExplorerPresenter ExplorerPresenter;

        /// <summary>
        /// Attach model to view.
        /// </summary>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            Operations = model as Operations;
            View = view as OperationsView;
            ExplorerPresenter = explorerPresenter;

            PopulateEditorView();
            View.EditorView.ContextItemsNeeded += OnContextItemsNeeded;
            View.EditorView.TextHasChangedByUser += OnTextHasChangedByUser;
            ExplorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Detach model from view.
        /// </summary>
        public void Detach()
        {
            View.EditorView.ContextItemsNeeded -= OnContextItemsNeeded;
            View.EditorView.TextHasChangedByUser -= OnTextHasChangedByUser;
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
            View.EditorView.Text = st;
        }

        /// <summary>
        /// User has changed the text
        /// </summary>
        private void OnTextHasChangedByUser(object sender, EventArgs e)
        {
            ExplorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            List<Operation> operations = new List<Operation>();
            foreach (string line in View.EditorView.Lines)
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
            ExplorerPresenter.CommandHistory.Add(new Commands.ChangePropertyCommand(Operations, "Schedule", operations));
            ExplorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Editor needs context items.
        /// </summary>
        private void OnContextItemsNeeded(object sender, Utility.NeedContextItems e)
        {
            object o = Operations.Get(e.ObjectName);

            if (o == null)
                o = Operations.Find(e.ObjectName);

            if (o != null)
            {
                foreach (MethodInfo method in o.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
                    e.Items.Add(method.Name);
            }
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
