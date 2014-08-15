namespace UserInterface.Views
{
    partial class TabbedExplorerView
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TabbedExplorerView));
            this.TabControl = new System.Windows.Forms.TabControl();
            this.StartPage = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.ListView = new System.Windows.Forms.ListView();
            this.ListViewImages = new System.Windows.Forms.ImageList(this.components);
            this.TabPopupMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CloseTabMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.TabImageList = new System.Windows.Forms.ImageList(this.components);
            this.OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.TabControl.SuspendLayout();
            this.StartPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.TabPopupMenu.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // TabControl
            // 
            this.TabControl.Controls.Add(this.StartPage);
            this.TabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TabControl.Location = new System.Drawing.Point(0, 0);
            this.TabControl.Name = "TabControl";
            this.TabControl.SelectedIndex = 0;
            this.TabControl.Size = new System.Drawing.Size(610, 523);
            this.TabControl.TabIndex = 0;
            this.TabControl.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OnTabControlMouseUp);
            // 
            // StartPage
            // 
            this.StartPage.Controls.Add(this.splitContainer1);
            this.StartPage.Location = new System.Drawing.Point(4, 22);
            this.StartPage.Name = "StartPage";
            this.StartPage.Padding = new System.Windows.Forms.Padding(3);
            this.StartPage.Size = new System.Drawing.Size(602, 497);
            this.StartPage.TabIndex = 0;
            this.StartPage.Text = " ";
            this.StartPage.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.ListView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.listBox1);
            this.splitContainer1.Panel2.Controls.Add(this.panel1);
            this.splitContainer1.Size = new System.Drawing.Size(596, 491);
            this.splitContainer1.SplitterDistance = 355;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 4;
            // 
            // ListView
            // 
            this.ListView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListView.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ListView.LargeImageList = this.ListViewImages;
            this.ListView.Location = new System.Drawing.Point(0, 0);
            this.ListView.MultiSelect = false;
            this.ListView.Name = "ListView";
            this.ListView.ShowItemToolTips = true;
            this.ListView.Size = new System.Drawing.Size(594, 353);
            this.ListView.TabIndex = 3;
            this.ListView.TileSize = new System.Drawing.Size(400, 100);
            this.ListView.UseCompatibleStateImageBehavior = false;
            this.ListView.DoubleClick += new System.EventHandler(this.ListView_DoubleClick);
            this.ListView.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ListView_KeyUp);
            // 
            // ListViewImages
            // 
            this.ListViewImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ListViewImages.ImageStream")));
            this.ListViewImages.TransparentColor = System.Drawing.Color.Transparent;
            this.ListViewImages.Images.SetKeyName(0, "open_file-icon.gif");
            this.ListViewImages.Images.SetKeyName(1, "chest.png");
            this.ListViewImages.Images.SetKeyName(2, "chart.png");
            this.ListViewImages.Images.SetKeyName(3, "user1.png");
            this.ListViewImages.Images.SetKeyName(4, "import2.png");
            // 
            // TabPopupMenu
            // 
            this.TabPopupMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CloseTabMenuItem});
            this.TabPopupMenu.Name = "TabPopupMenu";
            this.TabPopupMenu.Size = new System.Drawing.Size(124, 26);
            // 
            // CloseTabMenuItem
            // 
            this.CloseTabMenuItem.Name = "CloseTabMenuItem";
            this.CloseTabMenuItem.Size = new System.Drawing.Size(123, 22);
            this.CloseTabMenuItem.Text = "Close tab";
            this.CloseTabMenuItem.Click += new System.EventHandler(this.OnCloseTabClick);
            // 
            // TabImageList
            // 
            this.TabImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("TabImageList.ImageStream")));
            this.TabImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.TabImageList.Images.SetKeyName(0, "application.png");
            // 
            // OpenFileDialog
            // 
            this.OpenFileDialog.DefaultExt = "apsimx";
            this.OpenFileDialog.Filter = "ApsimX files|*.apsimx";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.LightGoldenrodYellow;
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(594, 20);
            this.panel1.TabIndex = 10;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Recent files";
            // 
            // listBox1
            // 
            this.listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.IntegralHeight = false;
            this.listBox1.ItemHeight = 16;
            this.listBox1.Location = new System.Drawing.Point(0, 20);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(594, 109);
            this.listBox1.TabIndex = 11;
            this.toolTip1.SetToolTip(this.listBox1, "Double click to open a recent file");
            this.listBox1.DoubleClick += new System.EventHandler(this.listBox1_DoubleClick);
            // 
            // TabbedExplorerView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.TabControl);
            this.Name = "TabbedExplorerView";
            this.Size = new System.Drawing.Size(610, 523);
            this.Load += new System.EventHandler(this.OnLoad);
            this.TabControl.ResumeLayout(false);
            this.StartPage.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.TabPopupMenu.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl TabControl;
        private System.Windows.Forms.TabPage StartPage;
        private System.Windows.Forms.ImageList TabImageList;
        private System.Windows.Forms.ImageList ListViewImages;
        private System.Windows.Forms.OpenFileDialog OpenFileDialog;
        private System.Windows.Forms.ContextMenuStrip TabPopupMenu;
        private System.Windows.Forms.ToolStripMenuItem CloseTabMenuItem;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListView ListView;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ListBox listBox1;
    }
}
