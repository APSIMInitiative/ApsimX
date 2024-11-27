using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using APSIM.Shared.Utilities;
using Shared.Utilities;
using UserInterface.Classes;
using UserInterface.EventArguments;
using Gtk;
using UserInterface.Interfaces;
using Utility;

namespace UserInterface.Views
{
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
        protected Frame box;

        /// <summary>
        /// Table which is used to layout property labels/inputs.
        /// </summary>
        /// <remarks>
        /// The table is destroyed and rebuilt from scratch when
        /// <see cref="DisplayProperties(PropertyGroup)" /> is called.
        /// </remarks>

        protected Grid propertyTable;


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
        /// List of old property tables to be disposed of when this PropertyView
        /// instance is disposed of.
        /// </summary>
        private readonly List<Grid> oldPropertyTables = new List<Grid>();

        /// <summary>
        /// Flag to prevent double disposal.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// The values for the vertical scrollbar
        /// </summary>
        private ScrollerAdjustmentValues scrollV { get; set; } = null;
        
        /// <summary>
        /// List of code editor views that have been created
        /// </summary>
        private List<EditorView> codeEditors = new List<EditorView>();

        /// <summary>Constructor.</summary>
        public PropertyView() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">The owning view.</param>
        public PropertyView(ViewBase owner) : base(owner)
        {
            ScrolledWindow scroller = new ScrolledWindow();
            Initialise(owner, scroller);
        }

        /// <summary>Any properties displayed in the grid?</summary>
        public bool AnyProperties => propertyTable.Children.Length > 0;

        /// <summary>
        /// Initialise the view.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="gtkControl"></param>
        protected override void Initialise(ViewBase owner, GLib.Object gtkControl)
        {
            var scroller = gtkControl as ScrolledWindow;

            // Columns should not be homogenous - otherwise we'll have the
            // property name column taking up half the screen.

            propertyTable = new Grid();
            codeEditors = new List<EditorView>();

            box = new Frame();
            box.ShadowType = ShadowType.None;
            box.Add(propertyTable);
            mainWidget = scroller;

            Box container = new Box(Orientation.Vertical, 0);
            container.Margin = 10;
            container.PackStart(box, false, false, 0);
            scroller.Add(container);
            scroller.PropagateNaturalHeight = true;
            scroller.PropagateNaturalWidth = true;

            mainWidget.Destroyed += mainWidget_Destroyed;
        }

        /// <summary>
        /// Display properties and their values to the user.
        /// </summary>
        /// <param name="properties">Properties to be displayed/edited.</param>
        public virtual void DisplayProperties(PropertyGroup properties)
        {
            // Get the row/column indices of the child widget with focus.
            (int row, int col) = GetFocusChildIndices(propertyTable);

            // Dispose of current properties table.
            box.Remove(propertyTable);

            // We don't really want to destroy the old property table yet,
            // because it may have pending events. Destroying the widget in such
            // a scenario can lead to undesirable results (such as a crash). To
            // avoid this, we just add the table into a list of widgets to be
            // cleaned up later.
            oldPropertyTables.Add(propertyTable);

            // Construct a new properties table.
            propertyTable = new Grid();
            //propertyTable.RowHomogeneous = true;
            propertyTable.RowSpacing = 5;

            propertyTable.Destroyed += PropertyTable_Destroyed;
            box.Add(propertyTable);

            int nrow = 0;

            // Add the properties to the new properties table.
            AddPropertiesToTable(ref propertyTable, properties, ref nrow, 0);

            if (nrow > 0)
                mainWidget.ShowAll();
            else
                mainWidget.Hide();

            // If a widget was previously focused, then try to give it focus again.
            if (row >= 0 && col >= 0)
            {
                Widget widget = propertyTable.GetChildAt(col, row);
                if (widget != null && widget as Entry != null)
                    (widget as Entry).GrabFocus();
            }

            if (scrollV != null)
            {
                ScrolledWindow scroller = mainWidget as ScrolledWindow;
                scroller.Vadjustment?.Configure(scrollV.Value, 
                                            scrollV.Lower, 
                                            scrollV.Upper, 
                                            scrollV.StepIncrement, 
                                            scrollV.PageIncrement, 
                                            scrollV.PageSize);
            }
        }

        /// <summary>
        /// Dispose of old property tables.
        /// </summary>
        /// <param name="disposing">
        /// True iff being called by manually (as opposed to by the garbage
        /// collector) This doesn't really matter for the purposes of this
        /// particular Dispose() implementation.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                box.Remove(propertyTable);
                oldPropertyTables.Add(propertyTable);

                foreach (Grid grid in oldPropertyTables)
                {
                    grid.DetachAllHandlers();
                    grid.Destroy();
                    grid.Dispose();
                }

                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Adds a group of properties to the GtkTable, starting at the specified row.
        /// </summary>
        /// <param name="table">Table to be modified.</param>
        /// <param name="properties">Property group to be modified.</param>
        /// <param name="startRow">The row to which the first property will be added (used for recursive calls).</param>
        /// <param name="columnOffset">The number of columns to offset for this propertygroup (0 = single. >0 multiply models reported as columns).</param>
        protected void AddPropertiesToTable(ref Grid table, PropertyGroup properties, ref int startRow, int columnOffset)
        {
            // Using a regular for loop is not practical because we can
            // sometimes have multiple rows per property (e.g. if it has separators).
            foreach (Property property in properties.Properties.Where(p => p.Visible))
            {
                if (property.Separators != null)
                    foreach (string separator in property.Separators)
                    {
                        Label separatorLabel = new Label($"<b>{separator}</b>") { Xalign = 0, UseMarkup = true };
                        separatorLabel.StyleContext.AddClass("separator");
                        propertyTable.Attach(separatorLabel, 0, startRow, 3, 1);

                        startRow++;
                    }

                if (columnOffset == 0) // only perform on first or only entry
                {
                    Label label = new Label(property.Name);
                    label.TooltipText = property.Tooltip;
                    label.Xalign = 0;

                    label.MarginEnd = 10;
                    propertyTable.Attach(label, 0, startRow, 1, 1);
                }

                if (!string.IsNullOrEmpty(property.Tooltip))
                {
                    Button info = new Button(new Image(Stock.Info, IconSize.Button));
                    info.TooltipText = property.Tooltip;
                    propertyTable.Attach(info, 1, startRow, 1, 1);
                    info.Clicked += OnInfoButtonClicked;
                }

                Widget inputWidget = GenerateInputWidget(property);
                inputWidget.Name = property.ID.ToString();
                inputWidget.TooltipText = property.Tooltip;

                propertyTable.Attach(inputWidget, 2 + columnOffset, startRow, 1, 1);
                inputWidget.Hexpand = true;

                startRow++;
            }

            foreach (PropertyGroup subProperties in properties.SubModelProperties)
            {
                Label label = new Label($"<b>{subProperties.Name} Properties</b>");
                label.Xalign = 0;
                label.UseMarkup = true;
                propertyTable.Attach(label, 0, startRow, 2, 1);
                label.Ypad = 5;
                startRow++;
                AddPropertiesToTable(ref table, subProperties, ref startRow, columnOffset);
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
                    editor.FocusOutEvent += UpdateText;
                    editor.KeyPressEvent += UpdateText;
                    break;
                case PropertyType.Code:
                    EditorView codeEditor = new EditorView(this);
                    string code = ReflectionUtilities.ObjectToString(property.Value, CultureInfo.CurrentCulture);
                    if (code.Length > 0 && code[code.Length - 1] != '\n') //add a line if there is not a blank at the end
                        code += '\n';
                    codeEditor.Text = code;
                    Frame codeOutline = new Frame();
                    codeEditor.MainWidget.Name = property.ID.ToString();
                    codeEditor.MainWidget.TooltipText = property.Name;
                    codeEditor.DisposeEditor += OnEditorChange;
                    codeOutline.Add(codeEditor.MainWidget);
                    codeOutline.HeightRequest = 100;
                    component = codeOutline;
                    originalEntryText[property.ID] = code;
                    this.codeEditors.Add(codeEditor);
                    codeEditor.LeaveEditor += OnEditorChange;
                    break;
                case PropertyType.SingleLineText:
                    string entryValue = ReflectionUtilities.ObjectToString(property.Value, CultureInfo.InvariantCulture);
                    Entry textInput = new Entry(entryValue ?? "");
                    textInput.FocusOutEvent += UpdateText;
                    textInput.KeyPressEvent += UpdateText;
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
                    dropDown.Values = new string[1] { "" };
                    if (property.DropDownOptions != null)
                        dropDown.Values = dropDown.Values.Concat(property.DropDownOptions).ToArray();
                    dropDown.SelectedValue = property.Value?.ToString();
                    dropDown.Changed += OnDropDownChanged;
                    component = dropDown.MainWidget;
                    break;
                case PropertyType.File:
                case PropertyType.Directory:
                    //case PropertyType.Directories:
                    // Add an Entry and a Button inside a Box.
                    Entry fileNameInput = new Entry(property.Value?.ToString() ?? "");
                    fileNameInput.Name = property.ID.ToString();
                    fileNameInput.FocusOutEvent += UpdateText;
                    originalEntryText[property.ID] = fileNameInput.Text;

                    Button fileChooserButton = new Button("...");
                    fileChooserButton.Name = property.ID.ToString();
                    if (property.DisplayMethod == PropertyType.File)
                        fileChooserButton.Clicked += (o, _) => ChooseFile(o as Widget, false, false);
                    else if (property.DisplayMethod == PropertyType.Directory)
                        fileChooserButton.Clicked += (o, _) => ChooseFile(o as Widget, false, true);

                    Box container = new Box(Orientation.Horizontal, 0);
                    container.PackStart(fileNameInput, true, true, 0);
                    container.PackStart(fileChooserButton, false, false, 0);
                    component = container;
                    break;
                case PropertyType.Files:
                    string fileNamesText = "";
                    if (property.Value != null)
                    {
                        string[] filenamesArray = ReflectionUtilities.StringToObject(typeof(string[]), property.Value.ToString(), CultureInfo.CurrentCulture) as string[];
                        fileNamesText = string.Join("\n", filenamesArray) + "\n";
                    }
                    TextView filenamesEditor = new TextView();
                    filenamesEditor.SizeAllocated += OnTextViewSizeAllocated;
                    filenamesEditor.WrapMode = WrapMode.Word;
                    filenamesEditor.Buffer.Text = fileNamesText;
                    originalEntryText[property.ID] = fileNamesText;
                    filenamesEditor.Name = property.ID.ToString();
                    filenamesEditor.FocusOutEvent += UpdateText;

                    Frame filenamesOutline = new(){ filenamesEditor };
                    Button filesChooserButton = new("..."){ Name = property.ID.ToString() };
                    filesChooserButton.Clicked += (o, _) => ChooseFile(o as Widget, true, false);

                    Box filenamesContainer = new HBox();
                    filenamesContainer.PackStart(filenamesOutline, true, true, 0);
                    filenamesContainer.PackStart(filesChooserButton, false, false, 0);
                    component = filenamesContainer;
                    

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
                case PropertyType.ColourPicker:
                    ColorButton colourPicker = new ColorButton();
                    if (property.Value is System.Drawing.Color color)
                        colourPicker.Rgba = color.ToRGBA();
                    colourPicker.Xalign = 0;
                    colourPicker.WidthRequest = 350;
                    colourPicker.Name = property.ID.ToString();
                    colourPicker.ColorSet += OnColourChanged;
                    component = colourPicker;
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

            component.Sensitive = property.Enabled;

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
                    StoreScrollerPosition();
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
                    StoreScrollerPosition();
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
                    StoreScrollerPosition();
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
        private void UpdateText(object sender, EventArgs e)
        {
            try
            {
                bool doUpdate = false;
                if (e is KeyPressEventArgs) 
                {
                    if (!(sender is TextView) && (e as KeyPressEventArgs).Event.Key == Gdk.Key.Return)
                        doUpdate = true;
                }
                else 
                {
                    doUpdate = true;
                }
                if (doUpdate) 
                {
                    Widget widget = null;
                    if (sender is Widget)
                        widget = sender as Widget;
                    else if (sender is ViewBase)
                        widget = (sender as ViewBase).MainWidget;

                    if (widget != null)
                    {
                    	StoreScrollerPosition();
                        Guid id = Guid.Parse(widget.Name);
                        string text;
                        if (widget is Entry entry)
                            text = entry.Text;
                        else if (widget is TextView editor) {
                            text = editor.Buffer.Text;
                            text = text.Replace("\n", ", "); //if this is a "one thing per line", convert back to one line string
                            if (text.EndsWith(", "))
                                text = text.Remove(text.Length-2, 2);
                        }
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
                
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when a code editor changes texts
        /// Fires a chagned event if the editor's text has been modified.
        /// </summary>
        /// <param name="sender">The entry which has been modified.</param>
        /// <param name="e">Event data.</param>
        [GLib.ConnectBefore]
        private void OnEditorChange(object sender, EventArgs e)
        {
            try
            {
                Guid id = new Guid((sender as EditorView).MainWidget.Name);
                string text = (sender as EditorView).Text;

                //trim each line of the text and remove empty lines
                string[] lines = text.Split('\n');
                string trimmed = "";
                foreach (string line in lines) 
                {
                    string output = line.Trim();
                    if (output.Length > 0)
                        trimmed += line + "\n";
                }

                if (originalEntryText.ContainsKey(id) && !string.Equals(originalEntryText[id], trimmed, StringComparison.CurrentCulture))
                {
                    var args = new PropertyChangedEventArgs(id, trimmed);
                    originalEntryText[id] = trimmed;
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
                    StoreScrollerPosition();
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
                    StoreScrollerPosition();
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
        /// Called when a colour is picked from the colour picker
        /// </summary>
        /// <param name="sender">The GtkComboBox widget which has been changed.</param>
        /// <param name="e">Event arguments.</param>
        private void OnColourChanged(object sender, EventArgs e)
        {
            try
            {
                var gtkcolour = (sender as ColorButton).Rgba.ToColour().ToGdk();
                var colour = Utility.Colour.FromGtk(gtkcolour);
                Guid id = Guid.Parse((sender as ColorButton).Name);
                var args = new PropertyChangedEventArgs(id, colour);
                PropertyChanged?.Invoke(this, args);
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
                    StoreScrollerPosition();
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
        /// Get the row and column indices of the child of the grid which has
        /// focus. Return (-1, -1) if no children have focus.
        /// </summary>
        /// <param name="row">Row index of the child with focus, or -1 if no children have focus.</param>
        /// <param name="grid">Column index of the child with focus, or -1 if no children have focus.</param>
        private (int row, int col) GetFocusChildIndices(Grid grid)
        {
            // Check if a widget currently has the focus. If so, we should
            // attempt to give focus back to this widget after rebuilding the
            // properties table.
            if (propertyTable.FocusChild != null)
            {
                object topAttach = propertyTable.ChildGetProperty(propertyTable.FocusChild, "top-attach").Val;
                object leftAttach = propertyTable.ChildGetProperty(propertyTable.FocusChild, "left-attach").Val;
                if (topAttach.GetType() == typeof(int) && leftAttach.GetType() == typeof(int))
                {
                    int row = (int)topAttach;
                    int col = (int)leftAttach;
                    return (row, col);
                }
            }

            return (-1, -1);
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
                StoreScrollerPosition();
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
                if (sender is Widget widget)
                    widget.TriggerTooltipQuery();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void StoreScrollerPosition()
        {
            ScrolledWindow scroller = mainWidget as ScrolledWindow;
            this.scrollV = new ScrollerAdjustmentValues(scroller.Vadjustment.Value,
                                                        scroller.Vadjustment.Lower,
                                                        scroller.Vadjustment.Upper,
                                                        scroller.Vadjustment.StepIncrement,
                                                        scroller.Vadjustment.PageIncrement,
                                                        scroller.Vadjustment.PageSize);
        }

        /// <summary>
        /// Called when the main widget is destroyed, which occurs when the
        /// user clicks on another node in the UI, or when the properties
        /// list is refreshed.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        protected void mainWidget_Destroyed(object sender, EventArgs e)
        {
            propertyTable.Destroy();
            propertyTable.Dispose();
            mainWidget.DetachAllHandlers();
            mainWidget.Destroyed -= mainWidget_Destroyed;

        }

        protected void PropertyTable_Destroyed(object sender, EventArgs e)
        {
            propertyTable.DetachAllHandlers();
            propertyTable.Destroyed -= PropertyTable_Destroyed;
        }

        /// <summary>
        /// Fire off a PropertyChanged event for any outstanding changes.
        /// </summary>
        public void SaveChanges()
        {
            // The only widget which can have outstanding changes is an entry,
            // whose changes are applied when it loses focus. Therefore,
            // grabbing focus on the main widget will cause any focused entries
            // to lose focus and fire off a changed event.
            mainWidget.CanFocus = true;
            mainWidget.GrabFocus();
        }

        /// <summary>
        /// Returns a list of all code editor views that have been created.
        /// Used by the presenter to connect up intellisense events.
        /// </summary>
        public List<EditorView> GetAllEditorViews()
        {
            return this.codeEditors;
        }

        /// <summary>
        /// Clear code editors
        /// </summary>
        public void DeleteEditorViews()
        {
            codeEditors = new List<EditorView>();
        }
    }
}