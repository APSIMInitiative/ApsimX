namespace UserInterface.Views
{
    using System;
    using Gtk;
    using Interfaces;

    /// <summary>A button view.</summary>
    public class ButtonView : ViewBase, IButtonView
    {
        /// <summary>
        /// The button object
        /// </summary>
        private Button button;

        private ToolButton toolButton;

        /// <summary>Constructor.</summary>
        public ButtonView()
        {
        }

        /// <summary>The objects constructor</summary>
        /// <param name="owner">The owning view</param>
        public ButtonView(ViewBase owner) : base(owner)
        {
            button = new Button();
            mainWidget = button;
            if (button == null)
                toolButton.Clicked += OnButtonClick;
            else
            {
                button.Clicked += OnButtonClick;
                button.SetSizeRequest(80, 36);
            }
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        /// <summary>Invoked when the user clicks the button.</summary>
        public event EventHandler Clicked;

        /// <summary>Gets or sets the text of the button.</summary>
        public string Value
        {
            get { if (button == null) return toolButton.Label; else return button.Label; }
            set { if (button == null) toolButton.Label = value; else button.Label = value; }
        }

        /// <summary>Gets or sets a value indicating whether the dropdown is visible.</summary>
        public bool IsVisible
        {
            get { if (button == null) return toolButton.Visible; else return button.Visible; }
            set { if (button == null) toolButton.Visible = value; else button.Visible = value; }
        }

        /// <summary>
        /// Cleanup objects
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The argument parameters</param>
        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                if (button == null)
                    toolButton.Clicked -= OnButtonClick;
                else
                    button.Clicked -= OnButtonClick;
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>User has clicked the button.</summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The argument parameters</param>
        private void OnButtonClick(object sender, EventArgs e)
        {
            try
            {
                PerformClick();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Click the button.</summary>
        public void PerformClick()
        {
            if (Clicked != null)
                Clicked.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// A method used when a view is wrapping a gtk control.
        /// </summary>
        /// <param name="ownerView">The owning view.</param>
        /// <param name="gtkControl">The gtk control being wrapped.</param>
        protected override void Initialise(ViewBase ownerView, GLib.Object gtkControl)
        {
            owner = ownerView;
            if (gtkControl is Button)
            {
                button = (Button)gtkControl;
                button.Clicked += OnButtonClick;
                mainWidget = button;
            }
            else
            {
                toolButton = (ToolButton)gtkControl;
                toolButton.Clicked += OnButtonClick;
                mainWidget = toolButton;
            }
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

    }
}
