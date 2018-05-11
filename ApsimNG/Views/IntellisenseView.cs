using System;
using System.Collections.Generic;
using Gtk;
using UserInterface.EventArguments;
using System.Windows.Media;
using System.Reflection;
using System.IO;
using UserInterface.Intellisense;

namespace UserInterface.Views
{
    class IntellisenseView
    {
        /// <summary>
        /// Main/Parent window for the intellisense popup.
        /// </summary>
        public Window MainWindow { get; set; }

        /// <summary>
        /// Invoked when the user selects an item (via enter or double click).
        /// </summary>
        public event EventHandler<IntellisenseItemSelectedArgs> ItemSelected
        {
            add
            {
                if (onItemSelected == null)
                    onItemSelected += value;
            }
            remove
            {
                onItemSelected -= value;
            }
        }

        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.')
        /// </summary>
        public event EventHandler<NeedContextItemsArgs> ContextItemsNeeded
        {
            add
            {
                if (onContextItemsNeeded == null)
                    onContextItemsNeeded += value;
            }
            remove
            {
                onContextItemsNeeded -= value;
            }
        }

        /// <summary>
        /// Fired when the intellisense window loses focus.
        /// </summary>
        public event EventHandler LoseFocus
        {
            add
            {
                if (onLoseFocus == null)
                {
                    onLoseFocus += value;
                }
            }
            remove
            {
                onLoseFocus -= value;
            }
        }

        /// <summary>
        /// Invoked when the user selects an item (via enter or double click).
        /// </summary>
        private event EventHandler<IntellisenseItemSelectedArgs> onItemSelected;

        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.')
        /// </summary>
        private event EventHandler<NeedContextItemsArgs> onContextItemsNeeded;

        /// <summary>
        /// Invoked when the intellisense popup loses focus.
        /// </summary>
        private event EventHandler onLoseFocus;

        /// <summary>
        /// The popup window.
        /// </summary>
        private Window completionForm;

        /// <summary>
        /// The TreeView which displays the data.
        /// </summary>
        private TreeView completionView;

        /// <summary>
        /// The ListStore which holds the data (suggested completion options).
        /// </summary>
        private ListStore completionModel;

        /// <summary>
        /// Default constructor. Initialises intellisense popup, but doesn't display anything.
        /// </summary>
        public IntellisenseView()
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

            completionModel = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
            completionView = new TreeView(completionModel);
            completionScroller.Add(completionView);

            TreeViewColumn column = new TreeViewColumn()
            {
                Title = "Item",
                Resizable = true
            };
            CellRendererPixbuf iconRender = new CellRendererPixbuf();
            column.PackStart(iconRender, false);
            CellRendererText textRender = new CellRendererText()
            {
                Editable = false
            };

            column.PackStart(textRender, true);
            column.SetAttributes(iconRender, "pixbuf", 0);
            column.SetAttributes(textRender, "text", 1);
            completionView.AppendColumn(column);

            textRender = new CellRendererText();
            column = new TreeViewColumn("Units", textRender, "text", 2)
            {
                Resizable = true
            };
            completionView.AppendColumn(column);

            textRender = new CellRendererText();
            column = new TreeViewColumn("Type", textRender, "text", 3)
            {
                Resizable = true
            };
            completionView.AppendColumn(column);

            textRender = new CellRendererText();
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
        /// Displays the intellisense popup at the specified coordinates. Returns true if the 
        /// popup is successfully generated (e.g. if it finds some auto-completion options). 
        /// Returns false otherwise.        
        /// </summary>
        /// <param name="x">Horizontal coordinate</param>
        /// <param name="y">Vertical coordinate</param>        
        private bool showAtCoordinates(int x, int y)
        {            
            // only display the list if there are options to display
            if (completionModel.IterNChildren() > 0)
            {                
                completionForm.ShowAll();
                completionForm.TransientFor = MainWindow;
                completionForm.Move(x, y);
                completionForm.Resize(completionForm.WidthRequest, completionForm.HeightRequest);
                completionView.SetCursor(new TreePath("0"), null, false);
                if (completionForm.GdkWindow != null)
                    completionForm.GdkWindow.Focus(0);
                while (GLib.MainContext.Iteration()) ;
                return true;
            }
            return false;
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

            return showAtCoordinates(Math.Max(0, x), Math.Max(0, y));
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
            onContextItemsNeeded(this, new NeedContextItemsArgs() { ObjectName = node, Items = items, AllItems = allItems });

            if (allItems.Count < 1)
                return false;

            Populate(allItems);
            return true;
        }

        public void Populate(List<ICSharpCode.NRefactory.Completion.ICompletionData> items)
        {
            completionModel.Clear();
            string workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDirectory))
                Directory.CreateDirectory(workingDirectory);
            else
            {
                // chances of this ever being run are astronomically low
                Directory.Delete(workingDirectory, true);
                Directory.CreateDirectory(workingDirectory);
            }
            
            foreach (CompletionData item in items)
            {
                string imageName = item.Image.ToString();
                UriBuilder builder = new UriBuilder(imageName);
                imageName = imageName.Replace(";component", "");
                imageName = imageName.Replace("pack://application:,,,/", "");
                string uri = imageName.Replace('/', '.');
                imageName = Path.GetFileName(imageName);
                
                string imageLocation = Path.Combine(workingDirectory, imageName);
                if (!File.Exists(imageLocation))
                {
                    imageLocation = GetImagePath(imageName, workingDirectory, uri);
                }

                string resourceName = imageName.Replace('/', '.');

                Gdk.Pixbuf image = new Gdk.Pixbuf(imageLocation);
                completionModel.AppendValues(image, item.CompletionText, "(kg)", "type name", item.Description, "item.paramstring");
            }
        }

        /// <summary>
        /// Copies an image stored as an embdedded resource to a given directory and returns the path to the image.
        /// </summary>
        /// <param name="imageName">Name of the image (without the path).</param>
        /// <param name="imageDirectory">Directory which the image should be copied to.</param>
        /// <returns>Full path to the image.</returns>
        private static string GetImagePath(string imageName, string imageDirectory, string uri)
        {
            string path = Path.Combine(imageDirectory, imageName);
            using (FileStream file = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                Stream imageStream = Assembly.GetAssembly(typeof(ICSharpCode.AvalonEdit.AvalonEditCommands)).GetManifestResourceStream(uri);
                if (imageStream == null)
                    imageStream = Assembly.GetAssembly(typeof(ICSharpCode.AvalonEdit.AvalonEditCommands)).GetManifestResourceStream("ICSharpCode.AvalonEdit.CodeCompletion.Images.Struct.png");
                if (imageStream == null)
                    return null;
                imageStream.CopyTo(file);
            }

            return path;
        }

        public void Populate(List<NeedContextItemsArgs.ContextItem> items)
        {
            completionModel.Clear();

            Gdk.Pixbuf functionPixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.Function.png", 16, 16);
            Gdk.Pixbuf propertyPixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.Property.png", 16, 16);
            Gdk.Pixbuf pixbufToBeUsed;

            foreach (NeedContextItemsArgs.ContextItem item in items)
            {
                pixbufToBeUsed = item.IsProperty ? propertyPixbuf : functionPixbuf;
                completionModel.AppendValues(pixbufToBeUsed, item.Name, item.Units, item.TypeName, item.Descr, item.ParamString);
            }
        }

        /// <summary>
        /// Gets the currently selected item.
        /// </summary>
        /// <exception cref="Exception">Exception is thrown if no item is selected.</exception>
        /// <returns></returns>
        private string GetSelectedItem()
        {
            TreeViewColumn col;
            TreePath path;
            completionView.GetCursor(out path, out col);
            if (path != null)
            {
                TreeIter iter;
                completionModel.GetIter(out iter, path);
                return (string)completionModel.GetValue(iter, 1);
            }
            throw new Exception("Unable to get selected intellisense item: no item is selected.");
        }

        /// <summary>
        /// Focus out event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [GLib.ConnectBefore]
        private void OnLeaveCompletion(object sender, FocusOutEventArgs e)
        {
            completionForm.Hide();
            onLoseFocus?.Invoke(this, new EventArgs());
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
            if (e.Event.Type == Gdk.EventType.TwoButtonPress && e.Event.Button == 1)
            {
                completionForm.Hide();
                onItemSelected(this, new IntellisenseItemSelectedArgs { ItemSelected = GetSelectedItem() });                
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
            // If user clicks ENTER and the context list is visible then insert the currently
            // selected item from the list into the TextBox and close the list.
            if (e.Event.Key == Gdk.Key.Return && completionForm.Visible)
            {
                completionForm.Hide();
                onItemSelected(this, new IntellisenseItemSelectedArgs { ItemSelected = GetSelectedItem() });
            }

            // If the user presses ESC and the context list is visible then close the list.
            else if (e.Event.Key == Gdk.Key.Escape && completionView.Visible)
            {
                completionForm.Hide();
            }
        }

        /// <summary>
        /// Key release event handler. If the key is enter, consumes the ItemSelected event.
        /// </summary>
        /// <param name="o">Sender</param>
        /// <param name="args">Event arguments</param>
        [GLib.ConnectBefore]
        private void OnKeyRelease(object o, KeyReleaseEventArgs e)
        {            
            if (e.Event.Key == Gdk.Key.Return && completionForm.Visible)
            {
                completionForm.Hide();
                onItemSelected(this, new IntellisenseItemSelectedArgs { ItemSelected = GetSelectedItem() });
                while (GLib.MainContext.Iteration()) ;
            }                
        }

        /// <summary>
        /// Safely disposes of several objects.
        /// </summary>
        public void Cleanup()
        {
            if (completionForm.IsRealized)
                completionForm.Destroy();
            completionView.Dispose();
            completionForm.Destroy();
            completionForm = null;
        }
    }
}
