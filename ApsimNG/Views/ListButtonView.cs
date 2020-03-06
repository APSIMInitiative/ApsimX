namespace UserInterface.Views
{
    using System;
    using System.Linq;
    using Classes;
    using Gtk;

    /// <summary>An interface for a list with a button bar</summary>
    public interface IListButtonView
    {
        /// <summary>The list.</summary>
        IListBoxView List { get; }

        /// <summary>
        /// Filter to be applied to displayed items.
        /// </summary>
        string Filter { get; }

        /// <summary>Add a button to the button bar</summary>
        /// <param name="text">Text for button</param>
        /// <param name="image">Image for button</param>
        /// <param name="handler">Handler to call when user clicks on button</param>
        void AddButton(string text, Image image, EventHandler handler);
        
        /// <summary>
        /// Adds a button with a submenu.
        /// </summary>
        /// <param name="text">Text for button.</param>
        /// <param name="image">Image for button.</param>
        void AddButtonWithMenu(string text, Image image);

        /// <summary>
        /// Adds a button to a sub-menu.
        /// </summary>
        /// <param name="menuId">Text on the menu button.</param>
        /// <param name="text">Text on the button.</param>
        /// <param name="image">Image on the button.</param>
        /// <param name="handler">Handler to call when button is clicked.</param>
        void AddButtonToMenu(string menuId, string text, Image image, EventHandler handler);

        /// <summary>
        /// Invoked when the filter is changed.
        /// </summary>
        event EventHandler FilterChanged;
    }

    /// <summary>A view for a list with a button bar</summary>
    public class ListButtonView : ViewBase, IListButtonView
    {
        private bool buttonsAreToolbar;
        private VBox vbox;
        private ListBoxView listboxView;
        private ScrolledWindow scrolledwindow1;
        private HBox buttonPanel;
        private HBox filterPanel;
        private Entry filterEntry;
        private Toolbar btnToolbar = null;

        /// <summary>Constructor</summary>
        public ListButtonView(ViewBase owner) : base(owner)
        {
            vbox = new VBox(false, 0);
            mainWidget = vbox;
            buttonPanel = new HBox();
            // buttonPanel.Layout = ButtonBoxStyle.Start;
            filterPanel = new HBox();
            Label filterLabel = new Label("Search: ");
            filterEntry = new Entry();
            filterPanel.PackStart(filterLabel, false, false, 0);
            filterPanel.PackStart(filterEntry, false, true, 0);
            filterEntry.Changed += OnFilterChanged;
            listboxView = new ListBoxView(this);
            scrolledwindow1 = new ScrolledWindow();
            scrolledwindow1.Add(listboxView.MainWidget);
            vbox.PackStart(filterPanel, false, true, 0);
            vbox.PackStart(buttonPanel, false, true, 0);
            vbox.PackStart(scrolledwindow1, true, true, 0);
            mainWidget.ShowAll();
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                filterEntry.Changed -= OnFilterChanged;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>The list.</summary>
        public IListBoxView List { get { return listboxView; } }

        /// <summary>
        /// Filter to be applied to displayed items.
        /// </summary>
        public string Filter
        {
            get
            {
                return filterEntry.Text;
            }
            private set
            {
                filterEntry.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets whether the buttons are a toolbar.
        /// </summary>
        /// <remarks>
        /// This controls the appearance of the view. If true, the buttons will
        /// look like those at the top of the main view. If false, the buttons
        /// will look like the "Add Model" options in the right-hand panel.
        /// 
        /// The filter will only be shown if this is false.
        /// </remarks>
        public bool ButtonsAreToolbar
        {
            get
            {
                return buttonsAreToolbar;
            }
            set
            {
                buttonsAreToolbar = value;

                // If buttonsAreToolbar is true, we don't want to show the filter.
                if (buttonsAreToolbar && vbox.Children.Contains(filterPanel))
                    vbox.Remove(filterPanel);
                else if (!buttonsAreToolbar && !vbox.Children.Contains(filterPanel))
                {
                    // If buttonsAreToolbar is not true, and the filter is not
                    // visible, display the filter.
                    vbox.PackStart(filterPanel, false, true, 0);
                    vbox.ReorderChild(filterPanel, 0);
                }
            }
        }

        public ListBoxView ListView
        {
            get
            {
                return listboxView;
            }
        }

        /// <summary>
        /// Invoked when the filter is changed.
        /// </summary>
        public event EventHandler FilterChanged;

        /// <summary>Creates a button according to given parameters.</summary>
        /// <param name="text">Text for button</param>
        /// <param name="image">Image for button</param>
        /// <param name="handler">Handler to call when user clicks on button</param>
        /// <param name="withDropDown">Should the button have a drop-down arrow for a sub-menu?</param>
        private ToolButton CreateButton(string text, Image image, EventHandler handler, bool withDropDown)
        {
            ToolButton button = withDropDown ? new CustomMenuToolButton(image, null) : new ToolButton(image, null);
            button.Homogeneous = false;
            Label btnLabel = new Label(text);

            // Unsure why, but sometimes the label's font is incorrect
            // (inconsistent with default font).
            Pango.FontDescription font = Utility.Configuration.Settings.Font;
            if (font != null && font != btnLabel.Style.FontDescription)
                btnLabel.ModifyFont(Utility.Configuration.Settings.Font);
            btnLabel.LineWrap = true;
            btnLabel.LineWrapMode = Pango.WrapMode.Word;
            btnLabel.Justify = Justification.Center;
            btnLabel.Realized += BtnLabel_Realized;
            button.LabelWidget = btnLabel;
            if (handler != null)
                button.Clicked += handler;
            return button;
        }

        private void AddButton(ToolButton button)
        {
            if (ButtonsAreToolbar)
            {
                if (btnToolbar == null)
                {
                    btnToolbar = new Toolbar();
                    btnToolbar.ToolbarStyle = ToolbarStyle.Both;
                    buttonPanel.PackStart(btnToolbar, true, true, 0);
                }
                btnToolbar.Add(button);
            }
        }

        /// <summary>Add a button to the button bar</summary>
        /// <param name="text">Text for button</param>
        /// <param name="image">Image for button</param>
        /// <param name="handler">Handler to call when user clicks on button</param>
        public void AddButton(string text, Image image, EventHandler handler)
        {
            if (ButtonsAreToolbar)
            {
                if (btnToolbar == null)
                {
                    btnToolbar = new Toolbar();
                    btnToolbar.ToolbarStyle = ToolbarStyle.Both;
                    buttonPanel.PackStart(btnToolbar, true, true, 0);
                }
                AddButton(CreateButton(text, image, handler, false));
            }
            else
            {
                Button button = new Button(text);
                button.Image = image;
                button.Clicked += handler;
                button.BorderWidth = 5;
                if (image != null)
                {
                    button.ImagePosition = PositionType.Top;
                    image.Show();
                }

                buttonPanel.PackStart(button, false, false, 0);
            }
            buttonPanel.ShowAll();

        }

        /// <summary>
        /// Adds a button with a submenu.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="menuId"></param>
        /// <param name="image"></param>
        /// <param name="handler"></param>
        public void AddButtonWithMenu(string text, Image image)
        {
            if (!ButtonsAreToolbar)
                throw new NotImplementedException();

            AddButton(CreateButton(text, image, null, true));
        }

        /// <summary>
        /// Adds a menu item button to a menu button.
        /// </summary>
        /// <param name="menuId">ID of the sub-menu.</param>
        /// <param name="text">Text on the button.</param>
        /// <param name="image">Image on the button.</param>
        /// <param name="handler">Handler to call when button is clicked.</param>
        public void AddButtonToMenu(string parentButtonText, string text, Image image, EventHandler handler)
        {
            if (!ButtonsAreToolbar)
                throw new NotImplementedException();

            // Find the top-level menu button (the button which, when clicked, causes the menu to appear).
            MenuToolButton toplevel = btnToolbar.AllChildren.OfType<MenuToolButton>().FirstOrDefault(b => (b.LabelWidget as Label).Text == parentButtonText);
            if (toplevel.Menu as Menu == null)
                toplevel.Menu = new Menu();
            Menu menu = toplevel.Menu as Menu;

            ImageMenuItem menuItem = new ImageMenuItem(text);
            menuItem.Image = image;
            menuItem.Activated += handler;
            menu.Append(menuItem);
            menuItem.ShowAll();
        }

        /// <summary>
        /// Gtk seems to have some trouble getting a wrapped label centered within
        /// the space allocated to it, at least in this context. This is a hack to
        /// get around the problem.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnLabel_Realized(object sender, EventArgs e)
        {
            try
            {
                ((sender as Label).Parent as VBox).Spacing = 0;
                Pango.Layout layout = (sender as Label).Layout;
                Pango.Rectangle ink;
                Pango.Rectangle logical;
                layout.GetExtents(out ink, out logical);
                (sender as Label).Xpad = ((layout.Width - logical.Width) / (int)Pango.Scale.PangoScale) / 2;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the filter is changed.
        /// </summary>
        /// <param name="sender">Event arguments.</param>
        /// <param name="e">Sender object.</param>
        private void OnFilterChanged(object sender, EventArgs e)
        {
            try
            {
                FilterChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
