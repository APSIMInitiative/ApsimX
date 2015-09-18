namespace UserInterface.Views
{
    partial class TreeProxyView
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
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
            this.dgvHeights = new System.Windows.Forms.DataGridView();
            this.Grid = new System.Windows.Forms.DataGridView();
            this.pBelowGround = new OxyPlot.WindowsForms.PlotView();
            this.pAboveGround = new OxyPlot.WindowsForms.PlotView();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.panel2 = new System.Windows.Forms.Panel();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Date = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TreeHeight = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NDemands = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.gridView1 = new GridView();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHeights)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Grid)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.AutoSize = true;
            this.panel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Controls.Add(this.splitter1);
            this.panel1.Controls.Add(this.tabControl1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(703, 693);
            this.panel1.TabIndex = 0;
            // 
            // dgvHeights
            // 
            this.dgvHeights.AllowDrop = true;
            this.dgvHeights.AllowUserToResizeColumns = false;
            this.dgvHeights.AllowUserToResizeRows = false;
            this.dgvHeights.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgvHeights.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Date,
            this.TreeHeight,
            this.NDemands});
            this.dgvHeights.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHeights.Location = new System.Drawing.Point(3, 3);
            this.dgvHeights.Margin = new System.Windows.Forms.Padding(4);
            this.dgvHeights.Name = "dgvHeights";
            this.dgvHeights.Size = new System.Drawing.Size(689, 278);
            this.dgvHeights.TabIndex = 7;
            this.dgvHeights.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgvHeights_RowsAdded);
            this.dgvHeights.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.dgvHeights_RowsRemoved);
            // 
            // Grid
            // 
            this.Grid.AllowUserToAddRows = false;
            this.Grid.AllowUserToDeleteRows = false;
            this.Grid.AllowUserToResizeColumns = false;
            this.Grid.AllowUserToResizeRows = false;
            this.Grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.Grid.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.Grid.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.Grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.Grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Grid.Location = new System.Drawing.Point(3, 3);
            this.Grid.Margin = new System.Windows.Forms.Padding(4);
            this.Grid.Name = "Grid";
            this.Grid.RowHeadersVisible = false;
            this.Grid.Size = new System.Drawing.Size(689, 278);
            this.Grid.TabIndex = 4;
            this.Grid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.Grid_CellEndEdit);
            this.Grid.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.Grid_EditingControlShowing);
            this.Grid.SelectionChanged += new System.EventHandler(this.Grid_SelectionChanged);
            this.Grid.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Grid_KeyDown);
            this.Grid.KeyUp += new System.Windows.Forms.KeyEventHandler(this.Grid_KeyUp);
            this.Grid.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.Grid_PreviewKeyDown);
            // 
            // pBelowGround
            // 
            this.pBelowGround.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pBelowGround.Location = new System.Drawing.Point(333, 0);
            this.pBelowGround.Margin = new System.Windows.Forms.Padding(4);
            this.pBelowGround.Name = "pBelowGround";
            this.pBelowGround.PanCursor = System.Windows.Forms.Cursors.Hand;
            this.pBelowGround.Size = new System.Drawing.Size(370, 377);
            this.pBelowGround.TabIndex = 3;
            this.pBelowGround.Text = "plot1";
            this.pBelowGround.ZoomHorizontalCursor = System.Windows.Forms.Cursors.SizeWE;
            this.pBelowGround.ZoomRectangleCursor = System.Windows.Forms.Cursors.SizeNWSE;
            this.pBelowGround.ZoomVerticalCursor = System.Windows.Forms.Cursors.SizeNS;
            // 
            // pAboveGround
            // 
            this.pAboveGround.Dock = System.Windows.Forms.DockStyle.Left;
            this.pAboveGround.Location = new System.Drawing.Point(0, 0);
            this.pAboveGround.Margin = new System.Windows.Forms.Padding(4);
            this.pAboveGround.Name = "pAboveGround";
            this.pAboveGround.PanCursor = System.Windows.Forms.Cursors.Hand;
            this.pAboveGround.Size = new System.Drawing.Size(333, 377);
            this.pAboveGround.TabIndex = 2;
            this.pAboveGround.Text = "plot1";
            this.pAboveGround.ZoomHorizontalCursor = System.Windows.Forms.Cursors.SizeWE;
            this.pAboveGround.ZoomRectangleCursor = System.Windows.Forms.Cursors.SizeNWSE;
            this.pAboveGround.ZoomVerticalCursor = System.Windows.Forms.Cursors.SizeNS;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(703, 313);
            this.tabControl1.TabIndex = 8;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.dgvHeights);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(695, 284);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Temporal Data";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.Grid);
            this.tabPage2.Location = new System.Drawing.Point(4, 25);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(695, 284);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Spatial Data";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter1.Location = new System.Drawing.Point(0, 313);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(703, 3);
            this.splitter1.TabIndex = 9;
            this.splitter1.TabStop = false;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.pBelowGround);
            this.panel2.Controls.Add(this.pAboveGround);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 316);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(703, 377);
            this.panel2.TabIndex = 10;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "Description";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 85;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.HeaderText = "Value";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.Width = 59;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.HeaderText = "Description";
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 85;
            // 
            // Date
            // 
            this.Date.HeaderText = "Date";
            this.Date.Name = "Date";
            this.Date.Width = 63;
            // 
            // TreeHeight
            // 
            this.TreeHeight.HeaderText = "Height (m)";
            this.TreeHeight.Name = "TreeHeight";
            this.TreeHeight.Width = 99;
            // 
            // NDemands
            // 
            this.NDemands.HeaderText = "N Demands (g/m2)";
            this.NDemands.Name = "NDemands";
            this.NDemands.Width = 152;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.HeaderText = "Value";
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.Width = 59;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.gridView1);
            this.tabPage3.Location = new System.Drawing.Point(4, 25);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(695, 284);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Constants";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // gridView1
            // 
            this.gridView1.AutoFilterOn = false;
            this.gridView1.DataSource = null;
            this.gridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridView1.GetCurrentCell = null;
            this.gridView1.Location = new System.Drawing.Point(0, 0);
            this.gridView1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gridView1.ModelName = null;
            this.gridView1.Name = "gridView1";
            this.gridView1.NumericFormat = null;
            this.gridView1.ReadOnly = false;
            this.gridView1.RowCount = 0;
            this.gridView1.Size = new System.Drawing.Size(695, 284);
            this.gridView1.TabIndex = 0;
            // 
            // TreeProxyView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "TreeProxyView";
            this.Size = new System.Drawing.Size(703, 693);
            this.Resize += new System.EventHandler(this.ForestryView_Resize);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvHeights)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Grid)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private OxyPlot.WindowsForms.PlotView pBelowGround;
        private OxyPlot.WindowsForms.PlotView pAboveGround;
        private System.Windows.Forms.DataGridView Grid;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridView dgvHeights;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn Date;
        private System.Windows.Forms.DataGridViewTextBoxColumn TreeHeight;
        private System.Windows.Forms.DataGridViewTextBoxColumn NDemands;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TabPage tabPage3;
        private GridView gridView1;
    }
}
