using Gtk;
using System;
using System.Collections.Generic;
using EventArguments;

namespace UserInterface.Views
{
    /// <summary>
    /// A view for a summary file.
    /// </summary>
    public class SummaryView : ViewBase, ISummaryView
    {
        /// <summary>
        /// Gtk VBox which holds all other UI elements in the view.
        /// </summary>
        private VBox vbox1 = null;

        /// <summary>
        /// Drop down box which displays the simulation names.
        /// </summary>
        private ComboBox combobox1 = null;

        /// <summary>
        /// Model behind the dropdown box, which stores the simulation names.
        /// </summary>
        private ListStore comboModel = new ListStore(typeof(string));

        /// <summary>
        /// The cell which displays the data in the drop down box.
        /// </summary>
        private CellRendererText comboRender = new CellRendererText();

        /// <summary>
        /// View which displays the summary data.
        /// </summary>
        private HTMLView htmlview;

        /// <summary>Initializes a new instance of the <see cref="SummaryView"/> class.</summary>
        public SummaryView(ViewBase owner) : base(owner)
        {
            Builder builder = MasterView.BuilderFromResource("ApsimNG.Resources.Glade.SummaryView.glade");
            vbox1 = (VBox)builder.GetObject("vbox1");
            combobox1 = (ComboBox)builder.GetObject("combobox1");
            _mainWidget = vbox1;
            combobox1.PackStart(comboRender, false);
            combobox1.AddAttribute(comboRender, "text", 0);
            combobox1.Model = comboModel;
            combobox1.Changed += comboBox1_TextChanged;
            htmlview = new HTMLView(this);
            htmlview.Copy += OnCopy;
            vbox1.PackEnd(htmlview.MainWidget, true, true, 0);
            _mainWidget.Destroyed += MainWidgetDestroyed;
        }

        /// <summary>
        /// Invoked when the user wishes to copy data out of the HTMLView.
        /// This is currently only used on Windows, as the other web 
        /// browsers are capable of handling the copy event themselves.
        /// </summary>
        public event EventHandler<CopyEventArgs> Copy;

        /// <summary>Occurs when the name of the simulation is changed by the user</summary>
        public event EventHandler SimulationNameChanged;

        /// <summary>Gets or sets the currently selected simulation name.</summary>
        public string SimulationName
        {
            get
            {
                TreeIter iter;
                if (combobox1.GetActiveIter(out iter))
                    return (string)combobox1.Model.GetValue(iter, 0);
                else
                    return "";
            }
            set
            {
                TreeIter iter;
                if (comboModel.GetIterFirst(out iter))
                {
                    string entry = (string)comboModel.GetValue(iter, 0);
                    while (!entry.Equals(value, StringComparison.InvariantCultureIgnoreCase) && comboModel.IterNext(ref iter)) // Should the text matchin be case-insensitive?
                        entry = (string)comboModel.GetValue(iter, 0);
                    if (entry == value)
                        combobox1.SetActiveIter(iter);
                    else // Could not find a matching entry
                        combobox1.Active = -1;
                }
            }
        }

        /// <summary>Gets or sets the simulation names.</summary>
        public IEnumerable<string> SimulationNames
        {
            get
            {
                int nameCount = comboModel.IterNChildren();
                string[] result = new string[nameCount];
                TreeIter iter;
                int i = 0;
                if (combobox1.GetActiveIter(out iter))
                    do
                        result[i++] = (string)comboModel.GetValue(iter, 0);
                    while (comboModel.IterNext(ref iter) && i < nameCount);
                return result;
            }
            set
            {
                comboModel.Clear();
                foreach (string text in value)
                    comboModel.AppendValues(text);
                if (comboModel.IterNChildren() > 0)
                    combobox1.Active = 0;
                else
                    combobox1.Active = -1;
            }
        }

        /// <summary>Sets the content of the summary window.</summary>
        /// <param name="content">The html content</param>
        public void SetSummaryContent(string content)
        {
            this.htmlview.SetContents(content, false);
        }

        /// <summary>
        /// Event handler which fires whenever the combo box's text changes.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            if (this.SimulationNameChanged != null)
                this.SimulationNameChanged(this, e);
        }

        /// <summary>
        /// Event handler for <see cref="htmlview"/>'s copy event.
        /// Propagates the copy event up to the presenter.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnCopy(object sender, CopyEventArgs e)
        {
            Copy?.Invoke(sender, e);
        }


        private void MainWidgetDestroyed(object sender, EventArgs e)
        {
            comboModel.Dispose();
            comboRender.Destroy();
            _mainWidget.Destroyed -= MainWidgetDestroyed;
            htmlview.Copy -= OnCopy;
            _owner = null;
        }
    }
}
