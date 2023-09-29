using Models.Interfaces;
using UserInterface.Views;
using Models.Core;
using System.Collections.Generic;
using Models.Utilities;

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

            view.ShowGrid(2, false);
            if (tables.Count > 0)
            {
                gridPresenter1 = new GridPresenter();
                gridPresenter1.Attach(tables[0], view.Grid1, presenter);
                gridPresenter1.AddContextMenuOptions(contextMenuOptions);
                gridPresenter1.AddIntellisense(model as Model);
            } 
            if (tables.Count > 1)
            {
                gridPresenter2 = new GridPresenter();
                gridPresenter2.Attach(tables[1], view.Grid2, presenter);
                gridPresenter2.AddContextMenuOptions(contextMenuOptions);
                gridPresenter2.AddIntellisense(model as Model);
                view.ShowGrid(2, true);
            }

            string text = tableModel.GetDescription();
            if (text.Length > 0)
            {
                view.SetLabelText(text);
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
            if (gridPresenter1 != null)
                gridPresenter1.Detach();

            if (gridPresenter2 != null)
                gridPresenter2.Detach();
        }
    }
}
