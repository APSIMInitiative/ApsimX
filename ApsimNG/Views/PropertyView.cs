namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using Interfaces;
    using Classes;
    using Gtk;
    using Utility;
    using System.Linq;
    using Models.Core;
    using EventArguments;

    /// <summary>
    /// This view will display a list of properties to the user
    /// in a GtkTable, with each row containing a label and an
    /// input component (e.g. an entry, combobox, checkbox, etc).
    /// </summary>
    public class PropertyView : ViewBase, IPropertyView
    {
        /// <summary>
        /// The main widget which holds the property table.
        /// </summary>
        private Box box;

        /// <summary>
        /// Used to layout property labels/inputs.
        /// </summary>
        private Table propertyTable;

        /// <summary>
        /// Called when a property is changed by the user.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs> PropertyChanged;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">The owning view.</param>
        public PropertyView(ViewBase owner) : base(owner)
        {
            propertyTable = new Table(0, 0, true);

            box = new VBox();
            box.PackStart(propertyTable, true, true, 0);
            mainWidget = box;
            mainWidget.Destroyed += OnMainWidgetDestroyed;
        }

        /// <summary>
        /// Display properties and their values to the user.
        /// </summary>
        /// <param name="properties">Properties to be displayed/edited.</param>
        public void DisplayProperties(IEnumerable<Property> properties)
        {
            box.Remove(propertyTable);
            propertyTable.Destroy();
            propertyTable = new Table((uint)properties.Count(), 2, false);
            box.PackStart(propertyTable, true, true, 0);

            for (int i = 0; i < properties.Count(); i++)
            {
                Property property = properties.ElementAt(i);
                Label label = new Label(property.Name);
                label.TooltipText = property.Tooltip;
                label.Xalign = 0;
                propertyTable.Attach(label, 0, 1, (uint)i, (uint)i + 1, AttachOptions.Fill, AttachOptions.Fill, 5, 0);

                Widget inputWidget = GenerateInputWidget(property);
                inputWidget.Name = property.Name;
                inputWidget.TooltipText = property.Tooltip;
                propertyTable.Attach(inputWidget, 1, 2, (uint)i, (uint)i + 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 0, 0);
            }
            mainWidget.ShowAll();
        }

        /// <summary>
        /// Generate and initialise input widget for the given property.
        /// This involves attaching the appropriate event handlers and
        /// populating it with an initial value.
        /// </summary>
        /// <param name="property">The property to be presented.</param>
        private Widget GenerateInputWidget(Property property)
        {
            Widget component;
            switch (property.DisplayMethod)
            {
                case DisplayType.None:
                    Entry input = new Entry(property.Value?.ToString() ?? "");
                    input.FocusOutEvent += OnEntryChanged;
                    component = input;
                    break;
                case DisplayType.DropDown:
                    DropDownView dropDown = new DropDownView(this);
                    dropDown.Values = property.DropDownOptions;
                    dropDown.SelectedValue = property.Value?.ToString();
                    dropDown.Changed += OnDropDownChanged;
                    component = dropDown.MainWidget;
                    break;
                //case DisplayType.FileName:
                //case DisplayType.FileNames:
                //case DisplayType.DirectoryName:
                //    break;
                default:
                    throw new Exception($"Unknown display type {property.DisplayMethod}");
            }

            // Set the widget's name to the property name.
            // This allows us to provide the property name when firing off
            // the property changed event, despite the event handlers being
            // shared by multiple components.
            return component;
        }

        /// <summary>
        /// Called when an entry widget has been modified by the user.
        /// </summary>
        /// <param name="sender">The entry which has been modified.</param>
        /// <param name="e">Event data.</param>
        [GLib.ConnectBefore]
        private void OnEntryChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender is Entry component)
                {
                    var args = new PropertyChangedEventArgs(component.Name, component.Text);
                    PropertyChanged?.Invoke(this, args);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when a dropdown has been changed by the user.
        /// </summary>
        /// <param name="sender">The GtkComboBox widget which has been changed.</param>
        /// <param name="e">Event arguments.</param>
        private void OnDropDownChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender is ComboBox combo && combo.GetActiveIter(out TreeIter iter))
                {
                    string newValue = combo.Model.GetValue(iter, 0)?.ToString();
                    var args = new PropertyChangedEventArgs(combo.Name, newValue);
                    PropertyChanged?.Invoke(this, args);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the main widget is destroyed, which occurs when the
        /// user clicks on another node in the UI, or when the properties
        /// list is refreshed.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnMainWidgetDestroyed(object sender, EventArgs e)
        {
            Console.WriteLine($"Destroying main widget. HasFocus={propertyTable.HasFocus}");
            propertyTable.DetachHandlers();
            foreach (Widget widget in propertyTable.Children)
                widget.DetachHandlers();
        }
    }
}