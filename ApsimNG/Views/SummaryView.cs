using Glade;
using Gtk;
using System;
using System.Collections.Generic;

namespace UserInterface.Views
{
    /// <summary>
    /// A view for a summary file.
    /// </summary>
    public class SummaryView : ViewBase, ISummaryView
    {
        [Widget]
        private VBox vbox1 = null;
        [Widget]
        private ComboBox combobox1 = null;
        private ListStore comboModel = new ListStore(typeof(string));
        private CellRendererText comboRender = new CellRendererText();
        private HTMLView htmlview;

        /// <summary>Initializes a new instance of the <see cref="SummaryView"/> class.</summary>
        public SummaryView(ViewBase owner) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.SummaryView.glade", "vbox1");
            gxml.Autoconnect(this);
            _mainWidget = vbox1;
            combobox1.PackStart(comboRender, false);
            combobox1.AddAttribute(comboRender, "text", 0);
            combobox1.Model = comboModel;
            htmlview = new HTMLView(this);
            vbox1.PackEnd(htmlview.MainWidget, true, true, 0);
        }

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
                int nNames = comboModel.IterNChildren();
                string[] result = new string[nNames];
                TreeIter iter;
                int i = 0;
                if (combobox1.GetActiveIter(out iter))
                    do
                        result[i++] = (string)comboModel.GetValue(iter, 0);
                    while (comboModel.IterNext(ref iter) && i < nNames);
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
            /// TBI htmlView1.UseMonoSpacedFont();
            this.htmlview.SetContents(content, false);
        }

        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            if (this.SimulationNameChanged != null)
                this.SimulationNameChanged(this, e);
        }
    }
}
