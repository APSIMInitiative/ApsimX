using APSIM.Shared.Utilities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// This associates an ID with each property being displayed in
        /// the view, and the object to which that property belongs.
        /// </summary>
        private Dictionary<Guid, PropertyObjectPair> propertyMap = new Dictionary<Guid, PropertyObjectPair>();

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
        /// <param name="obj">The object whose properties will be queried.</param>
        private IEnumerable<Property> GetProperties(object obj)
        {
            IEnumerable<PropertyInfo> allProperties = GetAllProperties(obj)
                    // Only show properties with a DescriptionAttribute
                    .Where(p => Attribute.IsDefined(p, typeof(DescriptionAttribute)))
                    // Only show properties which have a getter and a setter.
                    .Where(p => p.CanRead && p.CanWrite)
                    // Order by line number of the description attribute.
                    .OrderBy(p => p.GetCustomAttribute<DescriptionAttribute>().LineNumber);

            // Filter out properties which don't fit the user's custom filter.
            if (Filter != null)
                allProperties = allProperties.Where(Filter);

            // Due to DisplayType.SubModel, each PropertyInfo can potentially
            // yield multiple properties to be displayed in the view.
            return allProperties.SelectMany(p => CreateProperties(obj, p));
        }

        /// <summary>
        /// Gets all public instance members of a given type.
        /// </summary>
        /// <param name="obj">Object whose members will be retrieved.</param>
        private IEnumerable<PropertyInfo> GetAllProperties(object obj)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
            return obj.GetType().GetProperties(flags);
        }

        /// <summary>
        /// Create Property objects from a PropertyInfo.
        /// </summary>
        /// <remarks>
        /// Each PropertyInfo (a property in the code) can yield multiple
        /// properties shown in the gui, by using DisplayType.SubModel.
        /// </remarks>
        /// <param name="obj">Object reference.</param>
        /// <param name="property">The property metadata.</param>
        private IEnumerable<Property> CreateProperties(object obj, PropertyInfo property)
        {
            DisplayAttribute display = property.GetCustomAttribute<DisplayAttribute>();
            if (display != null && display.Type == DisplayType.SubModel)
            {
                IEnumerable<SeparatorAttribute> separators = property.GetCustomAttributes<SeparatorAttribute>();

                object subModel = property.GetValue(obj);
                bool first = true;
                foreach (Property subProperty in GetProperties(subModel))
                {
                    // If the property contains separators, we want to add those separators
                    // to the first property returned by the sub-model.
                    if (first)
                    {
                        first = false;
                        if (separators != null && separators.Count() > 0)
                            subProperty.Separators.InsertRange(0, separators.Select(s => s.ToString()));
                    }
                    yield return subProperty;
                }
            }
            else
            {
                Property result = new Property(obj, property);
                propertyMap.Add(result.ID, new PropertyObjectPair() { Model = obj, Property = property });
                yield return result;
            }
        }

        /// <summary>
        /// Detach the presenter from the view. Perform misc cleanup.
        /// </summary>
        public void Detach()
        {
            presenter.CommandHistory.ModelChanged -= OnModelChanged;
        }
    
        /// <summary>
        /// Called when a model is changed. Refreshes the view.
        /// </summary>
        /// <param name="changedModel">The model which was changed.</param>
        private void OnModelChanged(object changedModel)
        {
            /************* fixme ***************/
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
            //presenter.CommandHistory.ModelChanged -= OnModelChanged;

            // Figure out which property of which object is being changed.
            PropertyInfo property = propertyMap[args.ID].Property;
            object changedObject = propertyMap[args.ID].Model;

            // In some cases, the new value passed back from the view may be
            // already of the correct type. For example a boolean property
            // is editable via a checkbutton, so the view will return a bool.
            // However, most numbers are just rendered using an entry widget,
            // so the value from the view will be a string (e.g. 1e-6).
            object newValue = args.NewValue;
            if ((newValue == null || newValue is string) && property.PropertyType != typeof(string))
                newValue = ReflectionUtilities.StringToObject(property.PropertyType, (string)args.NewValue, CultureInfo.CurrentCulture);

            // Update the model.
            ICommand updateModel = new ChangeProperty(changedObject, property.Name, newValue);
            presenter.CommandHistory.Add(updateModel);

            // Re-attach the model changed handler, so we can continue to trap
            // changes to the model from other sources (e.g. undo/redo).
            //presenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Stores a property and the object to which it belongs.
        /// </summary>
        private struct PropertyObjectPair
        {
            public object Model { get; set; }
            public PropertyInfo Property { get; set; }
        }
    }
}