# if NETCOREAPP
using TreeModel = Gtk.ITreeModel;
#endif

namespace UserInterface.Views
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Gtk;
    using Interfaces;
    using Extensions;

    /// <summary>An interface for a list box</summary>
    public interface IListBoxView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        event EventHandler Changed;

        /// <summary>Invoked when the user double clicks the selection</summary>
        event EventHandler DoubleClicked;

        /// <summary>Get or sets the list of valid values.</summary>
        string[] Values { get; set; }

        /// <summary>Gets or sets the selected value.</summary>
        string SelectedValue { get; set; }

        /// <summary>Return true if dropdown is visible.</summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// If true, we are display a list of models
        /// This will turn on display of images and drag-drop logic
        /// </summary>
        bool IsModelList { get; set; }

        /// <summary>
        /// Populates a context menu
        /// </summary>
        /// <param name="menuDescriptions"></param>
        void PopulateContextMenu(List<MenuDescriptionArgs> menuDescriptions);

        /// <summary>
        /// Invoked when a drag operation has commenced. Need to create a DragObject.
        /// </summary>
        event EventHandler<DragStartArgs> DragStarted;
    }

    public class IkonView : IconView
    {
        public IkonView(TreeModel model) : base(model) { }

#if NETFRAMEWORK
        // ItemPadding is included in the GtkSharp API but not in gtk-sharp (the gtk2 wrapper).
        public int ItemPadding
        {
            get { return (int)GetProperty("item-padding"); }
            set { SetProperty("item-padding", new GLib.Value(value)); }
        }
#endif
    }

    /// <summary>A list view.</summary>
    public class ListBoxView : ViewBase, IListBoxView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler Changed;

        /// <summary>Invoked when the user double clicks the selection</summary>
        public event EventHandler DoubleClicked;

        public IkonView Listview { get; set; }

        private ListStore listmodel = new ListStore(typeof(string), typeof(Gdk.Pixbuf), typeof(string));

        /// <summary>
        /// Invoked when a drag operation has commenced. Need to create a DragObject.
        /// </summary>
        public event EventHandler<DragStartArgs> DragStarted;

        private const string modelMime = "application/x-model-component";
        private GCHandle dragSourceHandle;
        private bool isModels = false;
        private Menu popup = new Menu();
        private AccelGroup accel = new AccelGroup();

        /// <summary>Constructor</summary>
        public ListBoxView(ViewBase owner) : base(owner)
        {
            Listview = new IkonView(listmodel);
            mainWidget = Listview;
#if NETCOREAPP
            // It appears that the gtkiconview has changed considerably
            // between gtk2 and gtk3. In the gtk3 world, use of the 
            // set_text_column API is not recommended and in fact it appears
            // to behave differently to the way it did in gtk2 anyway.
            // https://bugzilla.gnome.org/show_bug.cgi?id=680953
            CellRendererPixbuf imageCell = new CellRendererPixbuf();
            Listview.PackStart(imageCell, false);
            Listview.AddAttribute(imageCell, "pixbuf", 1);
            CellRenderer cell = new CellRendererText(){ WrapMode = Pango.WrapMode.Word };
            Listview.PackStart(cell, true);
            Listview.AddAttribute(cell, "markup", 0);
#else
            Listview.MarkupColumn = 0;
            Listview.PixbufColumn = 1;
#endif
            Listview.TooltipColumn = 2;
            Listview.SelectionMode = SelectionMode.Browse;
#if NETFRAMEWORK
            Listview.Orientation = Gtk.Orientation.Horizontal;
#else
            Listview.ItemOrientation = Gtk.Orientation.Horizontal;
#endif
            Listview.RowSpacing = 0;
            Listview.ColumnSpacing = 0;
            Listview.ItemPadding = 0;

            Listview.SelectionChanged += OnSelectionChanged;
            Listview.ButtonPressEvent += OnDoubleClick;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                //listview.CursorChanged -= OnSelectionChanged;
                Listview.SelectionChanged -= OnSelectionChanged;
                Listview.ButtonPressEvent -= OnDoubleClick;
                ClearPopup();
                popup.Cleanup();
                listmodel.Dispose();
                accel.Dispose();
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Get or sets the list of valid values.</summary>
        public string[] Values
        {
            get
            {
                List<string> items = new List<string>();
                foreach (object[] row in listmodel)
                    items.Add((string)row[0]);
                return items.ToArray();
            }
            set
            {
                listmodel.Clear();
                foreach (string val in value)
                {
                    // simplify text from FullName (now passed) to Name - lie112 to allow Resource TreeViewImages folder structure based on Model namespace
                    string text = val;
                    string addedModelDetails = "";
                    string[] nameParts = val.Split('.');
                    if (val.StartsWith("Models."))
                    {
                        text = nameParts[nameParts.Length - 1];
                        addedModelDetails = nameParts[1] + ".";
                    }
                    Gdk.Pixbuf image = null;
                    int posLastSlash = text.LastIndexOfAny("\\/".ToCharArray());
                    if (posLastSlash != -1)
                    {
                        text = AddFileNameListItem(StringUtilities.PangoString(val), ref image);
                    }
                    else if (isModels)
                    {
                        // lie112 Add model name component of namespace to allow for treeview images to be placed in folders in resources
                        string resourceNameForImage = "ApsimNG.Resources.TreeViewImages." + addedModelDetails + text + ".png";
                        if (!MasterView.HasResource(resourceNameForImage))
                        {
                            resourceNameForImage = "ApsimNG.Resources.TreeViewImages." + text + ".png";
                        }
                        if (MasterView.HasResource(resourceNameForImage))
                            image = new Gdk.Pixbuf(null, resourceNameForImage);
                        else
                            image = new Gdk.Pixbuf(null, "ApsimNG.Resources.TreeViewImages.Simulations.png"); // It there something else we could use as a default?
                    }
                    string tooltip = isModels ? val : StringUtilities.PangoString(val);
                    listmodel.AppendValues(text, image, tooltip);
                }
            }
        }

        /// <summary>
        /// Add a list item based on a file name
        /// </summary>
        /// <param name="fileName">The filename.</param>
        /// <param name="image">The image.</param>
        private string AddFileNameListItem(string fileName, ref Gdk.Pixbuf image)
        {
            List<string> resourceNames = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames().ToList();
            List<string> largeImageNames = resourceNames.FindAll(r => r.Contains(".LargeImages."));
            string result = $"<span>{Path.GetFileName(fileName)}</span>\n<small><i><span>{Path.GetDirectoryName(fileName)}</span></i></small>";
            Listview.ItemPadding = 6; // Restore padding if we have images to display

            image = null;
            // Add an image index.
            foreach (string largeImageName in largeImageNames)
            {
                string shortImageName = StringUtilities.GetAfter(largeImageName, ".LargeImages.").Replace(".png", "").ToLower();
                if (result.ToLower().Contains(shortImageName))
                {
                    image = new Gdk.Pixbuf(null, largeImageName);
                    break;
                }
            }
            if (image == null)
                image = new Gdk.Pixbuf(null, "ApsimNG.Resources.apsim logo32.png");
            return result;
        }

        /// <summary>Gets or sets the selected value.</summary>
        public string SelectedValue
        {
            get
            {
                TreePath[] selPath = Listview.SelectedItems;
                //TreePath selPath;
                //TreeViewColumn selCol;
                //listview.GetCursor(out selPath, out selCol);
                if (selPath == null || selPath.Length == 0)
                    return null;
                else
                {
                    TreeIter iter;
                    listmodel.GetIter(out iter, selPath[0]);
                    string result;
                    if (listmodel.GetValue(iter, 1) != null)
                        result = (string)listmodel.GetValue(iter, 2);
                    else
                        result = (string)listmodel.GetValue(iter, 0);
                    return isModels ? result : result.Replace("&amp;", "&");
                }
            }
            set
            {
                TreePath[] selPath = Listview.SelectedItems;
                //TreePath selPath;
                //TreeViewColumn selCol;
                //listview.GetCursor(out selPath, out selCol);
                if (selPath != null)
                {
                    TreeIter iter;
                    listmodel.GetIter(out iter, selPath[0]);
                    listmodel.SetValue(iter, 0, value);
                    listmodel.SetValue(iter, 2, value);
                }
            }
        }

        /// <summary>Return true if the listview is visible.</summary>
        public bool IsVisible
        {
            get { return Listview.Visible; }
            set { Listview.Visible = value; }
        }


        /// <summary>
        /// If true, try to show images; otherwise text only
        /// </summary>
        public bool IsModelList
        {
            get { return isModels; }
            set
            {
                bool wasModels = isModels;
                isModels = value;
                if (value)
                {
                    TargetEntry[] target_table = new TargetEntry[] { new TargetEntry(modelMime, TargetFlags.App, 0) };

                    Drag.SourceSet(Listview, Gdk.ModifierType.Button1Mask, target_table, Gdk.DragAction.Copy);
                    Listview.DragBegin += OnDragBegin;
                    Listview.DragDataGet += OnDragDataGet;
                    Listview.DragEnd += OnDragEnd;
                }
                else if (wasModels)
                {
                    Drag.SourceUnset(Listview);
                    Listview.DragBegin -= OnDragBegin;
                    Listview.DragDataGet -= OnDragDataGet;
                    Listview.DragEnd -= OnDragEnd;
                }
            }
        }

        /// <summary>User has changed the selection.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (Changed != null)
                    Changed.Invoke(this, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>User has double clicked the list box.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [GLib.ConnectBefore] // Otherwise this is handled internally, and we won't see it
        private void OnDoubleClick(object sender, ButtonPressEventArgs e)
        {
            try
            {
                if (e.Event.Type == Gdk.EventType.TwoButtonPress && e.Event.Button == 1 && DoubleClicked != null)
                    DoubleClicked.Invoke(sender, e);
                if (e.Event.Button == 3)
                {
                    TreePath path = Listview.GetPathAtPos((int)e.Event.X, (int)e.Event.Y);
                    if (path != null)
                    {
                        Listview.SelectPath(path);
                        if (popup.Children.Count() > 0)
                            popup.Popup();
                    }
                    e.RetVal = true;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Node has begun to be dragged.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragBegin(object sender, DragBeginArgs e)
        {
            try
            {
                DragStartArgs args = new DragStartArgs();
                args.NodePath = SelectedValue;
                if (DragStarted != null)
                {
                    DragStarted(this, args);
                    if (args.DragObject != null)
                    {
                        dragSourceHandle = GCHandle.Alloc(args.DragObject);
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Get data to be sent to presenter.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragDataGet(object sender, DragDataGetArgs e)
        {
            try
            {
                IntPtr data = (IntPtr)dragSourceHandle;
                Int64 ptrInt = data.ToInt64();
                Gdk.Atom target = Drag.DestFindTarget(sender as Widget, e.Context, null);
                e.SelectionData.Set(target, 8, BitConverter.GetBytes(ptrInt));
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void OnDragEnd(object sender, DragEndArgs e)
        {
            try
            {
                if (dragSourceHandle.IsAllocated)
                {
                    dragSourceHandle.Free();
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Place text on the clipboard
        /// </summary>
        /// <param name="text"></param>
        public void SetClipboardText(string text)
        {
            Gdk.Atom modelClipboard = Gdk.Atom.Intern("_APSIM_MODEL", false);
            Clipboard cb = Clipboard.Get(modelClipboard);
            cb.Text = text;
        }

        /// <summary>Populate the context menu from the descriptions passed in.</summary>
        /// <param name="menuDescriptions">Menu descriptions for each menu item.</param>
        public void PopulateContextMenu(List<MenuDescriptionArgs> menuDescriptions)
        {
            ClearPopup();
            foreach (MenuDescriptionArgs description in menuDescriptions)
            {
                MenuItem item;
                if (description.ShowCheckbox)
                {
                    CheckMenuItem checkItem = new CheckMenuItem(description.Name);
                    checkItem.Active = description.Checked;
                    item = checkItem;
                }
                else if (!String.IsNullOrEmpty(description.ResourceNameForImage) && MasterView.HasResource(description.ResourceNameForImage))
                {
                    item = WidgetExtensions.CreateImageMenuItem(description.Name, new Image(null, description.ResourceNameForImage));
                }
                else
                {
                    item = new MenuItem(description.Name);
                }
                if (!String.IsNullOrEmpty(description.ShortcutKey))
                {
                    string keyName = String.Empty;
                    Gdk.ModifierType modifier = Gdk.ModifierType.None;
                    string[] keyNames = description.ShortcutKey.Split(new Char[] { '+' });
                    foreach (string name in keyNames)
                    {
                        if (name == "Ctrl")
                            modifier |= Gdk.ModifierType.ControlMask;
                        else if (name == "Shift")
                            modifier |= Gdk.ModifierType.ShiftMask;
                        else if (name == "Alt")
                            modifier |= Gdk.ModifierType.Mod1Mask;
                        else if (name == "Del")
                            keyName = "Delete";
                        else
                            keyName = name;
                    }
                    try
                    {
                        Gdk.Key accelKey = (Gdk.Key)Enum.Parse(typeof(Gdk.Key), keyName, false);
                        item.AddAccelerator("activate", accel, (uint)accelKey, modifier, AccelFlags.Visible);
                    }
                    catch
                    {
                    }
                }
                item.Activated += description.OnClick;
                popup.Append(item);

            }
            if (popup.AttachWidget == null)
                popup.AttachToWidget(Listview, null);
            popup.ShowAll();
        }

        private void ClearPopup()
        {
            foreach (Widget w in popup)
            {
                if (w is MenuItem)
                {
                    PropertyInfo pi = w.GetType().GetProperty("AfterSignals", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (pi != null)
                    {
                        System.Collections.Hashtable handlers = (System.Collections.Hashtable)pi.GetValue(w);
                        if (handlers != null && handlers.ContainsKey("activate"))
                        {
                            EventHandler handler = (EventHandler)handlers["activate"];
                            (w as MenuItem).Activated -= handler;
                        }
                    }
                }
                popup.Remove(w);
                w.Cleanup();
            }
        }
    }
}
