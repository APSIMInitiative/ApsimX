using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models;
using UserInterface.Views;
using System.Data;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Attaches an Input model to an Input View.
    /// </summary>
    public class ObservedInputPresenter : IPresenter
    {
        private PredictedObserved Input;
        private IInputView View;
        private ExplorerPresenter ExplorerPresenter;

        /// <summary>
        /// Attaches an Input model to an Input View.
        /// </summary>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            Input = model as PredictedObserved;
            View = view as IInputView;
            ExplorerPresenter = explorerPresenter;

            View.FileName = Input.FileName;
            View.GridView.DataSource = Input.GetTable();

            View.BrowseButtonClicked += OnBrowseButtonClicked;
            View.GridView.ColumnHeaderClicked += GridView_ColumnHeaderClicked;
            ExplorerPresenter.CommandHistory.ModelChanged += OnModelChanged;

            HighlightMatchingColumn();
        }

        private void HighlightMatchingColumn()
        {
            if (View.GridView.DataSource != null)
            {
                int col = (View.GridView.DataSource as DataTable).Columns.IndexOf(Input.FieldNameUsedForMatch);
                if (col != -1)
                    View.GridView.SetColumnHeaderColours(col, System.Drawing.Color.Red);
            }
        }


        /// <summary>
        /// Detaches an Input model from an Input View.
        /// </summary>
        public void Detach()
        {
            View.BrowseButtonClicked -= OnBrowseButtonClicked;
            View.GridView.ColumnHeaderClicked -= GridView_ColumnHeaderClicked;
            ExplorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>
        /// Browse button was clicked by user.
        /// </summary>
        private void OnBrowseButtonClicked(object sender, OpenDialogArgs e)
        {
            ExplorerPresenter.CommandHistory.Add(new Commands.ChangePropertyCommand(Input, "FullFileName", e.FileName));
        }

        /// <summary>
        /// User has clicked a header cell.
        /// </summary>
        void GridView_ColumnHeaderClicked(string headerText)
        {
            ExplorerPresenter.CommandHistory.Add(new Commands.ChangePropertyCommand(Input, "FieldNameUsedForMatch", headerText));
        }

        /// <summary>
        /// The model has changed - update the view.
        /// </summary>
        void OnModelChanged(object changedModel)
        {
            View.FileName = Input.FileName;
            View.GridView.DataSource = Input.GetTable();
            HighlightMatchingColumn();
        }


    }
}
