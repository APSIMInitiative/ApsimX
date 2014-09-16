namespace UserInterface.Views
{
    partial class SeriesView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListView listView1;
        private SeriesEditorView seriesEditorView1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem addToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearAllSeriesToolStripMenuItem;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearAllSeriesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.seriesEditorView1 = new SeriesEditorView();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listView1
            // 
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.listView1.ContextMenuStrip = this.contextMenuStrip1;
            this.listView1.FullRowSelect = true;
            this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listView1.HideSelection = false;
            this.listView1.LabelEdit = true;
            this.listView1.LabelWrap = false;
            this.listView1.Location = new System.Drawing.Point(16, 14);
            this.listView1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.listView1.MultiSelect = false;
            this.listView1.Name = "listView1";
            this.listView1.ShowGroups = false;
            this.listView1.Size = new System.Drawing.Size(213, 259);
            this.listView1.TabIndex = 69;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.OnSeriesRenameAfterLabelEdit);
            this.listView1.SelectedIndexChanged += new System.EventHandler(this.OnListView1SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Width = 154;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addToolStripMenuItem,
            this.deleteToolStripMenuItem,
            this.clearAllSeriesToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(174, 76);
            // 
            // addToolStripMenuItem
            // 
            this.addToolStripMenuItem.Name = "addToolStripMenuItem";
            this.addToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Insert;
            this.addToolStripMenuItem.Size = new System.Drawing.Size(173, 24);
            this.addToolStripMenuItem.Text = "Add";
            this.addToolStripMenuItem.Click += new System.EventHandler(this.OnAddToolStripMenuItemClick);
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(173, 24);
            this.deleteToolStripMenuItem.Text = "Delete";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.OnDeleteToolStripMenuItemClick);
            // 
            // clearAllSeriesToolStripMenuItem
            // 
            this.clearAllSeriesToolStripMenuItem.Name = "clearAllSeriesToolStripMenuItem";
            this.clearAllSeriesToolStripMenuItem.Size = new System.Drawing.Size(173, 24);
            this.clearAllSeriesToolStripMenuItem.Text = "Clear all series";
            this.clearAllSeriesToolStripMenuItem.Click += new System.EventHandler(this.OnClearAllSeriesToolStripMenuItemClick);
            // 
            // seriesEditorView1
            // 
            this.seriesEditorView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.seriesEditorView1.Colour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.seriesEditorView1.DataSource = "";
            this.seriesEditorView1.Location = new System.Drawing.Point(238, 14);
            this.seriesEditorView1.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.seriesEditorView1.Name = "seriesEditorView1";
            this.seriesEditorView1.Regression = false;
            this.seriesEditorView1.SeriesLineType = "Solid";
            this.seriesEditorView1.SeriesMarkerType = "None";
            this.seriesEditorView1.SeriesType = "Scatter";
            this.seriesEditorView1.ShowInLegend = false;
            this.seriesEditorView1.Size = new System.Drawing.Size(685, 272);
            this.seriesEditorView1.SplitOn = "Simulation";
            this.seriesEditorView1.TabIndex = 70;
            this.seriesEditorView1.Visible = false;
            this.seriesEditorView1.X = "";
            this.seriesEditorView1.X2 = "";
            this.seriesEditorView1.XOnTop = false;
            this.seriesEditorView1.Y = "";
            this.seriesEditorView1.Y2 = "";
            this.seriesEditorView1.YOnRight = false;
            // 
            // SeriesView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.seriesEditorView1);
            this.Controls.Add(this.listView1);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "SeriesView";
            this.Size = new System.Drawing.Size(947, 288);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
    }
}