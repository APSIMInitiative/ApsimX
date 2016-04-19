namespace UserInterface.Views
{
    partial class MainView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainView));
            this.ListViewImages = new System.Windows.Forms.ImageList(this.components);
            this.tabPopupMenu1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CloseTabMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.TabImageList = new System.Windows.Forms.ImageList(this.components);
            this.OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.SaveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.startPage1 = new System.Windows.Forms.TabPage();
            this.listButtonView1 = new Views.ListButtonView();
            this.tabControl2 = new System.Windows.Forms.TabControl();
            this.tabPopupMenu2 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.startPage2 = new System.Windows.Forms.TabPage();
            this.listButtonView2 = new Views.ListButtonView();
            this.statusPanel = new System.Windows.Forms.Panel();
            this.StatusWindow = new System.Windows.Forms.TextBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.tabPopupMenu1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.startPage1.SuspendLayout();
            this.tabControl2.SuspendLayout();
            this.tabPopupMenu2.SuspendLayout();
            this.startPage2.SuspendLayout();
            this.statusPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // ListViewImages
            // 
            this.ListViewImages.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.ListViewImages.ImageSize = new System.Drawing.Size(16, 16);
            this.ListViewImages.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // tabPopupMenu1
            // 
            this.tabPopupMenu1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CloseTabMenuItem});
            this.tabPopupMenu1.Name = "tabPopupMenu1";
            this.tabPopupMenu1.Size = new System.Drawing.Size(124, 26);
            this.tabPopupMenu1.Opening += new System.ComponentModel.CancelEventHandler(this.OnPopupMenuOpening);
            // 
            // CloseTabMenuItem
            // 
            this.CloseTabMenuItem.Name = "CloseTabMenuItem";
            this.CloseTabMenuItem.Size = new System.Drawing.Size(123, 22);
            this.CloseTabMenuItem.Text = "Close tab";
            this.CloseTabMenuItem.Click += new System.EventHandler(this.OnCloseTabClick1);
            // 
            // TabImageList
            // 
            this.TabImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.TabImageList.ImageSize = new System.Drawing.Size(16, 16);
            this.TabImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // OpenFileDialog
            // 
            this.OpenFileDialog.DefaultExt = "apsimx";
            this.OpenFileDialog.Filter = "ApsimX files|*.apsimx";
            // 
            // SaveFileDialog
            // 
            this.SaveFileDialog.DefaultExt = "apsimx";
            this.SaveFileDialog.Filter = "*.apsimx|*.apsimx";
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 0);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.tabControl1);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.tabControl2);
            this.splitContainer.Panel2Collapsed = true;
            this.splitContainer.Size = new System.Drawing.Size(769, 464);
            this.splitContainer.SplitterDistance = 383;
            this.splitContainer.TabIndex = 1;
            // 
            // tabControl1
            // 
            this.tabControl1.ContextMenuStrip = this.tabPopupMenu1;
            this.tabControl1.Controls.Add(this.startPage1);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(4);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(769, 464);
            this.tabControl1.TabIndex = 1;
            this.tabControl1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnTabControlMouseDown);
            // 
            // startPage1
            // 
            this.startPage1.Controls.Add(this.listButtonView1);
            this.startPage1.Location = new System.Drawing.Point(4, 25);
            this.startPage1.Margin = new System.Windows.Forms.Padding(4);
            this.startPage1.Name = "startPage1";
            this.startPage1.Padding = new System.Windows.Forms.Padding(4);
            this.startPage1.Size = new System.Drawing.Size(761, 435);
            this.startPage1.TabIndex = 0;
            this.startPage1.Text = "+";
            this.startPage1.UseVisualStyleBackColor = true;
            // 
            // listButtonView1
            // 
            this.listButtonView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listButtonView1.Location = new System.Drawing.Point(4, 4);
            this.listButtonView1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.listButtonView1.Name = "listButtonView1";
            this.listButtonView1.Size = new System.Drawing.Size(753, 427);
            this.listButtonView1.TabIndex = 0;
            // 
            // tabControl2
            // 
            this.tabControl2.ContextMenuStrip = this.tabPopupMenu2;
            this.tabControl2.Controls.Add(this.startPage2);
            this.tabControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl2.Location = new System.Drawing.Point(0, 0);
            this.tabControl2.Margin = new System.Windows.Forms.Padding(4);
            this.tabControl2.Name = "tabControl2";
            this.tabControl2.SelectedIndex = 0;
            this.tabControl2.Size = new System.Drawing.Size(96, 100);
            this.tabControl2.TabIndex = 1;
            this.tabControl2.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnTabControlMouseDown);
            // 
            // tabPopupMenu2
            // 
            this.tabPopupMenu2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1});
            this.tabPopupMenu2.Name = "tabPopupMenu2";
            this.tabPopupMenu2.Size = new System.Drawing.Size(124, 26);
            this.tabPopupMenu2.Opening += new System.ComponentModel.CancelEventHandler(this.OnPopupMenuOpening);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(123, 22);
            this.toolStripMenuItem1.Text = "Close tab";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.OnCloseTabClick2);
            // 
            // startPage2
            // 
            this.startPage2.Controls.Add(this.listButtonView2);
            this.startPage2.Location = new System.Drawing.Point(4, 25);
            this.startPage2.Margin = new System.Windows.Forms.Padding(4);
            this.startPage2.Name = "startPage2";
            this.startPage2.Padding = new System.Windows.Forms.Padding(4);
            this.startPage2.Size = new System.Drawing.Size(88, 71);
            this.startPage2.TabIndex = 0;
            this.startPage2.Text = "+";
            this.startPage2.UseVisualStyleBackColor = true;
            // 
            // listButtonView2
            // 
            this.listButtonView2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listButtonView2.Location = new System.Drawing.Point(4, 4);
            this.listButtonView2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.listButtonView2.Name = "listButtonView2";
            this.listButtonView2.Size = new System.Drawing.Size(80, 63);
            this.listButtonView2.TabIndex = 0;
            // 
            // statusPanel
            // 
            this.statusPanel.Controls.Add(this.StatusWindow);
            this.statusPanel.Controls.Add(this.progressBar);
            this.statusPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusPanel.Location = new System.Drawing.Point(0, 444);
            this.statusPanel.Margin = new System.Windows.Forms.Padding(2);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Size = new System.Drawing.Size(769, 20);
            this.statusPanel.TabIndex = 15;
            // 
            // StatusWindow
            // 
            this.StatusWindow.BackColor = System.Drawing.SystemColors.Info;
            this.StatusWindow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StatusWindow.Location = new System.Drawing.Point(92, 0);
            this.StatusWindow.Margin = new System.Windows.Forms.Padding(2);
            this.StatusWindow.Multiline = true;
            this.StatusWindow.Name = "StatusWindow";
            this.StatusWindow.ReadOnly = true;
            this.StatusWindow.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.StatusWindow.Size = new System.Drawing.Size(677, 20);
            this.StatusWindow.TabIndex = 13;
            // 
            // progressBar
            // 
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Left;
            this.progressBar.Location = new System.Drawing.Point(0, 0);
            this.progressBar.Margin = new System.Windows.Forms.Padding(2);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(92, 20);
            this.progressBar.TabIndex = 14;
            this.progressBar.Visible = false;
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitter1.Location = new System.Drawing.Point(0, 441);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(769, 3);
            this.splitter1.TabIndex = 16;
            this.splitter1.TabStop = false;
            // 
            // MainView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(769, 464);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.statusPanel);
            this.Controls.Add(this.splitContainer);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainView";
            this.tabPopupMenu1.ResumeLayout(false);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.startPage1.ResumeLayout(false);
            this.tabControl2.ResumeLayout(false);
            this.tabPopupMenu2.ResumeLayout(false);
            this.startPage2.ResumeLayout(false);
            this.statusPanel.ResumeLayout(false);
            this.statusPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ImageList TabImageList;
        private System.Windows.Forms.ImageList ListViewImages;
        private System.Windows.Forms.OpenFileDialog OpenFileDialog;
        private System.Windows.Forms.ContextMenuStrip tabPopupMenu1;
        private System.Windows.Forms.ToolStripMenuItem CloseTabMenuItem;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.SaveFileDialog SaveFileDialog;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage startPage1;
        private Views.ListButtonView listButtonView1;
        private System.Windows.Forms.TabControl tabControl2;
        private System.Windows.Forms.TabPage startPage2;
        private Views.ListButtonView listButtonView2;
        private System.Windows.Forms.Panel statusPanel;
        private System.Windows.Forms.TextBox StatusWindow;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.ContextMenuStrip tabPopupMenu2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
    }
}
