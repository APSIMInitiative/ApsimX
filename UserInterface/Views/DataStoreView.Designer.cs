// -----------------------------------------------------------------------
// <copyright file="DataStoreView.Designer.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    /// <summary>
    /// A data store view
    /// </summary>
    public partial class DataStoreView
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// An output grid view.
        /// </summary>
        private GridView gridView;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.editView1 = new EditView();
            this.dropDownView1 = new DropDownView();
            this.gridView = new GridView();
            this.label3 = new System.Windows.Forms.Label();
            this.editView2 = new EditView();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.editView1);
            this.panel1.Controls.Add(this.dropDownView1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(603, 79);
            this.panel1.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(90, 17);
            this.label2.TabIndex = 8;
            this.label2.Text = "Column filter:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 17);
            this.label1.TabIndex = 7;
            this.label1.Text = "Table:";
            // 
            // editView1
            // 
            this.editView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.editView1.AutoSize = true;
            this.editView1.IsVisible = true;
            this.editView1.Location = new System.Drawing.Point(99, 41);
            this.editView1.Name = "editView1";
            this.editView1.Size = new System.Drawing.Size(489, 24);
            this.editView1.TabIndex = 3;
            this.editView1.Value = "";
            // 
            // dropDownView1
            // 
            this.dropDownView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dropDownView1.AutoSize = true;
            this.dropDownView1.IsEditable = true;
            this.dropDownView1.IsVisible = true;
            this.dropDownView1.Location = new System.Drawing.Point(99, 11);
            this.dropDownView1.Name = "dropDownView1";
            this.dropDownView1.SelectedValue = null;
            this.dropDownView1.Size = new System.Drawing.Size(489, 24);
            this.dropDownView1.TabIndex = 2;
            this.dropDownView1.Values = new string[0];
            // 
            // gridView
            // 
            this.gridView.AutoFilterOn = false;
            this.gridView.DataSource = null;
            this.gridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridView.GetCurrentCell = null;
            this.gridView.Location = new System.Drawing.Point(0, 79);
            this.gridView.Margin = new System.Windows.Forms.Padding(5);
            this.gridView.ModelName = null;
            this.gridView.Name = "gridView";
            this.gridView.NumericFormat = null;
            this.gridView.ReadOnly = false;
            this.gridView.RowCount = 0;
            this.gridView.Size = new System.Drawing.Size(603, 526);
            this.gridView.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 15);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(161, 17);
            this.label3.TabIndex = 9;
            this.label3.Text = "Max. number of records:";
            // 
            // editView2
            // 
            this.editView2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.editView2.AutoSize = true;
            this.editView2.IsVisible = true;
            this.editView2.Location = new System.Drawing.Point(174, 15);
            this.editView2.Name = "editView2";
            this.editView2.Size = new System.Drawing.Size(73, 24);
            this.editView2.TabIndex = 10;
            this.editView2.Value = "";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.editView2);
            this.panel2.Controls.Add(this.label3);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 553);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(603, 52);
            this.panel2.TabIndex = 11;
            // 
            // DataStoreView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.gridView);
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "DataStoreView";
            this.Size = new System.Drawing.Size(603, 605);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private EditView editView1;
        private DropDownView dropDownView1;
        private System.Windows.Forms.Label label3;
        private EditView editView2;
        private System.Windows.Forms.Panel panel2;
    }
}
