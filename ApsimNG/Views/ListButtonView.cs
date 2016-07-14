// -----------------------------------------------------------------------
// <copyright file="ListButtonView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using Gtk;

    /// <summary>An interface for a list with a button bar</summary>
    public interface IListButtonView
    {
        /// <summary>The list.</summary>
        IListBoxView List { get; }

        /// <summary>Add a button to the button bar</summary>
        /// <param name="text">Text for button</param>
        /// <param name="image">Image for button</param>
        /// <param name="handler">Handler to call when user clicks on button</param>
        void AddButton(string text, Image image, EventHandler handler);
    }

    /// <summary>A view for a list with a button bar</summary>
    public class ListButtonView : ViewBase, IListButtonView
    {
        private VBox vbox;
        private ListBoxView listboxView;
        private ScrolledWindow scrolledwindow1;
        private HBox buttonPanel;

        /// <summary>Constructor</summary>
        public ListButtonView(ViewBase owner) : base(owner)
        {
            vbox = new VBox(false, 0);
            _mainWidget = vbox;
            buttonPanel = new HBox();
            // buttonPanel.Layout = ButtonBoxStyle.Start;
            listboxView = new ListBoxView(this);
            scrolledwindow1 = new ScrolledWindow();
            scrolledwindow1.Add(listboxView.MainWidget);
            vbox.PackStart(buttonPanel, false, true, 0);
            vbox.PackStart(scrolledwindow1, true, true, 0);
            _mainWidget.ShowAll();
        }

        /// <summary>The list.</summary>
        public IListBoxView List { get { return listboxView; } }

        public bool ButtonsAreToolbar { get; set; }

        private Toolbar btnToolbar = null;

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
                    buttonPanel.PackStart(btnToolbar, true, true, 0);
                }
                ToolButton button = new ToolButton(image, null);
                button.Homogeneous = false;
                Label btnLabel = new Label(text);
                Pango.FontDescription font = new Pango.FontDescription();
                font.Size = (int)(8 * Pango.Scale.PangoScale);
                btnLabel.ModifyFont(font);
                btnLabel.LineWrap = true;
                btnLabel.LineWrapMode = Pango.WrapMode.Word;
                btnLabel.Justify = Justification.Center;
                btnLabel.HeightRequest = 38;
                btnLabel.WidthRequest = 80;
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
    }
}
