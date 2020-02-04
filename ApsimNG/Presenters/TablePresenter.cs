namespace UserInterface.Presenters
{
    using Commands;
    using EventArguments;
    using Interfaces;
    using Models.Interfaces;
    using System;
    using Views;
    using Models.Core;
    using System.Collections.Generic;
    using System.Data;

    /// <summary>
    /// Presenter for any <see cref="IModelAsTable"/>.
    /// </summary>
    class TablePresenter : IPresenter
    {
        /// <summary>
        /// The underlying model.
        /// </summary>
        private IModelAsTable tableModel;

        /// <summary>
        /// The intellisense.
        /// </summary>
        private IntellisensePresenter intellisense;

        private IDualGridView view;
        private ExplorerPresenter presenter;
        private List<DataTable> tables;
        private GridPresenter gridPresenter1;
        private GridPresenter gridPresenter2;

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
            tableModel = model as IModelAsTable;
            tables = tableModel.Tables;
            view.Grid1.DataSource = tables[0];
            view.Grid2.DataSource = tables.Count > 1 ? tables[1] : null;
            view.Grid1.CellsChanged += OnCellValueChanged1;
            view.Grid2.CellsChanged += OnCellValueChanged2;

            bool readOnly = !tableModel.GetType().GetProperty("Tables").CanWrite;
            view.Grid1.ReadOnly = readOnly;
            view.Grid2.ReadOnly = readOnly;

            parentPresenter.CommandHistory.ModelChanged += OnModelChanged;

            gridPresenter1 = new GridPresenter();
            gridPresenter1.Attach(model, view.Grid1, parentPresenter);
            gridPresenter2 = new GridPresenter();
            gridPresenter2.Attach(model, view.Grid2, parentPresenter);

            intellisense = new IntellisensePresenter(view.Grid2 as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;
            view.Grid2.ContextItemsNeeded += OnIntellisenseItemsNeeded;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            view.Grid2.ContextItemsNeeded -= OnIntellisenseItemsNeeded;
            view.Grid1.CellsChanged -= OnCellValueChanged1;
            view.Grid2.CellsChanged -= OnCellValueChanged2;
            gridPresenter1.Detach();
            gridPresenter2.Detach();
            presenter.CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnCellValueChanged1(object sender, GridCellsChangedArgs e)
        {
            foreach (GridCellChangedArgs cell in e.ChangedCells)
            {
                try
                {
                    tables[0] = view.Grid1.DataSource;
                    ChangeProperty cmd = new ChangeProperty(tableModel, "Tables", tables);
                    presenter.CommandHistory.Add(cmd);
                }
                catch (Exception ex)
                {
                    presenter.MainPresenter.ShowError(ex);
                }
            }
        }

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnCellValueChanged2(object sender, GridCellsChangedArgs e)
        {
            foreach (GridCellChangedArgs cell in e.ChangedCells)
            {
                try
                {
                    tables[1] = view.Grid2.DataSource;
                    ChangeProperty cmd = new ChangeProperty(tableModel, "Tables", tables);
                    presenter.CommandHistory.Add(cmd);
                }
                catch (Exception ex)
                {
                    presenter.MainPresenter.ShowError(ex);
                }
            }
        }

        /// <summary>
        /// The model has changed, update the grid.
        /// </summary>
        /// <param name="changedModel">The model that has changed.</param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == tableModel)
            {
                tables = tableModel.Tables;
                view.Grid1.DataSource = tables[0];
                view.Grid2.DataSource = tables[1];
            }
        }

        /// <summary>
        /// Invoked when the view is asking for completion options.
        /// Generates and displays these completion options.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Evenet arguments.</param>
        private void OnIntellisenseItemsNeeded(object sender, NeedContextItemsArgs args)
        {
            try
            {
                if (intellisense.GenerateGridCompletions(args.Code, args.Offset, (tableModel as IModel).Children[0], true, false, false, args.ControlSpace))
                    intellisense.Show(args.Coordinates.X, args.Coordinates.Y);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user selects an item in the intellisense.
        /// Inserts the selected item at the caret.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs args)
        {
            try
            {
                view.Grid2.InsertText(args.ItemSelected);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
        }
    }
}
