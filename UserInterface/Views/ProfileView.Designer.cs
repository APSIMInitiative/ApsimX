namespace UserInterface.Views
{
    partial class ProfileView
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.ProfileGrid = new GridView();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.PropertyGrid = new GridView();
            this.Graph = new GraphView();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(4);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.ProfileGrid);
            this.splitContainer1.Panel1.Controls.Add(this.splitter1);
            this.splitContainer1.Panel1.Controls.Add(this.PropertyGrid);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.Graph);
            this.splitContainer1.Panel2MinSize = 600;
            this.splitContainer1.Size = new System.Drawing.Size(899, 649);
            this.splitContainer1.SplitterDistance = 41;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 3;
            // 
            // ProfileGrid
            // 
            this.ProfileGrid.AutoFilterOn = false;
            this.ProfileGrid.DataSource = null;
            this.ProfileGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ProfileGrid.GetCurrentCell = null;
            this.ProfileGrid.Location = new System.Drawing.Point(0, 145);
            this.ProfileGrid.Margin = new System.Windows.Forms.Padding(5);
            this.ProfileGrid.Name = "ProfileGrid";
            this.ProfileGrid.NumericFormat = null;
            this.ProfileGrid.ReadOnly = false;
            this.ProfileGrid.RowCount = 0;
            this.ProfileGrid.Size = new System.Drawing.Size(899, 0);
            this.ProfileGrid.TabIndex = 4;
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter1.Location = new System.Drawing.Point(0, 141);
            this.splitter1.Margin = new System.Windows.Forms.Padding(4);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(899, 4);
            this.splitter1.TabIndex = 5;
            this.splitter1.TabStop = false;
            // 
            // PropertyGrid
            // 
            this.PropertyGrid.AutoFilterOn = false;
            this.PropertyGrid.DataSource = null;
            this.PropertyGrid.Dock = System.Windows.Forms.DockStyle.Top;
            this.PropertyGrid.GetCurrentCell = null;
            this.PropertyGrid.Location = new System.Drawing.Point(0, 0);
            this.PropertyGrid.Margin = new System.Windows.Forms.Padding(5);
            this.PropertyGrid.Name = "PropertyGrid";
            this.PropertyGrid.NumericFormat = null;
            this.PropertyGrid.ReadOnly = false;
            this.PropertyGrid.RowCount = 0;
            this.PropertyGrid.Size = new System.Drawing.Size(899, 141);
            this.PropertyGrid.TabIndex = 2;
            // 
            // Graph
            // 
            this.Graph.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Graph.Location = new System.Drawing.Point(0, 0);
            this.Graph.Margin = new System.Windows.Forms.Padding(4);
            this.Graph.Name = "Graph";
            this.Graph.Size = new System.Drawing.Size(899, 603);
            this.Graph.TabIndex = 4;
            // 
            // ProfileView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "ProfileView";
            this.Size = new System.Drawing.Size(899, 649);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private GridView ProfileGrid;
        private System.Windows.Forms.Splitter splitter1;
        private GridView PropertyGrid;
        private GraphView Graph;
    }
}
