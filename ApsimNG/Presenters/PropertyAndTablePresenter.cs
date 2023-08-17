using APSIM.Shared.Utilities;
using UserInterface.Commands;
using UserInterface.EventArguments;
using Models.Core;
using Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UserInterface.Views;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace UserInterface.Presenters
{

    /// <summary>Presenter that has a PropertyPresenter and a GridPresenter.</summary>
    class PropertyAndTablePresenter : IPresenter
    {
        /// <summary>The underlying model</summary>
        private IntellisensePresenter intellisense;
        private IPropertyAndGridView view;
        private ExplorerPresenter explorerPresenter;
        private IPresenter propertyPresenter;
        private NewGridPresenter gridPresenter;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to connect to.</param>
        /// <param name="v">The view to connect to.</param>
        /// <param name="parentPresenter">The parent explorer presenter.</param>
        public void Attach(object model, object v, ExplorerPresenter parentPresenter)
        {
            explorerPresenter = parentPresenter;
            view = v as IPropertyAndGridView;
            intellisense = new IntellisensePresenter(view as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;

            propertyPresenter = new PropertyPresenter();
            explorerPresenter.ApsimXFile.Links.Resolve(propertyPresenter);
            propertyPresenter.Attach(model, view.PropertiesView, parentPresenter);
            gridPresenter = new NewGridPresenter();
            gridPresenter.Attach((model as IGridTable).Tables[0], view.Grid, parentPresenter);
            //view.Grid2.ContextItemsNeeded += OnContextItemsNeeded;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            intellisense.Cleanup();
            //gridPresenter.ContextItemsNeeded -= OnContextItemsNeeded;
            propertyPresenter.Detach();
            gridPresenter.Detach();
        }

        /// <summary>
        /// Called when an intellisense item is selected.
        /// Inserts the item into view.Grid2 (the lower gridview).
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs e)
        {
            //view.Grid2.InsertText(e.ItemSelected);
        }

        /// <summary>
        /// Called when the view is asking for completion items.
        /// Shows the intellisense popup.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event data.</param>
        private void OnContextItemsNeeded(object sender, NeedContextItemsArgs e)
        {
            //if (intellisense.GenerateGridCompletions(e.Code, e.Offset, tableModel as IModel, true, false, false, false, false))
            //    intellisense.Show(e.Coordinates.X, e.Coordinates.Y);
        }
    }
}
