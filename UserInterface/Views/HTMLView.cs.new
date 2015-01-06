using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ModelText.ModelEditControl;
using ModelText.ModelDom.Range;
using ModelText.ModelDom.Nodes;
using ModelText.ModelEditToolCommands;
using UserInterface.Forms;
namespace UserInterface.Views
{
    interface IHTMLView
    {
        event EventHandler<EditorArgs> MemoUpdate;

        /// <summary>
        /// Add an action (on context menu) on the memo.
        /// </summary>
        void AddContextAction(string ButtonText, System.EventHandler OnClick);

        string MemoText { get; set; }
        bool ReadOnly { get; set; }
    }

    /// <summary>
    /// The Presenter for a HTML component.
    /// This view uses the component developed here:
    /// http://www.modeltext.com/html/
    /// </summary>
    public partial class HTMLView : UserControl, IHTMLView
    {
        public event EventHandler<EditorArgs> MemoUpdate;

        public HTMLView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set or get the text of the richedit
        /// </summary>
        public string MemoText
        {
            get 
            {
                return richTextBox1.Rtf;
            }
            set 
            {
                richTextBox1.Rtf = value;
                
            }
        }

        /// <summary>
        /// Get or set the readonly name of the richedit.
        /// </summary>
        public bool ReadOnly 
        {
            get { return true; }
            set { }
        }

        /// <summary>
        /// The memo has been updated and now send the changed text to the model.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void richTextBox1_Leave(object sender, EventArgs e)
        {
            if (MemoUpdate != null)
            {
                EditorArgs args = new EditorArgs();
                args.TextString = MemoText;
                MemoUpdate(this, args);
            }
        }

        /// <summary>
        /// Add an action (on context menu) on the memo.
        /// </summary>
        public void AddContextAction(string buttonText, System.EventHandler onClick)
        {
           /* contextMenuStrip1.Items.Add(buttonText);
            contextMenuStrip1.Items[0].Click += onClick;
            tooledControl1.ContextMenuStrip = contextMenuStrip1;*/
        }

    }

}
