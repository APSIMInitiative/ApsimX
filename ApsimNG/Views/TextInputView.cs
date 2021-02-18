namespace UserInterface.Views
{
    using Gtk;
    using System;
    using FontDescription = Pango.FontDescription;

    public class TextInputView : ViewBase
    {
        private TextView editor;
        private ScrolledWindow scroller;

        /// <summary>
        /// Called when the text is changed.
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Default constructor provided for use with the automatic
        /// glade file infrastructure in ViewBase. Don't call this directly.
        /// </summary>
        public TextInputView() : base()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">Owner widget.</param>
        public TextInputView(ViewBase owner) : base(owner)
        {
            editor = new TextView();
            ConnectEvents();

            scroller = new ScrolledWindow();
            scroller.Add(editor);
            mainWidget = scroller;
        }

        /// <summary>
        /// Controls the text in the UI.
        /// </summary>
        public string Text
        {
            get => editor.Buffer.Text;
            set => editor.Buffer.Text = value;
        }

        /// <summary>
        /// Controls whether the text editor is visible.
        /// </summary>
        public bool Visible
        {
            get => mainWidget.Visible;
            set
            {
                if (value)
                    mainWidget.ShowAll();
                else
                    mainWidget.Hide();
            }
        }

        /// <summary>
        /// Controls whether line wrapping is enabled.
        /// </summary>
        public bool WrapText
        {
            get => editor.WrapMode != WrapMode.None;
            set
            {
                if (value)
                    editor.WrapMode = WrapMode.WordChar;
                else
                    editor.WrapMode = WrapMode.None;
            }
        }

        /// <summary>
        /// Change the font of the text in the editor.
        /// </summary>
        /// <param name="font">The font to be used.</param>
        public void ModifyFont(string font)
        {
            editor.ModifyFont(FontDescription.FromString(font));
        }

        /// <summary>
        /// Used by the automatic glade file infrastructure. Shouldn't be called
        /// directly.
        /// </summary>
        /// <param name="ownerView">Owner view.</param>
        /// <param name="gtkControl">A ScrolledWindow.</param>
        protected override void Initialise(ViewBase ownerView, GLib.Object gtkControl)
        {
            base.Initialise(ownerView, gtkControl);
            scroller = (ScrolledWindow)gtkControl;
            editor = new TextView();
            ConnectEvents();
            scroller.Add(editor);
            mainWidget = scroller;
        }

        private void ConnectEvents()
        {
            editor.Buffer.Changed += OnChanged;
            editor.Destroyed += OnDestroyed;
        }

        private void DisconnectEvents()
        {
            editor.Buffer.Changed -= OnChanged;
            editor.Destroyed -= OnDestroyed;
        }

        private void OnDestroyed(object sender, EventArgs e)
        {
            try
            {
                DisconnectEvents();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void OnChanged(object sender, EventArgs e)
        {
            try
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}