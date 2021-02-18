namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using Extensions;
    using Gtk;
    using Interfaces;
    using Models.GrazPlan;   // For access to the TSuppAttribute enumeration

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

        private Table table1 = null;
        private Entry tbSulph = null;
        private Entry tbPhos = null;
        private Entry tbADIP2CP = null;
        private Entry tbProtDegrad = null;
        private Entry tbEE = null;
        private Entry tbCP = null;
        private Entry tbME = null;
        private Entry tbDMD = null;
        private Entry tbDM = null;
        private CheckButton cbxRoughage = null;
        private Entry tbAmount = null;
        private Entry tbName = null;
        private Button btnResetAll = null;
        private Button btnReset = null;
        private Button btnDelete = null;
        private Button btnAdd = null;
        private IconView lblDefaultNames = null;
        private Gtk.TreeView lvSupps = null;

        private ListStore suppList = new ListStore(typeof(string));
        private ListStore defNameList = new ListStore(typeof(string));

        private Dictionary<Entry, FoodSupplement.SuppAttribute> entryLookup = new Dictionary<Entry, FoodSupplement.SuppAttribute>();

        public SupplementView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.SupplementView.glade");
            table1 = (Table)builder.GetObject("table1");
            tbSulph = (Entry)builder.GetObject("tbSulph");
            tbPhos = (Entry)builder.GetObject("tbPhos");
            tbADIP2CP = (Entry)builder.GetObject("tbADIP2CP");
            tbProtDegrad = (Entry)builder.GetObject("tbProtDegrad");
            tbEE = (Entry)builder.GetObject("tbEE");
            tbCP = (Entry)builder.GetObject("tbCP");
            tbME = (Entry)builder.GetObject("tbME");
            tbDMD = (Entry)builder.GetObject("tbDMD");
            tbDM = (Entry)builder.GetObject("tbDM");
            cbxRoughage = (CheckButton)builder.GetObject("cbxRoughage");
            tbAmount = (Entry)builder.GetObject("tbAmount");
            tbName = (Entry)builder.GetObject("tbName");
            btnResetAll = (Button)builder.GetObject("btnResetAll");
            btnReset = (Button)builder.GetObject("btnReset");
            btnDelete = (Button)builder.GetObject("btnDelete");
            btnAdd = (Button)builder.GetObject("btnAdd");
            lblDefaultNames = (IconView)builder.GetObject("lbDefaultNames");
            lvSupps = (Gtk.TreeView)builder.GetObject("lvSupps");
            mainWidget = table1;

            entryLookup.Add(tbDM, FoodSupplement.SuppAttribute.spaDMP);
            entryLookup.Add(tbDMD, FoodSupplement.SuppAttribute.spaDMD);
            entryLookup.Add(tbME, FoodSupplement.SuppAttribute.spaMEDM);
            entryLookup.Add(tbEE, FoodSupplement.SuppAttribute.spaEE);
            entryLookup.Add(tbCP, FoodSupplement.SuppAttribute.spaCP);
            entryLookup.Add(tbProtDegrad, FoodSupplement.SuppAttribute.spaDG);
            entryLookup.Add(tbADIP2CP, FoodSupplement.SuppAttribute.spaADIP);
            entryLookup.Add(tbPhos, FoodSupplement.SuppAttribute.spaPH);
            entryLookup.Add(tbSulph, FoodSupplement.SuppAttribute.spaSU);

            lvSupps.Model = suppList;
            lblDefaultNames.Model = defNameList;
            lblDefaultNames.TextColumn = 0;
            lblDefaultNames.ItemActivated += LbDefaultNames_Click;
            lblDefaultNames.LeaveNotifyEvent += LbDefaultNames_Leave;

            CellRendererText textRender = new Gtk.CellRendererText();
            TreeViewColumn column = new TreeViewColumn("Supplement Names", textRender, "text", 0);
            lvSupps.AppendColumn(column);
            lvSupps.HeadersVisible = false;

            tbName.Changed += TbName_Validating;
            tbDM.Changed += RealEditValidator;
            tbDMD.Changed += RealEditValidator;
            tbME.Changed += RealEditValidator;
            tbEE.Changed += RealEditValidator;
            tbCP.Changed += RealEditValidator;
            tbProtDegrad.Changed += RealEditValidator;
            tbADIP2CP.Changed += RealEditValidator;
            tbPhos.Changed += RealEditValidator;
            tbSulph.Changed += RealEditValidator;
            tbAmount.Changed += TbAmount_Validating;
            btnAdd.Clicked += BtnAdd_Click;
            btnDelete.Clicked += BtnDelete_Click;
            btnReset.Clicked += BtnReset_Click;
            btnResetAll.Clicked += BtnResetAll_Click;
            cbxRoughage.Toggled += CbxRoughage_CheckedChanged;
            lblDefaultNames.LeaveNotifyEvent += LbDefaultNames_Leave;
            lblDefaultNames.Visible = false;
            lvSupps.CursorChanged += LvSupps_SelectedIndexChanged;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                lblDefaultNames.ItemActivated -= LbDefaultNames_Click;
                lblDefaultNames.LeaveNotifyEvent -= LbDefaultNames_Leave;
                tbName.Changed -= TbName_Validating;
                tbDM.Changed -= RealEditValidator;
                tbDMD.Changed -= RealEditValidator;
                tbME.Changed -= RealEditValidator;
                tbEE.Changed -= RealEditValidator;
                tbCP.Changed -= RealEditValidator;
                tbProtDegrad.Changed -= RealEditValidator;
                tbADIP2CP.Changed -= RealEditValidator;
                tbPhos.Changed -= RealEditValidator;
                tbSulph.Changed -= RealEditValidator;
                tbAmount.Changed -= TbAmount_Validating;
                btnAdd.Clicked -= BtnAdd_Click;
                btnDelete.Clicked -= BtnDelete_Click;
                btnReset.Clicked -= BtnReset_Click;
                btnResetAll.Clicked -= BtnResetAll_Click;
                cbxRoughage.Toggled -= CbxRoughage_CheckedChanged;
                lblDefaultNames.LeaveNotifyEvent -= LbDefaultNames_Leave;
                lvSupps.CursorChanged -= LvSupps_SelectedIndexChanged;
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void RealEditValidator(object sender, EventArgs e)
        {
            try
            {
                Entry tb = sender as Entry;
                if (tb != null)
                {
                    FoodSupplement.SuppAttribute tagEnum;
                    if (entryLookup.TryGetValue(tb, out tagEnum))
                    {
                        double maxVal = 0.0;
                        double scale = 1.0;
                        switch (tagEnum)
                        {
                            case FoodSupplement.SuppAttribute.spaDMP:
                            case FoodSupplement.SuppAttribute.spaDMD:
                            case FoodSupplement.SuppAttribute.spaEE:
                            case FoodSupplement.SuppAttribute.spaDG:
                                maxVal = 100.0;
                                scale = 0.01;
                                break;
                            case FoodSupplement.SuppAttribute.spaMEDM:
                                maxVal = 20.0;
                                break;
                            case FoodSupplement.SuppAttribute.spaCP:
                                maxVal = 300.0;
                                scale = 0.01;
                                break;
                            case FoodSupplement.SuppAttribute.spaPH:
                            case FoodSupplement.SuppAttribute.spaSU:
                            case FoodSupplement.SuppAttribute.spaADIP:
                                maxVal = 100.0;  
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
                            // e.Cancel = true;
                            cancel = true;
                            MessageDialog md = new MessageDialog(MainWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.Ok,
                                               String.Format("Value should be a number in the range 0 to {0:F2}", maxVal));
                            md.Title = "Invalid entry";
                            md.Run();
                            md.Cleanup();
                        }
                        if (!cancel)
                        {
                            if (SuppAttrChanged != null)
                            {
                                TSuppAttrArgs args = new TSuppAttrArgs();
                                args.Attr = (int)tagEnum;
                                args.AttrVal = value * scale;
                                if (SuppAttrChanged != null)
                                    SuppAttrChanged.Invoke(sender, args);
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                TreeIter first;
                if (defNameList.GetIterFirst(out first))
                    lblDefaultNames.SelectPath(defNameList.GetPath(first));
                lblDefaultNames.Visible = true;
                lblDefaultNames.GrabFocus();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (SupplementDeleted != null)
                    SupplementDeleted.Invoke(sender, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            try
            {
                if (SupplementReset != null)
                    SupplementReset.Invoke(sender, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void BtnResetAll_Click(object sender, EventArgs e)
        {
            try
            {
                if (AllSupplementsReset != null)
                    AllSupplementsReset.Invoke(sender, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void LvSupps_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (!internalSelect && SupplementSelected != null)
                {
                    TreePath selPath;
                    TreeViewColumn selCol;
                    lvSupps.GetCursor(out selPath, out selCol);

                    TIntArgs args = new TIntArgs();
                    args.Value = selPath.Indices[0];
                    if (SupplementSelected != null)
                        SupplementSelected.Invoke(sender, args);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
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

        public SupplementItem SelectedSupplementValues
        {
            set
            {
                tbName.Text = value.Name;
                SetEditValue(tbAmount, value.Amount.ToString("F"));
                cbxRoughage.Active = value.IsRoughage;
                SetEditValue(tbDM, (value.DMPropn * 100.0).ToString("F"));
                SetEditValue(tbDMD, (value.DMDigestibility * 100.0).ToString("F"));
                SetEditValue(tbME, value.ME2DM.ToString("F"));
                SetEditValue(tbEE, (value.EtherExtract * 100.0).ToString("F"));
                SetEditValue(tbCP, (value.CrudeProt * 100.0).ToString("F"));
                SetEditValue(tbProtDegrad, (value.DegProt * 100.0).ToString("F"));
                SetEditValue(tbADIP2CP, (value.ADIP2CP * 100.0).ToString("F"));
                SetEditValue(tbPhos, (value.Phosphorus * 100.0).ToString("F"));
                SetEditValue(tbSulph, (value.Sulphur * 100.0).ToString("F"));
            }
        }

        /// <summary>
        /// We do this a bit indirectly, so that if we've just modified a value in a Entry widget,
        /// and that widget still has focus, we don't try to change its value
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="text"></param>
        private void SetEditValue(Entry entry, string text)
        {
            if (!entry.IsFocus)
                entry.Text = text;
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

        private void CbxRoughage_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                TSuppAttrArgs args = new TSuppAttrArgs();
                args.Attr = -2;
                args.AttrVal = cbxRoughage.Active ? 1 : 0;
                if (SuppAttrChanged != null)
                    SuppAttrChanged.Invoke(sender, args);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void TbAmount_Validating(object sender, EventArgs e)
        {
            try
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
                    md.Run();
                    md.Cleanup();
                }
                if (!cancel)
                {
                    if (SuppAttrChanged != null)
                    {
                        TSuppAttrArgs args = new TSuppAttrArgs();
                        args.Attr = -1;
                        args.AttrVal = value;
                        if (SuppAttrChanged != null)
                            SuppAttrChanged.Invoke(sender, args);
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void TbName_Validating(object sender, EventArgs e)
        {
            try
            {
                bool cancel = false;
                if (string.IsNullOrWhiteSpace(tbName.Text) && SupplementNames.Length > 0)
                {
                    cancel = true;
                    MessageDialog md = new MessageDialog(MainWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Warning, ButtonsType.Ok,
                                       "You must provide a name for the supplement");
                    md.Title = "Invalid entry";
                    md.Run();
                    md.Cleanup();
                }
                if (!cancel)
                {
                    if (SuppAttrChanged != null)
                    {
                        TStringArgs args = new TStringArgs();
                        args.Name = tbName.Text;
                        if (SuppNameChanged != null)
                            SuppNameChanged.Invoke(sender, args);
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void LbDefaultNames_Click(object sender, ItemActivatedArgs e)
        {
            try
            {
                if (SupplementAdded != null && e.Path.Indices[0] > 0)
                {
                    TreeIter iter;
                    if (defNameList.GetIter(out iter, e.Path))
                    {
                        TStringArgs args = new TStringArgs();
                        args.Name = (string)defNameList.GetValue(iter, 0);
                        if (SupplementAdded != null)
                            SupplementAdded.Invoke(sender, args);
                    }
                }
                lblDefaultNames.Visible = false;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void LbDefaultNames_Leave(object sender, EventArgs e)
        {
            try
            {
                lblDefaultNames.Visible = false;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
