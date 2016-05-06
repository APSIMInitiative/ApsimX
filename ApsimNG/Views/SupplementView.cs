// -----------------------------------------------------------------------
// <copyright file="SupplementView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Glade;
    using Gtk;
    using Interfaces;
    using Models.Grazplan;   // For access to the TSuppAttribute enumeration

    public class SupplementView : ViewBase, ISupplementView
    {
        /// <summary>
        /// Invoked when a supplement has been selected by user.
        /// </summary>
        public event EventHandler<TIntArgs> SupplementSelected;

        /// <summary>
        /// Invoked when a new supplement is added.
        /// </summary>
        public event EventHandler<TStringArgs> SupplementAdded;

        /// <summary>
        /// Invoked when a supplement is deleted.
        /// </summary>
        public event EventHandler SupplementDeleted;

        /// <summary>
        /// Invoked when a supplement is reset to default values.
        /// </summary>
        public event EventHandler SupplementReset;

        /// <summary>
        /// Invoked when all supplements are reset.
        /// </summary>
        public event EventHandler AllSupplementsReset;

        public event EventHandler<TSuppAttrArgs> SuppAttrChanged;

        public event EventHandler<TStringArgs> SuppNameChanged;

        [Widget]
        private Table table1;
        [Widget]
        private Entry tbSulph;
        [Widget]
        private Entry tbPhos;
        [Widget]
        private Entry tbADIP2CP;
        [Widget]
        private Entry tbProtDegrad;
        [Widget]
        private Entry tbEE;
        [Widget]
        private Entry tbCP;
        [Widget]
        private Entry tbME;
        [Widget]
        private Entry tbDMD;
        [Widget]
        private Entry tbDM;
        [Widget]
        private CheckButton cbxRoughage;
        [Widget]
        private Entry tbAmount;
        [Widget]
        private Entry tbName;
        [Widget]
        private Button btnResetAll;
        [Widget]
        private Button btnReset;
        [Widget]
        private Button btnDelete;
        [Widget]
        private Button btnAdd;
        [Widget]
        private IconView lbDefaultNames;
        [Widget]
        private TreeView lvSupps;

        private ListStore suppList = new ListStore(typeof(string));
        private ListStore defNameList = new ListStore(typeof(string));

        private Dictionary<Entry, TSupplement.TSuppAttribute> entryLookup = new Dictionary<Entry, TSupplement.TSuppAttribute>();

        public SupplementView(ViewBase owner) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.SupplementView.glade", "table1");
            gxml.Autoconnect(this);
            _mainWidget = table1;

            entryLookup.Add(tbDM, TSupplement.TSuppAttribute.spaDMP);
            entryLookup.Add(tbDMD, TSupplement.TSuppAttribute.spaDMD);
            entryLookup.Add(tbME, TSupplement.TSuppAttribute.spaMEDM);
            entryLookup.Add(tbEE, TSupplement.TSuppAttribute.spaEE);
            entryLookup.Add(tbCP, TSupplement.TSuppAttribute.spaCP);
            entryLookup.Add(tbProtDegrad, TSupplement.TSuppAttribute.spaDG);
            entryLookup.Add(tbADIP2CP, TSupplement.TSuppAttribute.spaADIP);
            entryLookup.Add(tbPhos, TSupplement.TSuppAttribute.spaPH);
            entryLookup.Add(tbSulph, TSupplement.TSuppAttribute.spaSU);

            lvSupps.Model = suppList;
            lbDefaultNames.Model = defNameList;
            lbDefaultNames.TextColumn = 0;
            lbDefaultNames.ItemActivated += lbDefaultNames_Click;
            lbDefaultNames.LeaveNotifyEvent += lbDefaultNames_Leave;

            CellRendererText textRender = new Gtk.CellRendererText();
            TreeViewColumn column = new TreeViewColumn("Supplement Names", textRender, "text", 0);
            lvSupps.AppendColumn(column);
            lvSupps.HeadersVisible = false;

            tbName.Changed += tbName_Validating;
            tbDM.Changed += RealEditValidator;
            tbDMD.Changed += RealEditValidator;
            tbME.Changed += RealEditValidator;
            tbEE.Changed += RealEditValidator;
            tbCP.Changed += RealEditValidator;
            tbProtDegrad.Changed += RealEditValidator;
            tbADIP2CP.Changed += RealEditValidator;
            tbPhos.Changed += RealEditValidator;
            tbSulph.Changed += RealEditValidator;
            tbAmount.Changed += tbAmount_Validating;
            btnAdd.Clicked += btnAdd_Click;
            btnDelete.Clicked += btnDelete_Click;
            btnReset.Clicked += btnReset_Click;
            btnResetAll.Clicked += btnResetAll_Click;
            cbxRoughage.Toggled += cbxRoughage_CheckedChanged;
            lbDefaultNames.LeaveNotifyEvent += lbDefaultNames_Leave;
            lbDefaultNames.Visible = false;
            lvSupps.CursorChanged += lvSupps_SelectedIndexChanged;
        }

        private void RealEditValidator(object sender, EventArgs e)
        {
            Entry tb = sender as Entry;
            if (tb != null)
            {
                TSupplement.TSuppAttribute tagEnum;
                if (entryLookup.TryGetValue(tb, out tagEnum))
                {
                    double maxVal = 0.0;
                    double scale = 1.0;
                    switch (tagEnum)
                    {
                        case TSupplement.TSuppAttribute.spaDMP:
                        case TSupplement.TSuppAttribute.spaDMD:
                        case TSupplement.TSuppAttribute.spaEE:
                        case TSupplement.TSuppAttribute.spaDG:
                            maxVal = 100.0;
                            scale = 0.01;
                            break;
                        case TSupplement.TSuppAttribute.spaMEDM:
                            maxVal = 20.0;
                            break;
                        case TSupplement.TSuppAttribute.spaCP:
                            maxVal = 300.0;
                            scale = 0.01;
                            break;
                        case TSupplement.TSuppAttribute.spaPH:
                        case TSupplement.TSuppAttribute.spaSU:
                        case TSupplement.TSuppAttribute.spaADIP:
                            maxVal = 200.0;  // Why 200?
                            scale = 0.01;
                            break;
                        default:
                            maxVal = 100.0;
                            break;
                    }
                    double value;
                    bool cancel = false;
                    if (string.IsNullOrWhiteSpace(tb.Text)) // Treat blank as a value of 0.
                        value = 0.0;
                    else if (!Double.TryParse(tb.Text, out value) || value < 0.0 || value > maxVal)
                    {
                        /// e.Cancel = true;
                        cancel = true;
                        MessageDialog md = new MessageDialog(MainWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.Ok,
                                           String.Format("Value should be a number in the range 0 to {0:F2}", maxVal));
                        md.Title = "Invalid entry";
                        int result = md.Run();
                        md.Destroy();
                    }
                    if (!cancel)
                    {
                        if (SuppAttrChanged != null)
                        {
                            TSuppAttrArgs args = new TSuppAttrArgs();
                            args.attr = (int)tagEnum;
                            args.attrVal = value * scale;
                            SuppAttrChanged.Invoke(sender, args);
                        }
                    }
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            TreeIter first;
            if (defNameList.GetIterFirst(out first))
                lbDefaultNames.SelectPath(defNameList.GetPath(first));
            lbDefaultNames.Visible = true;
            lbDefaultNames.GrabFocus();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (SupplementDeleted != null)
                SupplementDeleted.Invoke(sender, e);
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            if (SupplementReset != null)
                SupplementReset.Invoke(sender, e);
        }

        private void btnResetAll_Click(object sender, EventArgs e)
        {
            if (AllSupplementsReset != null)
                AllSupplementsReset.Invoke(sender, e);
        }

        private void lvSupps_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!internalSelect && SupplementSelected != null)
            {
                TreePath selPath;
                TreeViewColumn selCol;
                lvSupps.GetCursor(out selPath, out selCol);

                TIntArgs args = new TIntArgs();
                args.value = selPath.Indices[0];
                SupplementSelected.Invoke(sender, args);
            }
        }

        /// <summary>
        /// Gets or sets the supplement names.
        /// </summary>
        public string[] SupplementNames
        {
            get
            {
                int nNames = suppList.IterNChildren();
                string[] result = new string[nNames];
                TreeIter iter;
                int i = 0;
                if (suppList.GetIterFirst(out iter))
                    do
                        result[i++] = (string)suppList.GetValue(iter, 0);
                    while (suppList.IterNext(ref iter) && i < nNames);
                return result;
            }
            set
            {
                suppList.Clear();
                foreach (string text in value)
                    suppList.AppendValues(text);
            }

        }

        public string[] DefaultSuppNames
        {
            set
            {
                defNameList.Clear();
                defNameList.AppendValues("(none)");
                foreach (string st in value)
                    defNameList.AppendValues(st);
            }
        }

        /*
        private static double BoxTextToDouble(TextBox tb)
        {
            double value;
            if (string.IsNullOrWhiteSpace(tb.Text))
                return 0.0;
            else if (Double.TryParse(tb.Text, out value))
                return value;
            else
                return Double.NaN;
        }
        */

        private double ashAlk = 0;
        private double maxPassage = 0;

        public TSupplementItem SelectedSupplementValues
        {
            set
            {
                tbName.Text = value.sName;
                tbAmount.Text = value.Amount.ToString("F");
                cbxRoughage.Active = value.IsRoughage;
                tbDM.Text = (value.DM_Propn * 100.0).ToString("F");
                tbDMD.Text = (value.DM_Digestibility * 100.0).ToString("F");
                tbME.Text = value.ME_2_DM.ToString("F");
                tbEE.Text = (value.EtherExtract * 100.0).ToString("F");
                tbCP.Text = (value.CrudeProt * 100.0).ToString("F");
                tbProtDegrad.Text = (value.DgProt * 100.0).ToString("F");
                tbADIP2CP.Text = (value.ADIP_2_CP * 100.0).ToString("F");
                tbPhos.Text = (value.Phosphorus * 100.0).ToString("F");
                tbSulph.Text = (value.Sulphur * 100.0).ToString("F");
                ashAlk = value.AshAlkalinity;
                maxPassage = value.MaxPassage;
            }
        }

        private bool internalSelect = false;

        public int SelectedSupplementIndex
        {
            get
            {
                TreePath selPath;
                TreeViewColumn selCol;
                lvSupps.GetCursor(out selPath, out selCol);
                return selPath != null ? selPath.Indices[0] : 0;
            }
            set
            {
                try
                {
                    if (value >= 0)
                    {
                        internalSelect = true;
                        int[] indices = new int[1] { value };
                        TreePath selPath = new TreePath(indices);
                        lvSupps.SetCursor(selPath, null, false);
                    }
                    else // Clear everything
                    {
                        tbName.Text = "";
                        tbAmount.Text = "";
                        cbxRoughage.Active = false;
                        tbDM.Text = "";
                        tbDMD.Text = "";
                        tbME.Text = "";
                        tbEE.Text = "";
                        tbCP.Text = "";
                        tbProtDegrad.Text = "";
                        tbADIP2CP.Text = "";
                        tbPhos.Text = "";
                        tbSulph.Text = "";
                    }
                }
                finally
                {
                    internalSelect = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected supplement's name.
        /// </summary>
        public string SelectedSupplementName
        {
            get
            {
                TreePath selPath;
                TreeIter iter;
                TreeViewColumn selCol;
                lvSupps.GetCursor(out selPath, out selCol);
                if (selPath != null && suppList.GetIter(out iter, selPath))
                    return (string)suppList.GetValue(iter, 0);
                else
                    return null;
            }

            set
            {
                TreePath selPath;
                TreeIter iter;
                TreeViewColumn selCol;
                lvSupps.GetCursor(out selPath, out selCol);
                if (selPath != null && suppList.GetIter(out iter, selPath))
                    suppList.SetValue(iter, 0, value);
            }
        }

        private void cbxRoughage_CheckedChanged(object sender, EventArgs e)
        {
            TSuppAttrArgs args = new TSuppAttrArgs();
            args.attr = -2;
            args.attrVal = cbxRoughage.Active ? 1 : 0;
            SuppAttrChanged.Invoke(sender, args);
        }

        private void tbAmount_Validating(object sender, EventArgs e)
        {
            double value;
            bool cancel = false;
            if (string.IsNullOrWhiteSpace(tbAmount.Text)) // Treat blank as a value of 0.
                value = 0.0;
            else if (!Double.TryParse(tbAmount.Text, out value) || value < 0.0)
            {
                cancel = true;
                MessageDialog md = new MessageDialog(MainWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.Ok,
                                   "Value should be a non-negative number");
                md.Title = "Invalid entry";
                int result = md.Run();
                md.Destroy();
            }
            if (!cancel)
            {
                if (SuppAttrChanged != null)
                {
                    TSuppAttrArgs args = new TSuppAttrArgs();
                    args.attr = -1;
                    args.attrVal = value;
                    SuppAttrChanged.Invoke(sender, args);
                }
            }
        }

        private void tbName_Validating(object sender, EventArgs e)
        {
            bool cancel = false;
            if (string.IsNullOrWhiteSpace(tbName.Text))
            {
                cancel = true;
                MessageDialog md = new MessageDialog(MainWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.Ok,
                                   "You must provide a name for the supplement");
                md.Title = "Invalid entry";
                int result = md.Run();
                md.Destroy();
            }
            if (!cancel)
            {
                if (SuppAttrChanged != null)
                {
                    TStringArgs args = new TStringArgs();
                    args.name = tbName.Text;
                    SuppNameChanged.Invoke(sender, args);
                }
            }
        }

        private void lbDefaultNames_Click(object sender, ItemActivatedArgs e)
        {
            if (SupplementAdded != null && e.Path.Indices[0] > 0)
            {
                TreeIter iter;
                if (defNameList.GetIter(out iter, e.Path))
                {
                    TStringArgs args = new TStringArgs();
                    args.name = (string)defNameList.GetValue(iter, 0);
                    SupplementAdded.Invoke(sender, args);
                }
            }
            lbDefaultNames.Visible = false;
        }

        private void lbDefaultNames_Leave(object sender, EventArgs e)
        {
            lbDefaultNames.Visible = false;
        }

    }
}
