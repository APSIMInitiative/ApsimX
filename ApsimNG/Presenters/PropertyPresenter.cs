using APSIM.Shared.Utilities;
using Models.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UserInterface.Classes;
using UserInterface.Commands;
using UserInterface.EventArguments;
using UserInterface.Interfaces;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    public class PropertyPresenter : IPresenter
    {
        /// <summary>
        /// The model whose properties are being displayed.
        /// </summary>
        protected IModel model;

        /// <summary>
        /// The view.
        /// </summary>
        protected IPropertyView view;

        /// <summary>
        /// The explorer presenter instance.
        /// </summary>
        protected ExplorerPresenter presenter;

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
        /// Called when the view is refreshed
        /// </summary>
        public event EventHandler ViewRefreshed;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="view">The view.</param>
        /// <param name="explorerPresenter">An <see cref="ExplorerPresenter" /> instance.</param>
        public virtual void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));
            if (explorerPresenter == null)
                throw new ArgumentNullException(nameof(explorerPresenter));

            this.model = model as IModel;
            this.view = view as IPropertyView;
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
        public virtual void RefreshView(IModel model)
        {
            if (model != null)
            {
                view.PropertyChanged -= OnViewChanged;
                presenter.CommandHistory.ModelChanged -= OnModelChanged;

                this.view.SaveChanges();

                if (GetAllEditorViews().Count > 0)
                    this.view.DeleteEditorViews();

                this.model = model;
                view.DisplayProperties(GetProperties(this.model));

                view.PropertyChanged += OnViewChanged;
                presenter.CommandHistory.ModelChanged += OnModelChanged;

                ViewRefreshed?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// Get a list of properties from the model.
        /// </summary>
        /// <param name="obj">The object whose properties will be queried.</param>
        protected virtual PropertyGroup GetProperties(object obj)
        {
            IEnumerable<PropertyInfo> allProperties = GetAllProperties(obj)
                    // Only show properties with a DescriptionAttribute
                    .Where(p => Attribute.IsDefined(p, typeof(DescriptionAttribute)))
                    // Only show properties which have a getter and a setter.
                    .Where(p => p.CanRead && p.CanWrite)
                    // Order by line number of the description attribute.
                    .OrderBy(p => p.GetCustomAttribute<DisplayAttribute>()?.Order??0)
                    // Then order by line number of the description attribute.
                    .ThenBy(p => p.GetCustomAttribute<DescriptionAttribute>().LineNumber);

            // Filter out properties which don't fit the user's custom filter.
            if (Filter != null)
                allProperties = allProperties.Where(Filter);

            // Due to DisplayType.SubModel, each PropertyInfo can potentially
            // yield multiple properties to be displayed in the view.
            List<Property> properties = new List<Property>();
            List<PropertyGroup> subModelProperties = new List<PropertyGroup>();
            foreach (PropertyInfo property in allProperties)
            {
                DisplayAttribute display = property.GetCustomAttribute<DisplayAttribute>();
                if (display != null && display.Type == DisplayType.SubModel)
                {
                    object subObject = property.GetValue(obj);
                    if (subObject == null)
                        subObject = Activator.CreateInstance(property.PropertyType);
                    PropertyGroup group = GetProperties(subObject);
                    group.Name = property.GetCustomAttribute<DescriptionAttribute>()?.ToString() ?? property.Name;
                    string units = property.GetCustomAttribute<UnitsAttribute>()?.ToString();
                    if (!string.IsNullOrEmpty(units))
                    {
                        units = "(" + units + ")";
                        if (!group.Name.Contains(units))
                            group.Name += " " + units;
                    }
                    subModelProperties.Add(group);
                }
                else
                {
                    // determine where to link to the property or use a substitute sub-property for a class-based property.
                    Property result;
                    string subPropertyName = property.GetCustomAttribute<DisplayAttribute>()?.SubstituteSubPropertyName ?? "";
                    if (subPropertyName.Any())
                    {
                        object subObject = property.GetValue(obj);
                        subObject ??= Activator.CreateInstance(property.PropertyType);
                        PropertyInfo subProperty = GetAllProperties(subObject).Where(a => a.Name == subPropertyName).FirstOrDefault();

                        result = new Property(subObject, subProperty);
                        propertyMap.Add(result.ID, new PropertyObjectPair() { Model = subObject, Property = subProperty });
                    }
                    else
                    {
                        result = new Property(obj, property);
                        propertyMap.Add(result.ID, new PropertyObjectPair() { Model = obj, Property = property });
                    }
                    properties.Add(result);
                }
            }
            string name = obj is IModel model ? model.Name : obj.GetType().Name;
            return new PropertyGroup(name, properties, subModelProperties);
        }

        /// <summary>
        /// Gets all public instance members of a given type.
        /// </summary>
        /// <param name="obj">Object whose members will be retrieved.</param>
        private IEnumerable<PropertyInfo> GetAllProperties(object obj)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            return obj.GetType().GetProperties(flags);
        }

        /// <summary>
        /// Detach the presenter from the view. Perform misc cleanup.
        /// </summary>
        public virtual void Detach()
        {
            view.SaveChanges();
            view.PropertyChanged -= OnViewChanged;
            (view as ViewBase).Dispose();
            presenter.CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>
        /// Returns a list of all code editor views that have been created.
        /// Used by the presenter to connect up intellisense events.
        /// </summary>
        public List<EditorView> GetAllEditorViews()
        {
            return this.view.GetAllEditorViews();
        }

        /// <summary>
        /// Called when a model is changed. Refreshes the view.
        /// </summary>
        /// <param name="changedModel">The model which was changed.</param>
        protected virtual void OnModelChanged(object changedModel)
        {
            if (propertyMap.Values.Any(p => p.Model == changedModel))
                RefreshView(this.model);
        }
    
        /// <summary>
        /// Called when the view is changed. Updates the model's state.
        /// </summary>
        /// <param name="sender">Sending object.</param>
        /// <param name="args">Event data.</param>
        protected void OnViewChanged(object sender, PropertyChangedEventArgs args)
        {
            // We don't want to refresh the entire view after applying the change
            // to the model, so we need to temporarily detach the ModelChanged handler.
            //presenter.CommandHistory.ModelChanged -= OnModelChanged;

            // Figure out which property of which object is being changed.
            PropertyInfo property = propertyMap[args.ID].Property;
            object changedObject = propertyMap[args.ID].Model;

            object newValue = args.NewValue;

            // When using a multi-line text editor for an IEnumerable property, the
            // new value returned from the view will contain the enumerable with one
            // element per line. The 'canonical' form of an enumerable as recognised by
            // the StringToObject function is csv. Therefore we need to convert from
            // lf-separated elements to comma-separated elements. This should probably
            // occur somewhere else.
            DisplayAttribute attrib;
            if (newValue is string str && property.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && (attrib = property.GetCustomAttribute<DisplayAttribute>()) != null && attrib.Type == DisplayType.MultiLineText)
                newValue = string.Join(",", str.Split(Environment.NewLine.ToCharArray()));

            if (property.PropertyType.IsEnum && newValue is string enumDescription)
            {
                foreach (Enum value in Enum.GetValues(property.PropertyType))
                {
                    if (VariableProperty.GetEnumDescription(value) == enumDescription)
                    {
                        newValue = Enum.GetName(property.PropertyType, value);
                        break;
                    }
                }
            }

            // In some cases, the new value passed back from the view may be
            // already of the correct type. For example a boolean property
            // is editable via a checkbutton, so the view will return a bool.
            // However, most numbers are just rendered using an entry widget,
            // so the value from the view will be a string (e.g. 1e-6).
            if ((newValue == null || newValue is string) && property.PropertyType != typeof(string))
            {
                if (newValue is string modelName && typeof(IModel).IsAssignableFrom(property.PropertyType))
                    newValue = model.FindAllInScope(modelName).FirstOrDefault(m => property.PropertyType.IsAssignableFrom(m.GetType()));
                else
                    newValue = ReflectionUtilities.StringToObject(property.PropertyType, (string)newValue, CultureInfo.CurrentCulture);
            }

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