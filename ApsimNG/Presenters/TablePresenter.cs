using UserInterface.Commands;
using UserInterface.EventArguments;
using Models.Interfaces;
using System;
using UserInterface.Views;
using Models.Core;
using System.Collections.Generic;
using System.Data;
using Models.Management;
using Models.Utilities;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Presenter for any <see cref="IGridTable"/>.
    /// </summary>
    class TablePresenter : IPresenter
    {
        /// <summary>
        /// The underlying model.
        /// </summary>
        private IGridTable tableModel;

        /// <summary>
        /// The intellisense.
        /// </summary>
        private IntellisensePresenter intellisense;

        private IDualGridView view;
        private ExplorerPresenter presenter;
        private NewGridPresenter gridPresenter1;
        private NewGridPresenter gridPresenter2;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to connect to.</param>
        /// <param name="v">The view to connect to.</param>
        /// <param name="parentPresenter">The parent explorer presenter.</param>
        public void Attach(object model, object v, ExplorerPresenter parentPresenter)
        {
            if (model is ITestable t)
                t.Test(false, true);

            presenter = parentPresenter;
            view = v as IDualGridView;
            tableModel = model as IGridTable;

            List<GridTable> tables = tableModel.Tables;

            view.ShowGrid2(false);
            if (tables.Count > 0)
            {
                gridPresenter1 = new NewGridPresenter();
                gridPresenter1.Attach(tables[0], view.Grid1, parentPresenter);
                gridPresenter1.CellChanged += OnCellChanged;
            } 
            if (tables.Count > 1)
            {
                gridPresenter2 = new NewGridPresenter();
                gridPresenter2.Attach(tables[1], view.Grid2, parentPresenter);
                gridPresenter2.CellChanged += OnCellChanged;
                view.ShowGrid2(true);
            }

            intellisense = new IntellisensePresenter(view.Grid2 as ViewBase);
            //intellisense.ItemSelected += OnIntellisenseItemSelected;
            //view.Grid2.ContextItemsNeeded += OnIntellisenseItemsNeeded;

            if (model is BiomassRemovalFractions)
            {
                view.SetLabelText((model as BiomassRemovalFractions).Description);
                view.SetLabelHeight(0.1f);
            } 
            else
            {
                view.SetLabelText("");
                view.SetLabelHeight(0.0f);
            }
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            //intellisense.ItemSelected -= OnIntellisenseItemSelected;
            intellisense.Cleanup();
            //view.Grid2.ContextItemsNeeded -= OnIntellisenseItemsNeeded;
            if (gridPresenter1 != null)
                gridPresenter1.CellChanged -= OnCellChanged;
                gridPresenter1.Detach();
            if (gridPresenter2 != null)
                gridPresenter2.CellChanged -= OnCellChanged;
                gridPresenter2.Detach();
        }

        /// <summary>Invoked when a grid cell has changed.</summary>
        /// <param name="dataProvider">The provider that contains the data.</param>
        /// <param name="colIndex">The index of the column of the cell that was changed.</param>
        /// <param name="rowIndex">The index of the row of the cell that was changed.</param>
        private void OnCellChanged(ISheetDataProvider dataProvider, int colIndex, int rowIndex)
        {
        }

        /// <summary>
        /// Invoked when the view is asking for completion options.
        /// Generates and displays these completion options.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Evenet arguments.</param>
        private void OnIntellisenseItemsNeeded(object sender, NeedContextItemsArgs args)
        {
            /*
            try
            {
                if (intellisense.GenerateGridCompletions(args.Code, args.Offset, (tableModel as IModel).Children[0], true, false, false, false, args.ControlSpace))
                    intellisense.Show(args.Coordinates.X, args.Coordinates.Y);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
            */
        }

        /// <summary>
        /// Invoked when the user selects an item in the intellisense.
        /// Inserts the selected item at the caret.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs args)
        {
            /*
            try
            {
                view.Grid2.InsertText(args.ItemSelected);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
            */
        }
    }
}
