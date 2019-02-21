// -----------------------------------------------------------------------
// <copyright file="ListButtonView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Linq;
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
            _mainWidget = vbox;
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
            _mainWidget.ShowAll();
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            _mainWidget.Destroyed -= _mainWidget_Destroyed;
            filterEntry.Changed -= OnFilterChanged;
            _owner = null;
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
                ToolButton button = new ToolButton(image, null);
                button.Homogeneous = false;
                Label btnLabel = new Label(text);
                btnLabel.LineWrap = true;
                btnLabel.LineWrapMode = Pango.WrapMode.Word;
                btnLabel.Justify = Justification.Center;
                btnLabel.Realized += BtnLabel_Realized;
                button.LabelWidget = btnLabel;
                if (handler != null)
                    button.Clicked += handler;
                btnToolbar.Add(button);
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
        /// Gtk seems to have some trouble getting a wrapped label centered within
        /// the space allocated to it, at least in this context. This is a hack to
        /// get around the problem.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnLabel_Realized(object sender, EventArgs e)
        {
            ((sender as Label).Parent as VBox).Spacing = 0;
            Pango.Layout layout = (sender as Label).Layout;
            Pango.Rectangle ink;
            Pango.Rectangle logical;
            layout.GetExtents(out ink, out logical);
            (sender as Label).Xpad = ((layout.Width - logical.Width) / (int)Pango.Scale.PangoScale) / 2;
        }

        /// <summary>
        /// Invoked when the filter is changed.
        /// </summary>
        /// <param name="sender">Event arguments.</param>
        /// <param name="e">Sender object.</param>
        private void OnFilterChanged(object sender, EventArgs e)
        {
            FilterChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
