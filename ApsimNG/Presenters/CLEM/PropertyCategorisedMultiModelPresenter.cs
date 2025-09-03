using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Interfaces;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// A combination of the PropertyCategorisedPresenter for property category filtering
    /// and the PropertyMultiModelPresenter to display the properties of all children of the attachedm model
    /// as columns of property entry idems in the display
    /// </summary>
    public class PropertyCategorisedMultiModelPresenter: PropertyCategorisedPresenter
    {
        /// <summary>
        /// Attach the view to this presenter and begin populating the view.
        /// </summary>
        /// <param name="model">The simulation model</param>
        /// <param name="view">The view used for display</param>
        /// <param name="explorerPresenter">The presenter for this object</param>
        public override void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as Model;

            this.treeview = view as PropertyCategorisedView;
            if(treeview is null)
                throw new ArgumentException($"The view must be an PropertyCategorisedView instance");

            this.treeview.SelectedNodeChanged += this.OnNodeSelected;
            this.explorerPresenter = explorerPresenter;

            //Fill in the nodes in the tree view
            this.RefreshTreeView();

            //Initialise the Right Hand View
            this.propertyPresenter = new PropertyMultiModelPresenter();
            this.propertyView = new PropertyMultiModelView(this.treeview as ViewBase);
            this.ShowRightHandView();
        }

        public override IModel ModelForProperties()
        {
            return this.model.Node.FindChild<IModel>();
        }


        public override void CreateAndAttachRightPanel()
        {
            //create a new grid view to be added as a RightHandView
            //nb. the grid view is owned by the tree view not by this presenter.
            this.propertyView = new PropertyMultiModelView(this.treeview as ViewBase);
            this.treeview.AddRightHandView(this.propertyView);
            this.propertyPresenter.Attach(this.model, this.propertyView, this.explorerPresenter);
        }

    }
}
