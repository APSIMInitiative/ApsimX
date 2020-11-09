namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using Gtk;
    using EventArguments;
    using Intellisense;
    using System.Linq;

    class IntellisenseView : ViewBase
    {
        /// <summary>
        /// The popup window.
        /// </summary>
        private Window completionForm;

        /// <summary>
        /// The TreeView which displays the data.
        /// </summary>
        private Gtk.TreeView completionView;

        /// <summary>
        /// The ListStore which holds the data (suggested completion options).
        /// </summary>
        private ListStore completionModel;

        /// <summary>
        /// Invoked when the user selects an item (via enter or double click).
        /// </summary>
        private event EventHandler<NeedContextItemsArgs.ContextItem> OnItemSelected;

        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.')
        /// </summary>
        private event EventHandler<NeedContextItemsArgs> OnContextItemsNeeded;

        /// <summary>
        /// Invoked when the intellisense popup loses focus.
        /// </summary>
        private event EventHandler OnLoseFocus;

        /// <summary>
        /// Default constructor. Initialises intellisense popup, but doesn't display anything.
        /// </summary>
        public IntellisenseView(ViewBase owner) : base(owner)
        {
            completionForm = new Window(WindowType.Toplevel)
            {
                HeightRequest = 300,
                WidthRequest = 750,
                Decorated = false,
                SkipPagerHint = true,
                SkipTaskbarHint = true,
            };

            Frame completionFrame = new Frame();
            completionForm.Add(completionFrame);

            ScrolledWindow completionScroller = new ScrolledWindow();
            completionFrame.Add(completionScroller);

            completionModel = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(bool));
            completionView = new Gtk.TreeView(completionModel);
            completionScroller.Add(completionView);

            TreeViewColumn column = new TreeViewColumn()
            {
                Title = "Item",
                Resizable = true,
            };
            CellRendererPixbuf iconRender = new CellRendererPixbuf();
            column.PackStart(iconRender, false);
            CellRendererText textRender = new CellRendererText()
            {
                Editable = false,
                WidthChars = 25,
                Ellipsize = Pango.EllipsizeMode.End
            };

            column.PackStart(textRender, true);
            column.SetAttributes(iconRender, "pixbuf", 0);
            column.SetAttributes(textRender, "text", 1);
            completionView.AppendColumn(column);

            textRender = new CellRendererText()
            {
                Editable = false,
                WidthChars = 10,
                Ellipsize = Pango.EllipsizeMode.End
            };
            column = new TreeViewColumn("Units", textRender, "text", 2)
            {
                Resizable = true
            };
            completionView.AppendColumn(column);

            textRender = new CellRendererText()
            {
                Editable = false,
                WidthChars = 15,
                Ellipsize = Pango.EllipsizeMode.End
            };
            column = new TreeViewColumn("Type", textRender, "text", 3)
            {
                Resizable = true
            };
            completionView.AppendColumn(column);

            textRender = new CellRendererText()
            {
                Editable = false,
            };
            column = new TreeViewColumn("Descr", textRender, "text", 4)
            {
                Resizable = true
            };
            completionView.AppendColumn(column);

            completionView.HasTooltip = true;
            completionView.TooltipColumn = 5;
            completionForm.FocusOutEvent += OnLeaveCompletion;
            completionView.ButtonPressEvent += OnButtonPress;
            completionView.KeyPressEvent += OnContextListKeyDown;
            completionView.KeyReleaseEvent += OnKeyRelease;
        }

        /// <summary>
        /// Invoked when the user selects an item (via enter or double click).
        /// </summary>
        public event EventHandler<NeedContextItemsArgs.ContextItem> ItemSelected
        {
            add
            {
                DetachHandlers(ref OnItemSelected);
                OnItemSelected += value;
            }
            remove
            {
                OnItemSelected -= value;
            }
        }

        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.')
        /// </summary>
        public event EventHandler<NeedContextItemsArgs> ContextItemsNeeded
        {
            add
            {
                if (OnContextItemsNeeded == null)
                    OnContextItemsNeeded += value;
            }
            remove
            {
                OnContextItemsNeeded -= value;
            }
        }

        /// <summary>
        /// Fired when the intellisense window loses focus.
        /// </summary>
        public event EventHandler LoseFocus
        {
            add
            {
                if (OnLoseFocus == null)
                {
                    OnLoseFocus += value;
                }
            }
            remove
            {
                OnLoseFocus -= value;
            }
        }

        /// <summary>
        /// Returns true if the intellisense is visible. False otherwise.
        /// </summary>
        public bool Visible { get { return completionForm.Visible; } }

        /// <summary>
        /// Editor being used. This is mainly needed to get a reference to the top level window.
        /// </summary>
        public ViewBase Editor { get; set; }

        /// <summary>
        /// Gets the Main/Parent window for the intellisense popup.
        /// </summary>
        public Window MainWindow
        {
            get
            {
                return Owner.MainWidget.Toplevel as Window;
            }
        }

        /// <summary>
        /// Gets the currently selected item.
        /// </summary>
        public NeedContextItemsArgs.ContextItem SelectedItem
        {
            get
            {
                TreeViewColumn col;
                TreePath path;
                completionView.GetCursor(out path, out col);
                if (path != null)
                {
                    TreeIter iter;
                    completionModel.GetIter(out iter, path);
                    NeedContextItemsArgs.ContextItem returnObject = new NeedContextItemsArgs.ContextItem()
                    {
                        Name = (string)completionModel.GetValue(iter, 1),
                        Units = (string)completionModel.GetValue(iter, 2),
                        TypeName = (string)completionModel.GetValue(iter, 3),
                        Descr = (string)completionModel.GetValue(iter, 4),
                        ParamString = (string)completionModel.GetValue(iter, 5),
                        IsMethod = (bool)completionModel.GetValue(iter, 6)
                    };
                    return returnObject;
                }
                throw new Exception("Unable to get selected intellisense item: no item is selected.");
            }
        }

        /// <summary>
        /// Displays the intellisense popup at the specified coordinates. Returns true if the 
        /// popup is successfully generated (e.g. if it finds some auto-completion options). 
        /// Returns false otherwise.        
        /// </summary>
        /// <param name="x">Horizontal coordinate</param>
        /// <param name="y">Vertical coordinate</param>        
        private bool ShowAtCoordinates(int x, int y)
        {            
            // only display the list if there are options to display
            if (completionModel.IterNChildren() > 0)
            {
                completionForm.TransientFor = MainWindow;
                completionForm.Move(x, y);
                completionForm.Resize(completionForm.WidthRequest, completionForm.HeightRequest);
                completionForm.ShowAll();
                completionView.SetCursor(new TreePath("0"), null, false);
                completionView.Columns[2].FixedWidth = completionView.WidthRequest / 10;

                // OK so sometimes the HTMLView's web browser will steal focus. There is a
                // hack in the HTMLView code which manually gives focus back to the toplevel
                // window in this situation, but apparently creating the intellisense popup
                // is enough to trigger this hack if there is a HTMLView onscreen. This is a
                // problem because giving the focus back to the main window will take focus
                // away from the intellisense popup which causes the intellisense popup to
                // disappear. The workaround is to wait for all Gtk events to process, and
                // then recreate the intellisense popup if need be.
                while (GLib.MainContext.Iteration()) ;
                if (!completionForm.Visible)
                {
                    // For some reason, the coordinates/sizing get reset if we lose focus.
                    completionForm.Move(x, y);
                    completionForm.Resize(completionForm.WidthRequest, completionForm.HeightRequest);
                    completionForm.ShowAll();
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Selects the n-th item in the list of completion options.
        /// </summary>
        /// <param name="index">0-based index of the item to select.</param>
        public void SelectItem(int index)
        {
            TreeIter iter;
            if (completionModel.GetIter(out iter, new TreePath(index.ToString())))
            {
                completionView.Selection.SelectIter(iter);
                completionView.SetCursor(new TreePath(index.ToString()), null, false);
            }
        }

        /// <summary>
        /// Tries to display the intellisense popup at the specified coordinates. If the coordinates are
        /// too close to the right or bottom of the screen, they will be adjusted appropriately.
        /// Returns true if the popup is successfully generated (e.g. if it finds some auto-completion options).
        /// Returns false otherwise.
        /// </summary>
        /// <param name="x">Horizontal coordinate</param>
        /// <param name="y">Vertical coordinate</param>
        /// <param name="lineHeight">Font height</param>
        /// <returns></returns>
        public bool SmartShowAtCoordinates(int x, int y, int lineHeight = 17)
        {
            // By default, we use the given coordinates as the top-left hand corner of the popup.
            // If the popup is too close to the right of the screen, we use the x-coordinate as 
            // the right hand side of the popup instead.
            // If the popup is too close to the bottom of the screen, we use the y-coordinate as
            // the bottom side of the popup instead.
            int xres = MainWindow.Screen.Width;
            int yres = MainWindow.Screen.Height;

            if ((x + completionForm.WidthRequest) > xres)            
                // We are very close to the right-hand side of the screen
                x -= completionForm.WidthRequest;            
            
            if ((y + completionForm.HeightRequest) > yres)
                // We are very close to the bottom of the screen
                // Move the popup one line higher as well, to room allow for the input box in the popup.
                y -= completionForm.HeightRequest + lineHeight;

            return ShowAtCoordinates(Math.Max(0, x), Math.Max(0, y));
        }

        /// <summary>
        /// Generates a list of auto-completion options.
        /// </summary>
        /// <returns></returns>
        public bool GenerateAutoCompletionOptions(string node)
        {
            // generate list of intellisense options
            List<string> items = new List<string>();
            List<NeedContextItemsArgs.ContextItem> allItems = new List<NeedContextItemsArgs.ContextItem>();
            OnContextItemsNeeded?.Invoke(this, new NeedContextItemsArgs() { ObjectName = node, Items = items, AllItems = allItems });

            if (allItems.Count < 1)
                return false;

            Populate(allItems);
            return true;
        }

        /// <summary>
        /// Populates the completion window with data.
        /// </summary>
        /// <param name="items">List of completion data.</param>
        public void Populate(List<CompletionData> items)
        {
            completionModel.Clear();

            // Add empty first row.
            completionModel.AppendValues("", "", "", "", "", "", "");
            foreach (CompletionData item in items)
            {
                IEnumerable<string> descriptionLines = item.Description?.Split(Environment.NewLine.ToCharArray()).Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Take(2);
                string description = descriptionLines?.Count() < 2 ? descriptionLines.FirstOrDefault() : descriptionLines?.Aggregate((x, y) => x + Environment.NewLine + y);
                completionModel.AppendValues(item.Image, item.DisplayText, item.Units, item.ReturnType, description, item.CompletionText, item.IsMethod);
            }
        }

        /// <summary>
        /// Populates the completion window with data.
        /// </summary>
        /// <param name="items">List of completion data.</param>
        public void Populate(List<NeedContextItemsArgs.ContextItem> items)
        {
            completionModel.Clear();

            // Add empty first row.
            completionModel.AppendValues("", "", "", "", "", "", "");

            Gdk.Pixbuf functionPixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.Function.png", 16, 16);
            Gdk.Pixbuf propertyPixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.Property.png", 16, 16);
            Gdk.Pixbuf pixbufToBeUsed;

            foreach (NeedContextItemsArgs.ContextItem item in items)
            {
                pixbufToBeUsed = item.IsProperty ? propertyPixbuf : functionPixbuf;
                completionModel.AppendValues(pixbufToBeUsed, item.Name, item.Units, item.TypeName, item.Descr, item.ParamString, item.IsMethod);
            }
        }

        /// <summary>
        /// Safely disposes of several objects.
        /// </summary>
        public void Cleanup()
        {
            completionForm.FocusOutEvent -= OnLeaveCompletion;
            completionView.ButtonPressEvent -= OnButtonPress;
            completionView.KeyPressEvent -= OnContextListKeyDown;
            completionView.KeyReleaseEvent -= OnKeyRelease;

            if (completionForm.IsRealized)
                completionForm.Destroy();
            completionView.Dispose();
            completionForm.Destroy();
            completionForm = null;
        }

        /// <summary>
        /// Detaches all event handlers from an event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        private static void DetachHandlers<T>(ref EventHandler<T> e)
        {
            if (e == null)
                return;
            foreach (EventHandler<T> handler in e?.GetInvocationList())
                e -= handler;
        }

        /// <summary>
        /// Focus out event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [GLib.ConnectBefore]
        private void OnLeaveCompletion(object sender, FocusOutEventArgs e)
        {
            try
            {
                completionForm.Hide();
                OnLoseFocus?.Invoke(this, new EventArgs());
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// (Mouse) button press event handler. If it is a left mouse double click, consumes 
        /// the ItemSelected event.
        /// </summary>
        /// <param name="o">Sender</param>
        /// <param name="e">Event arguments</param>
        [GLib.ConnectBefore]
        private void OnButtonPress(object sender, ButtonPressEventArgs e)
        {
            try
            {
                if (e.Event.Type == Gdk.EventType.TwoButtonPress && e.Event.Button == 1)
                    HandleItemSelected();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Key down event handler. If the key is enter, consumes the ItemSelected event.
        /// If the key is escape, hides the intellisense.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        [GLib.ConnectBefore]
        private void OnContextListKeyDown(object sender, KeyPressEventArgs e)
        {
            try
            {
                // If user clicks ENTER and the context list is visible then insert the currently
                // selected item from the list into the TextBox and close the list.
                if (e.Event.Key == Gdk.Key.Return && completionForm.Visible)
                {
                    HandleItemSelected();
                    e.RetVal = true;
                }
                // If the user presses ESC and the context list is visible then close the list.
                else if (e.Event.Key == Gdk.Key.Escape && completionView.Visible)
                {
                    completionForm.Hide();
                    e.RetVal = true;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Key release event handler. If the key is enter, consumes the ItemSelected event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [GLib.ConnectBefore]
        private void OnKeyRelease(object sender, KeyReleaseEventArgs e)
        {            
            try
            {
                if (e.Event.Key == Gdk.Key.Return && completionForm.Visible)
                {
                    HandleItemSelected();
                    e.RetVal = true;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Handles the item selected event, by invoking the appropriate event handler.
        /// </summary>
        private void HandleItemSelected()
        {
            completionForm.Hide();
            OnItemSelected?.Invoke(this, SelectedItem);
        }
    }
}
