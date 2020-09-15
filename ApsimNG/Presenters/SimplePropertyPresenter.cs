using APSIM.Shared.Utilities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UserInterface.Classes;
using UserInterface.Commands;
using UserInterface.EventArguments;
using UserInterface.Interfaces;

namespace UserInterface.Presenters
{
    public class SimplePropertyPresenter : IPresenter
    {
        /// <summary>
        /// The model whose properties are being displayed.
        /// </summary>
        private IModel model;

        /// <summary>
        /// The view.
        /// </summary>
        private IPropertyView view;

        /// <summary>
        /// The explorer presenter instance.
        /// </summary>
        private ExplorerPresenter presenter;

        /// <summary>
        /// A filter function which can be used to filter which properties
        /// can be displayed.
        /// </summary>
        public Func<PropertyInfo, bool> Filter { get; set; }

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="view">The view.</param>
        /// <param name="explorerPresenter">An <see cref="ExplorerPresenter" /> instance.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (view == null)
                throw new ArgumentNullException(nameof(view));
            if (explorerPresenter == null)
                throw new ArgumentNullException(nameof(explorerPresenter));

            this.model = model as IModel;
            this.view = view as IPropertyView;
            this.presenter = explorerPresenter;

            if (this.model == null)
                throw new ArgumentException($"The model must be an IModel instance");
            if (this.view == null)
                throw new ArgumentException($"The view must be an IPropertyView instance");
            
            RefreshView();
            presenter.CommandHistory.ModelChanged += OnModelChanged;
            this.view.PropertyChanged += OnViewChanged;
        }

        /// <summary>
        /// Refresh the view with the model's current state.
        /// </summary>
        private void RefreshView()
        {
            IEnumerable<Property> properties = GetProperties(model).ToArray();
            view.DisplayProperties(properties);
        }

        /// <summary>
        /// Get a list of properties from the model.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private IEnumerable<Property> GetProperties(IModel model)
        {
            IEnumerable<PropertyInfo> allProperties = GetAllProperties(model)
                    .Where(p => Attribute.IsDefined(p, typeof(DescriptionAttribute)))
                    .Where(p => p.CanWrite && p.CanRead)
                    .OrderBy(p => (p.GetCustomAttribute<DescriptionAttribute>().LineNumber));

            // Filter out properties which don't fit the user's filter criterion.
            if (Filter != null)
                allProperties = allProperties.Where(Filter);

            return allProperties.Select(p => new Property(model, p));
        }

        /// <summary>
        /// Gets all public instance members of a given type,
        /// sorted by the line number of the member's declaration.
        /// </summary>
        /// <param name="o">Object whose members will be retrieved.</param>
        private IEnumerable<PropertyInfo> GetAllProperties(object o)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
            return o.GetType().GetProperties(flags);
        }

        /// <summary>
        /// Detach the presenter from the view. Perform misc cleanup.
        /// </summary>
        /// <remarks>
        /// Should we update the model at this point?
        /// </remarks>
        public void Detach()
        {
            //view.PropertyChanged -= OnViewChanged;
            presenter.CommandHistory.ModelChanged -= OnModelChanged;
        }
    
        /// <summary>
        /// Called when a model is changed. Refreshes the view.
        /// </summary>
        /// <param name="changedModel">The model which was changed.</param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == model)
                RefreshView();
        }
    
        /// <summary>
        /// Called when the view is changed. Updates the model's state.
        /// </summary>
        /// <param name="sender">Sending object.</param>
        /// <param name="args">Event data.</param>
        private void OnViewChanged(object sender, PropertyChangedEventArgs args)
        {
            // We don't want to refresh the entire view after applying the change
            // to the model, so we need to temporarily detach the ModelChanged handler.
            presenter.CommandHistory.ModelChanged -= OnModelChanged;

            // Update the model.
            Type propertyType = model.GetType().GetProperty(args.PropertyName).PropertyType;
            object newValue = ReflectionUtilities.StringToObject(propertyType, args.NewValue);
            ICommand updateModel = new ChangeProperty(model, args.PropertyName, newValue);
            presenter.CommandHistory.Add(updateModel);

            // Re-attach the model changed handler, so we can continue to trap
            // changes to the model from other sources (e.g. undo/redo).
            presenter.CommandHistory.ModelChanged += OnModelChanged;

            // Note: we don't really need to refresh the view at this point -
            // the assumption is that the view already contains the updated state.
        }
}
}