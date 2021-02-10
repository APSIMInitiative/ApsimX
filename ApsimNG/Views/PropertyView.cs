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
    using Extensions;
    using System.Reflection;

    /// <summary>
    /// This view will display a list of properties to the user
    /// in a GtkTable, with each row containing a label and an
    /// input component (e.g. an entry, combobox, checkbox, etc).
    /// </summary>
    /// <remarks>
    /// The <see cref="PropertyChanged" /> event is triggered differently
    /// for different input widgets:
    /// 
    /// - When a check button is toggled
    /// - When a dropdown selected item is changed
    /// - When a text editor (GtkEntry or GtkTextView) loses focus,
    ///   and its contents have been changed
    /// - After choosing file(s) in a file chooser dialog
    /// </remarks>
    public class PropertyView : ViewBase, IPropertyView
    {
        /// <summary>
        /// The main widget which holds the property table.
        /// </summary>
        private Frame box;

        /// <summary>
        /// Table which is used to layout property labels/inputs.
        /// </summary>
        /// <remarks>
        /// The table is destroyed and rebuilt from scratch when
        /// <see cref="DisplayProperties(PropertyGroup)" /> is called.
        /// </remarks>
#if NETFRAMEWORK
        private Table propertyTable;
#else
        private Grid propertyTable;
#endif

        /// <summary>
        /// Called when a property is changed by the user.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs> PropertyChanged;

        /// <summary>
        /// Used to check which entries are 'dirty' by keeping track
        /// of their original text.
        /// </summary>
        /// <remarks>
        /// The Guid is the ID of the entry/property.
        /// The string is the original text of the entry/value of the property.
        /// </remarks>
        private Dictionary<Guid, string> originalEntryText = new Dictionary<Guid, string>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">The owning view.</param>
        public PropertyView(ViewBase owner) : base(owner)
        {
            // Columns should not be homogenous - otherwise we'll have the
            // property name column taking up half the screen.
#if NETFRAMEWORK
            propertyTable = new Table(0, 0, false);
#else
            propertyTable = new Grid();
#endif
            box = new Frame("Properties");
            box.Add(propertyTable);
#if NETFRAMEWORK
            mainWidget = box;
#else
            Box container = new Box(Orientation.Vertical, 0);
            container.PackStart(box, false, false, 0);
            mainWidget = container;
#endif
            mainWidget.Destroyed += OnWidgetDestroyed;
        }

        /// <summary>
        /// Display properties and their values to the user.
        /// </summary>
        /// <param name="properties">Properties to be displayed/edited.</param>
        public void DisplayProperties(PropertyGroup properties)
        {
#if NETFRAMEWORK
            uint row = 0;
            uint col = 0;
#else
            int row = 0;
            int col = 0;
#endif
            bool widgetIsFocused = false;
            int entryPos = -1;
            int entrySelectionStart = 0;
            int entrySelectionEnd = 0;
#if NETFRAMEWORK
            // fixme - calls to propertyTable.ChildGetProperty result in a segfault on gtk3 builds.
            if (propertyTable.FocusChild != null)
            {
                object topAttach = propertyTable.ChildGetProperty(propertyTable.FocusChild, "top-attach").Val;
                object leftAttach = propertyTable.ChildGetProperty(propertyTable.FocusChild, "left-attach").Val;
                if (topAttach.GetType() == typeof(uint) && leftAttach.GetType() == typeof(uint))
                {
#if NETFRAMEWORK
                    row = (uint)topAttach;
                    col = (uint)leftAttach;
#else
                    row = (int)topAttach;
                    col = (int)leftAttach;
#endif
                    widgetIsFocused = true;
                    if (propertyTable.FocusChild is Entry entry)
                    {
                        entryPos = entry.Position;
                        entry.GetSelectionBounds(out entrySelectionStart, out entrySelectionEnd);
                    }
                }
            }
#endif
            box.Remove(propertyTable);
            box.Label = $"{properties.Name} Properties";

            propertyTable.Cleanup();

#if NETFRAMEWORK
            // Columns should not be homogenous - otherwise we'll have the
            // property name column taking up half the screen.
            propertyTable = new Table((uint)properties.Count(), 3, false);
#else
            propertyTable = new Grid();
            //propertyTable.RowHomogeneous = true;
            propertyTable.RowSpacing = 5;
#endif
            propertyTable.Destroyed += OnWidgetDestroyed;
            box.Add(propertyTable);

#if NETFRAMEWORK
            uint nrow = 0;
#else
            int nrow = 0;
#endif
            AddPropertiesToTable(ref propertyTable, properties, ref nrow);
            mainWidget.ShowAll();

            // If a widget was previously focused, then try to give it focus again.
            if (widgetIsFocused)
            {
                Widget widget = propertyTable.GetChild(row, col);
                if (widget is Entry entry)
                {
                    entry.GrabFocus();
                    if (entrySelectionStart >= 0 && entrySelectionStart < entrySelectionEnd && entrySelectionEnd <= entry.Text.Length)
                        entry.SelectRegion(entrySelectionStart, entrySelectionEnd);
                    else if (entryPos > -1 && entry.Text.Length >= entryPos)
                        entry.Position = entryPos;
                }
            }
        }

        /// <summary>
        /// Adds a group of properties to the GtkTable, starting at the specified row.
        /// </summary>
        /// <param name="table">Table to be modified.</param>
        /// <param name="properties">Property group to be modified.</param>
        /// <param name="startRow">The row to which the first property will be added (used for recursive calls).</param>
#if NETFRAMEWORK
        private void AddPropertiesToTable(ref Table table, PropertyGroup properties, ref uint startRow)
#else
        private void AddPropertiesToTable(ref Grid table, PropertyGroup properties, ref int startRow)
#endif
        {
            // Using a regular for loop is not practical because we can
            // sometimes have multiple rows per property (e.g. if it has separators).
            foreach (Property property in properties.Properties)
            {
                if (property.Separators != null)
                    foreach (string separator in property.Separators)
                    {
                        Label separatorLabel = new Label($"{separator}") { Xalign = 0, UseMarkup = true };
                        EventBox box = new EventBox();
                        box.Realized += OnSeparatorLabelRealized;
                        box.Add(separatorLabel);
#if NETFRAMEWORK
                        propertyTable.Attach(box, 0, 3, startRow, ++startRow, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 5, 5);
#else
                        propertyTable.Attach(box, 0, startRow, 2, 1);
                        startRow++;
#endif
                    }

                Label label = new Label(property.Name);
                label.TooltipText = property.Tooltip;
                label.Xalign = 0;
#if NETFRAMEWORK
                propertyTable.Attach(label, 0, 1, startRow, startRow + 1, AttachOptions.Fill, AttachOptions.Fill, 5, 0);
#else
                propertyTable.Attach(label, 0, startRow, 1, 1);
#endif

                if (!string.IsNullOrEmpty(property.Tooltip))
                {
                    Button info = new Button(new Image(Stock.Info, IconSize.Button));
                    info.TooltipText = property.Tooltip;
                    propertyTable.Attach(info, 1, 2, startRow, startRow + 1, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);
                    info.Clicked += OnInfoButtonClicked;
                }

                Widget inputWidget = GenerateInputWidget(property);
                inputWidget.Name = property.ID.ToString();
                inputWidget.TooltipText = property.Tooltip;
#if NETFRAMEWORK
                propertyTable.Attach(inputWidget, 2, 3, startRow, startRow + 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 0, 0);
#else
                propertyTable.Attach(inputWidget, 2, startRow, 1, 1);
                inputWidget.Hexpand = true;
#endif

                startRow++;
            }

            foreach (PropertyGroup subProperties in properties.SubModelProperties)
            {
                propertyTable.Attach(new Label($"<b>{subProperties.Name} Properties</b>") { Xalign = 0, UseMarkup = true }, 0, 2, startRow, ++startRow, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 0, 5);
                AddPropertiesToTable(ref table, subProperties, ref startRow);
            }
        }

        /// <summary>
        /// Called by the separator labels (well, technically by their
        /// parent EventBox) when they are realized. Changes the insensitive
        /// background colour to that of the normal background colour, to
        /// make the cells more distinct.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSeparatorLabelRealized(object sender, EventArgs e)
        {
            try
            {
                if (sender is Widget widget)
                {
#if NETFRAMEWORK
                    widget.ModifyBg(StateType.Normal, widget.Style.Background(StateType.Selected));
                    widget.ModifyFg(StateType.Normal, widget.Style.Background(StateType.Selected));
#else
                    // tbi
#endif
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
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
                case PropertyType.MultiLineText:
                    TextView editor = new TextView();
                    editor.SizeAllocated += OnTextViewSizeAllocated;
                    string text = ReflectionUtilities.ObjectToString(property.Value, CultureInfo.CurrentCulture);
                    editor.Buffer.Text = text ?? "";
                    originalEntryText[property.ID] = text;
                    editor.Name = property.ID.ToString();
                    Frame outline = new Frame();
                    outline.Add(editor);
                    component = outline;
                    editor.FocusOutEvent += OnEntryFocusOut;
                    break;
                case PropertyType.SingleLineText:
                    string entryValue = ReflectionUtilities.ObjectToString(property.Value, CultureInfo.InvariantCulture);
                    Entry textInput = new Entry(entryValue ?? "");
                    textInput.FocusOutEvent += OnEntryFocusOut;
                    component = textInput;
                    originalEntryText[property.ID] = textInput.Text;
                    break;
                case PropertyType.Checkbox:
                    CheckButton toggleButton = new CheckButton();
                    toggleButton.Active = (bool)property.Value;
                    toggleButton.Toggled += OnToggleCheckButton;
                    component = toggleButton;
                    break;
                case PropertyType.DropDown:
                    // Dropdown list - use a DropDownView (which wraps GtkComboBox).
                    DropDownView dropDown = new DropDownView(this);
                    dropDown.Values = property.DropDownOptions;
                    dropDown.SelectedValue = property.Value?.ToString();
                    dropDown.Changed += OnDropDownChanged;
                    component = dropDown.MainWidget;
                    break;
                case PropertyType.File:
                case PropertyType.Files:
                case PropertyType.Directory:
                //case PropertyType.Directories:
                    // Add an Entry and a Button inside a VBox.
                    Entry fileNameInput = new Entry(property.Value?.ToString() ?? "");
                    fileNameInput.Name = property.ID.ToString();
                    fileNameInput.FocusOutEvent += OnEntryFocusOut;
                    originalEntryText[property.ID] = fileNameInput.Text;

                    Button fileChooserButton = new Button("...");
                    fileChooserButton.Name = property.ID.ToString();
                    if (property.DisplayMethod == PropertyType.File)
                        fileChooserButton.Clicked += (o, _) => ChooseFile(o as Widget, false, false);
                    else if (property.DisplayMethod == PropertyType.Files)
                        fileChooserButton.Clicked += (o, _) => ChooseFile(o as Widget, true, false);
                    else if (property.DisplayMethod == PropertyType.Directory)
                        fileChooserButton.Clicked += (o, _) => ChooseFile(o as Widget, false, true);
                    
                    Box container = new HBox();
                    container.PackStart(fileNameInput, true, true, 0);
                    container.PackStart(fileChooserButton, false, false, 0);
                    component = container;
                    break;
                case PropertyType.Colour:
                    ColourDropDownView colourChooser = new ColourDropDownView(this);
                    List<object> colours = new List<object>();
                    foreach (var colour in ColourUtilities.Colours)
                        colours.Add(colour);
                    colourChooser.Values = colours.ToArray();
                    colourChooser.SelectedValue = property.Value;
                    colourChooser.Changed += OnDropDownChanged;
                    colourChooser.MainWidget.Name = property.ID.ToString();
                    component = colourChooser.MainWidget;
                    break;
                case PropertyType.Numeric:
                    SpinButton button = new SpinButton(double.MinValue, double.MaxValue, 1);
                    component = button;
                    if (property.Value == null)
                        button.Value = 0; // ?
                    else
                        button.Value = Convert.ToDouble(property.Value);
                    button.ValueChanged += OnNumberChanged;
                    break;
                case PropertyType.Font:
                    FontButton btnFont = new FontButton(property.Value?.ToString());
                    btnFont.FontSet += OnFontChanged;
                    component = btnFont;
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

        private void OnFontChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender is FontButton btnFont)
                {
                    Guid id = Guid.Parse(btnFont.Name);
                    PropertyChangedEventArgs args = new PropertyChangedEventArgs(id, btnFont.FontName);
                    PropertyChanged?.Invoke(this, args);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when a spinbutton is modified.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event data.</param>
        private void OnNumberChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender is SpinButton spinner)
                {
                    double newValue = spinner.Value;
                    Guid id = Guid.Parse(spinner.Name);
                    var args = new PropertyChangedEventArgs(id, newValue);
                    PropertyChanged?.Invoke(this, args);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when a TextView's size is allocated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnTextViewSizeAllocated(object sender, SizeAllocatedArgs args)
        {
            try
            {
                if (sender is TextView editor && propertyTable != null)
                {
                    Widget allocatedEntry = propertyTable.Children.FirstOrDefault(w => w is Entry && w.Allocation.Height > 0);
                    if (allocatedEntry != null)
                        editor.HeightRequest = Math.Max(editor.Allocation.Height, allocatedEntry.Allocation.Height);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when an entry or textview widget loses focus.
        /// Fires a chagned event if the widget's text has been modified.
        /// </summary>
        /// <param name="sender">The entry which has been modified.</param>
        /// <param name="e">Event data.</param>
        [GLib.ConnectBefore]
        private void OnEntryFocusOut(object sender, EventArgs e)
        {
            try
            {
                if (sender is Widget widget)
                {
                    Guid id = Guid.Parse(widget.Name);
                    string text;
                    if (widget is Entry entry)
                        text = entry.Text;
                    else if (widget is TextView editor)
                        text = editor.Buffer.Text;
                    else
                        throw new Exception($"Unknown widget type {sender.GetType().Name}");
                    if (originalEntryText.ContainsKey(id) && !string.Equals(originalEntryText[id], text, StringComparison.CurrentCulture))
                    {
                        var args = new PropertyChangedEventArgs(id, text);
                        originalEntryText[id] = text;
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
                else if (sender is ColourDropDownView colourChooser)
                {
                    var colour = (System.Drawing.Color)colourChooser.SelectedValue;
                    Guid id = Guid.Parse(colourChooser.MainWidget.Name);
                    var args = new PropertyChangedEventArgs(id, colour);
                    PropertyChanged?.Invoke(this, args);
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

                // If the user cancels the file selection, file will be null.
                if (file != null)
                {
                    if (button.Parent is Container container && container.Children.Length > 0 && container.Children[0] is Entry entry)
                        entry.Text = file;

                    Guid id = Guid.Parse(button.Name);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(id, file));
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Callback for a click event on the info/tooltip button.
        /// Causes the tooltip to be displayed.
        /// </summary>
        /// <remarks>
        /// Technically this could work for any event from any widget
        /// and would trigger a tooltip query.
        /// </remarks>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnInfoButtonClicked(object sender, EventArgs e)
        {
            try
            {
                // When the user clicks on the button, we want to immediately show the tooltip.
                // We can call Widget.TriggerTooltipQuery(), but the query to fail if the
                // tooltip timeout hasn't elapsed yet. What we have here is a gnarly workaround
                // for this problem. First, we get the current tooltip timeout duration. Then we
                // change it to 0 (ms), then we trigger the tooltip timeout, then we reset the
                // tooltip timeout to its original value so the user is none the wiser.
                if (sender is Widget widget)
                {
                    // Name of the tooltip timeout property.
                    string tooltipTimeout = "gtk-tooltip-timeout";

#if NETFRAMEWORK
                    // To get the default tooltip timeout, we need to call the GetProperty() method,
                    // which for some reason is a protected method in the gtk#2 API.
                    BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
                    MethodInfo method = mainWidget.Settings.GetType().GetMethod("GetProperty", flags);
                    GLib.Value result = (GLib.Value)method.Invoke(mainWidget.Settings, new object[1] { tooltipTimeout });
                    int timeout = (int)result.Val;

                    // Now set the tooltip timeout to 0ms.
                    mainWidget.Settings.SetLongProperty(tooltipTimeout, 0, "XProperty");

                    // Trigger a tooltip query on the button.
                    widget.TriggerTooltipQuery();

                    // Reset the tooltip timeout to the default value.
                    mainWidget.Settings.SetLongProperty(tooltipTimeout, timeout, "XProperty");
#else
                    int timeout = (int)mainWidget.GetProperty(tooltipTimeout).Val;
                    mainWidget.SetProperty(tooltipTimeout, new GLib.Value(0));
                    widget.TriggerTooltipQuery();
                    mainWidget.SetProperty(tooltipTimeout, new GLib.Value(timeout));
#endif
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
        private void OnWidgetDestroyed(object sender, EventArgs e)
        {
            if (sender is Widget widget)
            {
                widget.DetachAllHandlers();
            }
        }
    }
}