using APSIM.Shared.Utilities;
using UserInterface.EventArguments;
using UserInterface.Classes;
using Gtk;
using System;
using System.Collections.Generic;
using System.Reflection;
using Utility;
using System.Collections;
using Models.Core;
using System.Globalization;
using System.Runtime.InteropServices;

namespace UserInterface.Views
{
    /// <summary>
    /// A class for a dialog window for user settings.
    /// </summary>
    public class SettingsDialog : Dialog
    {
        private Window parentWindow = null;
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

            box = ContentArea;

            box.PackStart(propertyEditor.MainWidget, true, true, 0);
            parentWindow = parent;
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
                    {
                        ApplyChange(change.Key, change.Value);
                        CallOnChanged(change.Key);
                    }
                    Configuration.Settings.Save();
                    pendingChanges.Clear();
                }
            }
            while (response == ResponseType.Apply);
            this.Dispose();
        }

        private PropertyGroup GetPropertyGroup()
        {
            return new PropertyGroup("APSIM", GetProperties(), null);
        }

        private IEnumerable<Property> GetProperties()
        {
            bool isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            foreach (PropertyInfo property in typeof(Configuration).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (!isOSX || property.Name.CompareTo("DarkTheme") != 0)
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
                else if (property.PropertyType == typeof(string))
                    displayType = PropertyType.SingleLineText;
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

        private void CallOnChanged(PropertyInfo property)
        {
            InputAttribute attrib = property.GetCustomAttribute<InputAttribute>();
            if (!string.IsNullOrEmpty(attrib?.OnChanged))
            {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                MethodInfo method = typeof(Configuration).GetMethod(attrib.OnChanged, flags);
                if (method != null)
                    method.Invoke(Configuration.Settings, null);
            }
        }

        private void ApplyChange(PropertyInfo property, object newValue)
        {
            if (newValue != null && newValue.GetType() != property.PropertyType)
                newValue = ReflectionUtilities.StringToObject(property.PropertyType, newValue.ToString(), CultureInfo.CurrentCulture);
            property.SetValue(Configuration.Settings, newValue);
            if (property.Name.Equals("DarkTheme"))
            {
                ShowMsgDialog("Theme will be applied on next restart.",
                                "Restart Required",
                                Gtk.MessageType.Info,
                                ButtonsType.Ok,
                                parentWindow);
            }
        }

        /// <summary>Show a message in a dialog box</summary>
        /// <param name="message">The message.</param>
        /// <param name="title">Title of the dialog.</param>
        /// <param name="msgType">Message type (info, warning, error, ...).</param>
        /// <param name="buttonType">Type of buttons to be shown in the dialog.</param>
        /// <param name="errorLevel">The error level.</param>
        /// <param name="masterWindow">The main window.</param>
        public int ShowMsgDialog(string message, string title, Gtk.MessageType msgType, Gtk.ButtonsType buttonType, Window masterWindow)
        {
            MessageDialog md = new Gtk.MessageDialog(masterWindow, Gtk.DialogFlags.Modal,
                msgType, buttonType, message);
            md.Title = title;
            md.WindowPosition = WindowPosition.Center;
            int result = md.Run();
            md.Dispose();
            return result;
        }
    }
}