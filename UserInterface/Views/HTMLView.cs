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
        string LabelText { get; set; }
    }

    /// <summary>
    /// The Presenter for a HTML component.
    /// This view uses the component developed here:
    /// http://www.modeltext.com/html/
    /// </summary>
    public partial class HTMLView : UserControl, IHTMLView
    {
        public event EventHandler<EditorArgs> MemoUpdate;
        private string defaultHtml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                                     "<!DOCTYPE html PUBLIC \" -//W3C//DTD XHTML 1.1//EN\" \"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd\">" +
                                     "<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\">" +
                                     "<head>" +
                                     "<title />" +
                                     "</head>" +
                                     "<body />" +
                                     "</html>";

        public HTMLView()
        {
            InitializeComponent();
            //create the control's toolbar
            tooledControl1.addTools();

            //install an event handler, to help process some of the buttons on the edit toolbar
            tooledControl1.modelEdit.toolContainer.onToolCommand = onToolCommand;
        }

        /// <summary>
        /// Set or get the text of the richedit
        /// </summary>
        public string MemoText
        {
            get 
            {
                StringWriter writer = new StringWriter();
                tooledControl1.modelEdit.save(writer, XmlHeaderType.Xhtml);
                return writer.ToString(); 
            }
            set 
            {
                if (value == null)
                {
                    value = defaultHtml;
                }

                StringReader reader = new StringReader(value);
                try
                {
                    tooledControl1.modelEdit.openDocument(reader);
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Get or set the readonly name of the richedit.
        /// </summary>
        public bool ReadOnly 
        {
            get { return !tooledControl1.editControl.Enabled; }
            set 
            { 
                //tooledControl1.editControl.Enabled = !value;
                tooledControl1.modelEdit.toolContainer.visible = tooledControl1.editControl.Enabled;
            }
        }

        /// <summary>
        /// Get or set the label text.
        /// </summary>
        public string LabelText 
        {
            get { return label1.Text; }
            set { label1.Text = value; }
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
            contextMenuStrip1.Items.Add(buttonText);
            contextMenuStrip1.Items[0].Click += onClick;
            tooledControl1.ContextMenuStrip = contextMenuStrip1;
        }

        /// <summary>
        /// This method is invoked when any command on the toolbar is pressed
        /// </summary>
        /// <param name="command"></param>
        private void onToolCommand(Command command)
        {
            //see what type of command it is
            //the command-handler for most of the commands are implemented within the control
            //but two of the commands (i.e. Save and InsertHyperlink) need help from the client application
            if (object.ReferenceEquals(command, CommandInstance.commandInsertHyperlink))
            {
                //invoke a method to handle the Insert Hyperlink command being pressed
                onInsertHyperlink();
            }
        }

        private void onInsertHyperlink()
        {
            //get the document fragment which the user has currently selected using mouse and/or cursor
            IWindowSelection windowSelection = tooledControl1.modelEdit.windowSelection;
            //verify that there's only one selection
            if (windowSelection.rangeCount != 1)
            {
                //this can happen when the user has selected several cell in a table,
                //in which case each cell is a separate selection/range
                MessageBox.Show("Can't insert a hyperlink when more than one range in the document is selected");
                return;
            }
            using (IDomRange domRange = windowSelection.getRangeAt(0))
            {
                //verify that only one node is selected
                if (!domRange.startContainer.isSameNode(domRange.endContainer))
                {
                    //this can happen for example when the selection spans multiple paragraphs
                    MessageBox.Show("Can't insert a hyperlink when more than one node in the document is selected");
                    return;
                }
                IDomNode container = domRange.startContainer; //already just checked that this is the same as domRange.endContainer
                //read existing values from the current selection
                string url;
                string visibleText;
                IDomElement existingHyperlink;
                switch (container.nodeType)
                {
                    case DomNodeType.Text:
                        //selection is a text fragment
                        visibleText = container.nodeValue.Substring(domRange.startOffset, domRange.endOffset - domRange.startOffset);
                        IDomNode parentNode = container.parentNode;
                        if ((parentNode.nodeType == DomNodeType.Element) && (parentNode.nodeName == "a"))
                        {
                            //parent of this text node is a <a> element
                            existingHyperlink = parentNode as IDomElement;
                            url = existingHyperlink.getAttribute("href");
                            visibleText = container.nodeValue;
                            if ((existingHyperlink.childNodes.count != 1) || (existingHyperlink.childNodes.itemAt(0).nodeType != DomNodeType.Text))
                            {
                                //this can happen when an anchor tag wraps more than just a single, simple text node
                                //for example when it contains inline elements like <strong>
                                MessageBox.Show("Can't edit a complex hyperlink");
                                return;
                            }
                        }
                        else
                        {
                            existingHyperlink = null;
                            url = null;
                        }
                        break;

                    default:
                        //unexpected
                        MessageBox.Show("Can't insert a hyperlink when more than one node in the document is selected");
                        return;
                }
                //display the modal dialog box
                using (FormInsertHyperlink formInsertHyperlink = new FormInsertHyperlink())
                {
                    formInsertHyperlink.url = url;
                    formInsertHyperlink.visibleText = visibleText;
                    DialogResult dialogResult = formInsertHyperlink.ShowDialog();
                    if (dialogResult != DialogResult.OK)
                    {
                        //user cancelled
                        return;
                    }
                    //get new values from the dialog box
                    //the FormInsertHyperlink.onEditTextChanged method assures that both strings are non-empty
                    url = formInsertHyperlink.url;
                    visibleText = formInsertHyperlink.visibleText;
                }
                //need to change href, change text, and possibly delete existing text;
                //do this within the scope of a single IEditorTransaction instance so
                //that if the user does 'undo' then it will undo all these operations at once, instead of one at a time
                using (IEditorTransaction editorTransaction = tooledControl1.modelEdit.createEditorTransaction())
                {
                    if (existingHyperlink != null)
                    {
                        //changing an existing hyperlink ...
                        //... change the href attribute value
                        existingHyperlink.setAttribute("href", url);
                        //... change the text, by removing the old text node and inserting a new text node
                        IDomText newDomText = tooledControl1.modelEdit.domDocument.createTextNode(visibleText);
                        IDomNode oldDomText = existingHyperlink.childNodes.itemAt(0);
                        existingHyperlink.removeChild(oldDomText);
                        existingHyperlink.insertBefore(newDomText, null);
                    }
                    else
                    {
                        //creating a new hyperlink
                        IDomElement newHyperlink = tooledControl1.modelEdit.domDocument.createElement("a");
                        IDomText newDomText = tooledControl1.modelEdit.domDocument.createTextNode(visibleText);
                        newHyperlink.insertBefore(newDomText, null);
                        newHyperlink.setAttribute("href", url);
                        //remove whatever was previously selected, if anything
                        if (!domRange.collapsed)
                        {
                            domRange.deleteContents();
                        }
                        //insert the new hyperlink
                        domRange.insertNode(newHyperlink);
                    }
                }
            }
        }

    }

}
