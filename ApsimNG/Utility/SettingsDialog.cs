namespace UserInterface.Views
{
    using APSIM.Shared.Utilities;
    using EventArguments;
    using Classes;
    using Gtk;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Utility;
    using System.Collections;
    using Models.Core;
    using System.Globalization;
    using Extensions;

    /// <summary>
    /// A class for a dialog window for user settings.
    /// </summary>
    public class SettingsDialog : Dialog
    {
        private PropertyView propertyEditor;
        Dictionary<Guid, PropertyInfo> properties = new Dictionary<Guid, PropertyInfo>();
        List<KeyValuePair<PropertyInfo, object>> pendingChanges = new List<KeyValuePair<PropertyInfo, object>>();

        public SettingsDialog(Window parent) : base("Settings",
                                                    parent,
                                                    DialogFlags.Modal,
                                                    new object[]
                                                    {
                                                        "Cancel", ResponseType.Cancel,
                                                        "Apply", ResponseType.Apply,
                                                        "OK", ResponseType.Ok
                                                    })
        {
            propertyEditor = new PropertyView(null);
            Box box;
#if NETFRAMEWORK
            box = VBox;
#else
            box = ContentArea;
#endif
            box.PackStart(propertyEditor.MainWidget, true, true, 0);
            propertyEditor.MainWidget.ShowAll();
            propertyEditor.PropertyChanged += OnPropertyChanged;
            Refresh();
        }

        public void Refresh()
        {
            propertyEditor.DisplayProperties(GetPropertyGroup());
        }

        public new void Run()
        {
            ResponseType response;
            do
            {
                response = (ResponseType)base.Run();
                if (response == ResponseType.Ok || response == ResponseType.Apply)
                {
                    foreach (KeyValuePair<PropertyInfo, object> change in pendingChanges)
                        ApplyChange(change.Key, change.Value);
                    Configuration.Settings.Save();
                    pendingChanges.Clear();
                }
            }
            while (response == ResponseType.Apply);
            this.Cleanup();
        }

        private PropertyGroup GetPropertyGroup()
        {
            return new PropertyGroup("APSIM", GetProperties(), null);
        }

        private IEnumerable<Property> GetProperties()
        {
            foreach (PropertyInfo property in typeof(Configuration).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (IsUserInput(property))
                    yield return GetProperty(property, Configuration.Settings);
        }

        private Property GetProperty(PropertyInfo property, object instance)
        {
            InputAttribute attrib = property.GetCustomAttribute<InputAttribute>();
            if (attrib == null)
                throw new ArgumentException($"Property {property.Name} does not have an Input attribute");
            PropertyType displayType;
            if (attrib is FontInput)
                displayType = PropertyType.Font;
            else if (attrib is FileInput)
            {
                if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                    displayType = PropertyType.Files;
                else
                    displayType = PropertyType.File;
            }
            else if (attrib.GetType() == typeof(InputAttribute))
            {
                if (property.PropertyType == typeof(bool))
                    displayType = PropertyType.Checkbox;
                else if (ReflectionUtilities.IsNumericType(property.PropertyType))
                    displayType = PropertyType.Numeric;
                else
                    throw new NotImplementedException($"Unable to handle property type {property.PropertyType.Name}");
            }
            else
                throw new NotImplementedException($"Unknown input attribute type {attrib.GetType().Name}");
            string tooltip = property.GetCustomAttribute<TooltipAttribute>()?.Tooltip;
            object value = property.GetValue(instance);
            Property p = new Property(attrib.Name, tooltip, value, displayType);
            properties[p.ID] = property;
            return p;
        }

        private bool IsUserInput(PropertyInfo property)
        {
            return property.GetCustomAttribute<InputAttribute>() != null;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            pendingChanges.Add(new KeyValuePair<PropertyInfo, object>(properties[args.ID], args.NewValue));
        }

        private void ApplyChange(PropertyInfo property, object newValue)
        {
            if (newValue != null && newValue.GetType() != property.PropertyType)
                newValue = ReflectionUtilities.StringToObject(property.PropertyType, newValue.ToString(), CultureInfo.CurrentCulture);
            property.SetValue(Configuration.Settings, newValue);
        }
    }
}