using Models.Interfaces;
using UserInterface.Views;
using Models.Core;
using System.Collections.Generic;
using Models.Utilities;
using Gtk.Sheet;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Presenter for any <see cref="IGridModel"/>.
    /// </summary>
    class GridMultiPresenter : IPresenter
    {
        /// <summary>
        /// The underlying model.
        /// </summary>
        private IGridModel tableModel;
        private IGridView view;
        private ExplorerPresenter presenter;
        private GridPresenter gridPresenter1;
        private GridPresenter gridPresenter2;
        private GridPresenter gridPresenter3;
        private GridPresenter gridPresenter4;

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
            view = v as IGridView;
            tableModel = model as IGridModel;

            List<GridTable> tables = tableModel.Tables;

            string[] contextMenuOptions = new string[] { "Cut", "Copy", "Paste", "Delete", "Select All" };

            ExplorerView ev = parentPresenter.GetView() as ExplorerView;

            view.ShowGrid(2, false, ev);
            view.ShowGrid(3, false, ev);
            view.ShowGrid(4, false, ev);
            if (tables.Count > 0)
            {
                gridPresenter1 = new GridPresenter();
                gridPresenter1.Attach(tables[0], view.Grid1, presenter);
                gridPresenter1.AddContextMenuOptions(contextMenuOptions);
                gridPresenter1.AddIntellisense(model as Model);
                gridPresenter1.CellChanged += OnCellChanged;
                view.SetTableLabelText(tables[0].Name, 1);
                view.ShowGrid(1, true, ev);
            } 
            if (tables.Count > 1)
            {
                gridPresenter2 = new GridPresenter();
                gridPresenter2.Attach(tables[1], view.Grid2, presenter);
                gridPresenter2.AddContextMenuOptions(contextMenuOptions);
                gridPresenter2.AddIntellisense(model as Model);
                gridPresenter2.CellChanged += OnCellChanged;
                view.SetTableLabelText(tables[1].Name, 2);
                view.ShowGrid(2, true, ev);
            }
            if (tables.Count > 2)
            {
                gridPresenter3 = new GridPresenter();
                gridPresenter3.Attach(tables[2], view.Grid3, presenter);
                gridPresenter3.AddContextMenuOptions(contextMenuOptions);
                gridPresenter3.AddIntellisense(model as Model);
                gridPresenter3.CellChanged += OnCellChanged;
                view.SetTableLabelText(tables[2].Name, 3);
                view.ShowGrid(3, true, ev);
            }
            if (tables.Count > 3)
            {
                gridPresenter4 = new GridPresenter();
                gridPresenter4.Attach(tables[3], view.Grid4, presenter);
                gridPresenter4.AddContextMenuOptions(contextMenuOptions);
                gridPresenter4.AddIntellisense(model as Model);
                gridPresenter4.CellChanged += OnCellChanged;
                view.SetTableLabelText(tables[3].Name, 4);
                view.ShowGrid(4, true, ev);
            }

            string text = tableModel.GetDescription();
            if (text.Length > 0)
            {
                view.SetDescriptionText(text);
                view.SetLabelHeight(0.1f);
            } 
            else
            {
                view.SetDescriptionText("");
                view.SetLabelHeight(0.0f);
            }
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            if (gridPresenter1 != null)
            {
                gridPresenter1.CellChanged -= OnCellChanged;
                gridPresenter1.Detach();
            }
                

            if (gridPresenter2 != null)
            {
                gridPresenter2.CellChanged -= OnCellChanged;
                gridPresenter2.Detach();
            }
        }

        private void OnCellChanged(ISheetDataProvider dataProvider, int[] colIndex, int[] rowIndex, string[] values)
        {
        }
    }
}
