using Models;
using Models.CLEM;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Classes;
using UserInterface.Interfaces;
using UserInterface.Presenters;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// This presenter adds functionality to the SimplePropertyPresenter by using the child models (of the same type) of the
    /// model passed to the presenter. The property descriptions (of the child moels) are provided on the left with a column (named)
    /// of the property entries for each model provided
    /// </summary>
    /// <remarks>
    /// This can be used to let the user see and update all child model entries of the parent model which generally does not have any properties
    /// This approach is used in CLEM where the tree structure defines the setp of the simulation.
    /// </remarks>
    public class PropertyMultiModelPresenter: PropertyPresenter
    {
        /// <summary>
        /// The list of child models whose properties are being displayed.
        /// Used with PropertyMultiModelView and PropertyCategorisedPresenter
        /// </summary>
        private List<object> models = new List<object>();

        /// <summary>
        /// The view.
        /// </summary>
        protected new PropertyMultiModelView view;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="view">The view.</param>
        /// <param name="explorerPresenter">An <see cref="ExplorerPresenter" /> instance.</param>
        public override void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));
            if (explorerPresenter == null)
                throw new ArgumentNullException(nameof(explorerPresenter));

            this.model = model as IModel;
            this.view = view as PropertyMultiModelView;
            base.view = view as IPropertyView;
            this.presenter = explorerPresenter;

            if (this.model != null && !(this.model is IModel))
                throw new ArgumentException($"The model must be an IModel instance");
            if (this.view == null)
                throw new ArgumentException($"The view must be an IPropertyView instance");

            RefreshView(this.model);
            presenter.CommandHistory.ModelChanged += OnModelChanged;
            this.view.PropertyChanged += OnViewChanged;
        }


        /// <summary>
        /// Refresh the view with the model's current state.
        /// </summary>
        public override void RefreshView(IModel model)
        {
            if (model != null)
            {
                models.Clear();
                models.AddRange(this.model.Node.FindChildren<IModel>().Where(a => a.GetType() != typeof(Memo)));
                foreach (var ignoredChildGroup in (model as CLEMModel).GetChildrenInSummary().Where(a => !a.include))
                    models.RemoveAll(a => ignoredChildGroup.models.Contains(a));

//                models.AddRange(this.model.FindAllChildren<IModel>().Where(a => a.GetType() != typeof(Memo) && ((model is CLEMModel)?(model as CLEMModel).ChildrenToIgnoreInSummary()?.Where(b => b.IsAssignableFrom(a.GetType())).Any()??false == false : true)));
                if (models.GroupBy(a => a.GetType()).Count() > 1)
                {
                    throw new ArgumentException($"The models displayed in a PropertyMultiModelView must all be of the same type");
                }
                if (models.Count() >= 1)
                {
                    view.DisplayProperties(GetProperties(models));
                    return;
                }
                this.model = model;
            }
        }

        /// <summary>
        /// Get a list of properties from the model.
        /// </summary>
        /// <param name="objs">The list of all objects whose properties will be queried.</param>
        private List<PropertyGroup> GetProperties(List<object> objs)
        {
            List<PropertyGroup> propertyGroupList = new List<PropertyGroup>();
            foreach (var item in objs)
            {
                propertyGroupList.Add(GetProperties(item));
            }
            return propertyGroupList;
        }
    }
}
