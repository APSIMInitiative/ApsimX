// -----------------------------------------------------------------------
// <copyright file="SupplementView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Views
{
    using Models.Grazplan;   // For access to the TSuppAttribute enumeration
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;
    using Interfaces;

    public partial class SupplementView : UserControl, ISupplementView
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

        public SupplementView()
        {
            InitializeComponent();
            tbDM.Tag = TSupplement.TSuppAttribute.spaDMP;
            tbDMD.Tag = TSupplement.TSuppAttribute.spaDMD;
            tbME.Tag = TSupplement.TSuppAttribute.spaMEDM;
            tbEE.Tag = TSupplement.TSuppAttribute.spaEE;
            tbCP.Tag = TSupplement.TSuppAttribute.spaCP;
            tbProtDegrad.Tag = TSupplement.TSuppAttribute.spaDG;
            tbADIP2CP.Tag = TSupplement.TSuppAttribute.spaADIP;
            tbPhos.Tag = TSupplement.TSuppAttribute.spaPH;
            tbSulph.Tag = TSupplement.TSuppAttribute.spaSU;
        }

        private void RealEditValidator(object sender, CancelEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                TSupplement.TSuppAttribute tagEnum = (TSupplement.TSuppAttribute)tb.Tag;
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
                if (string.IsNullOrWhiteSpace(tbAmount.Text)) // Treat blank as a value of 0.
                    value = 0.0;
                else if (!Double.TryParse(tb.Text, out value) || value < 0.0 || value > maxVal)
                {
                    e.Cancel = true;
                    MessageBox.Show(String.Format("Value should be a number in the range 0 to {0:F2}", maxVal));
                }
                if (!e.Cancel && tb.Modified)
                {
                    if (SuppAttrChanged != null)
                    {
                        TSuppAttrArgs args = new TSuppAttrArgs();
                        args.attr = (int)tagEnum;
                        args.attrVal = value * scale;
                        SuppAttrChanged.Invoke(sender, args);
                        tb.Modified = false;
                    }
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            lbDefaultNames.SelectedIndex = 0;
            lbDefaultNames.Visible = true;
            lbDefaultNames.Focus();
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
            if (!internalSelect && SupplementSelected != null && lvSupps.SelectedIndices.Count > 0)
            {
                TIntArgs args = new TIntArgs();
                args.value = lvSupps.SelectedIndices[0];
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
                List<string> names = new List<string>();
                foreach (ListViewItem item in lvSupps.Items)
                {
                    names.Add(item.Text);
                }
                return names.ToArray();
            }

            set
            {
                lvSupps.Items.Clear();
                foreach (string st in value)
                {
                    lvSupps.Items.Add(st);
                }
            }
        }

        public string[] DefaultSuppNames
        {
            set
            {
                lbDefaultNames.Items.Clear();
                lbDefaultNames.Items.Add("(none)");
                foreach (string st in value)
                {
                    lbDefaultNames.Items.Add(st);
                }
            }
        }

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

        private double ashAlk = 0;
        private double maxPassage = 0;

        public TSupplementItem SelectedSupplementValues
        {
            set
            {
                tbName.Text = value.sName;
                tbAmount.Text = value.Amount.ToString("F");
                cbxRoughage.Checked = value.IsRoughage;
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
                return lvSupps.SelectedItems[0].Index;
            }
            set
            {
                try
                {
                    if (value >= 0)
                    {
                        internalSelect = true;
                        lvSupps.Items[value].Selected = true;
                    }
                    else // Clear everything
                    {
                        tbName.Text = "";
                        tbAmount.Text = "";
                        cbxRoughage.Checked = false;
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
                if (lvSupps.SelectedItems.Count > 0)
                {
                    return lvSupps.SelectedItems[0].Text;
                }
                return null;
            }

            set
            {
                foreach (ListViewItem item in lvSupps.Items)
                {
                    item.Selected = item.Text == value;
                }
            }
        }

        private void cbxRoughage_CheckedChanged(object sender, EventArgs e)
        {
            TSuppAttrArgs args = new TSuppAttrArgs();
            args.attr = -2;
            args.attrVal = cbxRoughage.Checked ? 1 : 0;
            SuppAttrChanged.Invoke(sender, args);
        }

        private void tbAmount_Validating(object sender, CancelEventArgs e)
        {
            double value;
            if (string.IsNullOrWhiteSpace(tbAmount.Text)) // Treat blank as a value of 0.
                value = 0.0;
            else if (!Double.TryParse(tbAmount.Text, out value) || value < 0.0 )
            {
                e.Cancel = true;
                MessageBox.Show(String.Format("Value should be a non-negative number"));
            }
            if (!e.Cancel && tbAmount.Modified)
            {
                if (SuppAttrChanged != null)
                {
                    TSuppAttrArgs args = new TSuppAttrArgs();
                    args.attr = -1;
                    args.attrVal = value;
                    SuppAttrChanged.Invoke(sender, args);
                    tbAmount.Modified = false;
                }
            }
        }

        private void tbName_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbName.Text)) 
            {
                e.Cancel = true;
                MessageBox.Show(String.Format("You must provide a name for the supplement"));
            }
            if (!e.Cancel && tbName.Modified)
            {
                if (SuppAttrChanged != null)
                {
                    TStringArgs args = new TStringArgs();
                    args.name = tbName.Text;
                    SuppNameChanged.Invoke(sender, args);
                    tbName.Modified = false;
                }
            }
        }

        private void lbDefaultNames_Click(object sender, EventArgs e)
        {
            if (SupplementAdded != null && lbDefaultNames.SelectedIndex > 0)
            {
                TStringArgs args = new TStringArgs();
                args.name = (string)lbDefaultNames.Items[lbDefaultNames.SelectedIndex];
                SupplementAdded.Invoke(sender, args);
            }
            lbDefaultNames.Visible = false;
        }

        private void lbDefaultNames_Leave(object sender, EventArgs e)
        {
            lbDefaultNames.Visible = false;
        }

    }
}
