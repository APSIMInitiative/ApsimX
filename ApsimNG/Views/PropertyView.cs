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
    using APSIM.Shared.Utilities;
    using System.Globalization;

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
        /// Table which is used to layout property labels/inputs.
        /// </summary>
        /// <remarks>
        /// The table is destroyed and rebuilt from scratch when
        /// <see cref="DisplayProperties()" /> is called.
        /// </remarks>
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
            mainWidget.Destroyed += OnWidgetDestroyed;
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
            propertyTable.Destroyed += OnWidgetDestroyed;
            box.PackStart(propertyTable, true, true, 0);

            // Using a regular for loop is not practical because we can
            // sometimes have multiple rows per property (e.g. if it has separators).
            uint i = 0;
            foreach (Property property in properties)
            {
                if (property.Separators != null)
                    foreach (string separator in property.Separators)
                        propertyTable.Attach(new Label(separator) { Xalign = 0 }, 0, 2, i, ++i, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 0, 5);

                Label label = new Label(property.DisplayName);
                label.TooltipText = property.Tooltip;
                label.Xalign = 0;
                propertyTable.Attach(label, 0, 1, i, i + 1, AttachOptions.Fill, AttachOptions.Fill, 5, 0);

                Widget inputWidget = GenerateInputWidget(property);
                inputWidget.Name = property.ID.ToString();
                inputWidget.TooltipText = property.Tooltip;
                propertyTable.Attach(inputWidget, 1, 2, i, i + 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 0, 0);

                i++;
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
                    if (property.DataType == typeof(bool))
                    {
                        CheckButton toggleButton = new CheckButton();
                        toggleButton.Toggled += OnToggleCheckButton;
                        toggleButton.Active = (bool)property.Value;
                        component = toggleButton;
                    }
                    else
                    {
                        // Default - just a simple text input (GtkEntry).
                        string entryValue = ReflectionUtilities.ObjectToString(property.Value, CultureInfo.InvariantCulture);
                        Entry textInput = new Entry(entryValue ?? "");
                        textInput.FocusOutEvent += OnEntryChanged;
                        component = textInput;
                    }
                    break;
                case DisplayType.DropDown:
                    // Dropdown list - use a DropDownView (which wraps GtkComboBox).
                    DropDownView dropDown = new DropDownView(this);
                    dropDown.Values = property.DropDownOptions;
                    dropDown.SelectedValue = property.Value?.ToString();
                    dropDown.Changed += OnDropDownChanged;
                    component = dropDown.MainWidget;
                    break;
                case DisplayType.FileName:
                case DisplayType.FileNames:
                case DisplayType.DirectoryName:
                    // Add an Entry and a Button inside a VBox.
                    Entry fileNameInput = new Entry(property.Value?.ToString() ?? "");
                    fileNameInput.Name = property.ID.ToString();
                    fileNameInput.FocusOutEvent += OnEntryChanged;

                    Button fileChooserButton = new Button("...");
                    fileChooserButton.Name = property.ID.ToString();
                    if (property.DisplayMethod == DisplayType.FileName)
                        fileChooserButton.Clicked += (o, _) => ChooseFile(o as Widget, false, false);
                    else if (property.DisplayMethod == DisplayType.FileNames)
                        fileChooserButton.Clicked += (o, _) => ChooseFile(o as Widget, true, false);
                    else if (property.DisplayMethod == DisplayType.DirectoryName)
                        fileChooserButton.Clicked += (o, _) => ChooseFile(o as Widget, false, true);
                    
                    Box container = new HBox();
                    container.PackStart(fileNameInput, true, true, 0);
                    container.PackStart(fileChooserButton, false, false, 0);
                    component = container;
                    break;
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
                    Guid id = Guid.Parse(component.Name);
                    var args = new PropertyChangedEventArgs(id, component.Text);
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
                if (sender is DropDownView dropdown)
                {
                    ComboBox combo = (ComboBox)dropdown.MainWidget;
                    if (combo.GetActiveIter(out TreeIter iter))
                    {
                        string newValue = combo.Model.GetValue(iter, 0)?.ToString();
                        Guid id = Guid.Parse(combo.Name);
                        var args = new PropertyChangedEventArgs(id, newValue);
                        PropertyChanged?.Invoke(this, args);
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when a check button is toggled by the user.
        /// </summary>
        /// <param name="sender">Sending object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnToggleCheckButton(object sender, EventArgs e)
        {
            try
            {
                if (sender is CheckButton checkButton)
                {
                    Guid id = Guid.Parse(checkButton.Name);
                    var args = new PropertyChangedEventArgs(id, checkButton.Active);
                    PropertyChanged?.Invoke(this, args);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Runs a file chooser dialog according to specified parameters,
        /// then updates the text in the preceeding Entry (button.Parent.Children[0]),
        /// then fires off a PropertyChanged event.
        /// </summary>
        /// <param name="button">The button which was clicked.</param>
        /// <param name="chooseMultiple">Allow the user to select multiple files?</param>
        /// <param name="chooseDirectory">Allow the user to select a directory?</param>
        private void ChooseFile(Widget button, bool chooseMultiple, bool chooseDirectory)
        {
            try
            {
                IFileDialog fileChooser = new FileDialog()
                {
                    FileType = "All files (*.*)|*.*",
                    Prompt = "Choose a file"
                };
                if (chooseDirectory)
                    fileChooser.Action = FileDialog.FileActionType.SelectFolder;
                else
                    fileChooser.Action = FileDialog.FileActionType.Open;

                string file;
                if (chooseMultiple)
                    file = string.Join(", ", fileChooser.GetFiles());
                else
                    file = fileChooser.GetFile();

                if (button.Parent is Container container && container.Children.Length > 0 && container.Children[0] is Entry entry)
                    entry.Text = file;

                Guid id = Guid.Parse(button.Name);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(id, file));
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
        private void OnWidgetDestroyed(object sender, EventArgs e)
        {
            if (sender is Widget widget)
            {
                widget.DetachHandlers();
                if (widget is Container container)
                    foreach (Widget child in container.Children)
                        child.DetachHandlers();
            }
        }
    }
}