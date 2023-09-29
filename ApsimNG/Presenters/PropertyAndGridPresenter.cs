using Models.Interfaces;
using UserInterface.Views;

namespace UserInterface.Presenters
{

    /// <summary>Presenter that has a PropertyPresenter and a GridPresenter.</summary>
    class PropertyAndGridPresenter : IPresenter
    {
        /// <summary>The underlying model</summary>
        private IPropertyAndGridView view;
        private ExplorerPresenter explorerPresenter;
        private IPresenter propertyPresenter;
        private GridPresenter gridPresenter;

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

            propertyPresenter = new PropertyPresenter();
            explorerPresenter.ApsimXFile.Links.Resolve(propertyPresenter);
            propertyPresenter.Attach(model, view.PropertiesView, parentPresenter);
            gridPresenter = new GridPresenter();
            gridPresenter.Attach((model as IGridModel).Tables[0], view.Grid, parentPresenter);
            gridPresenter.AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All" });
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            propertyPresenter.Detach();
            gridPresenter.Detach();
        }
    }
}
