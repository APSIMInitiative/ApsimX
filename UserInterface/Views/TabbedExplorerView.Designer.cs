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
            this.listViewMain = new System.Windows.Forms.ListView();
            this.ListViewImages = new System.Windows.Forms.ImageList(this.components);
            this.TabPopupMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CloseTabMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.TabImageList = new System.Windows.Forms.ImageList(this.components);
            this.OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.TabControl.SuspendLayout();
            this.StartPage.SuspendLayout();
            this.TabPopupMenu.SuspendLayout();
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
            this.TabControl.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.TabControl_Selecting);
            this.TabControl.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OnTabControlMouseUp);
            // 
            // StartPage
            // 
            this.StartPage.Controls.Add(this.listViewMain);
            this.StartPage.Location = new System.Drawing.Point(4, 22);
            this.StartPage.Name = "StartPage";
            this.StartPage.Padding = new System.Windows.Forms.Padding(3);
            this.StartPage.Size = new System.Drawing.Size(602, 497);
            this.StartPage.TabIndex = 0;
            this.StartPage.Text = " ";
            this.StartPage.UseVisualStyleBackColor = true;
            // 
            // listViewMain
            // 
            this.listViewMain.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listViewMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewMain.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listViewMain.LargeImageList = this.ListViewImages;
            this.listViewMain.Location = new System.Drawing.Point(3, 3);
            this.listViewMain.MultiSelect = false;
            this.listViewMain.Name = "listViewMain";
            this.listViewMain.ShowItemToolTips = true;
            this.listViewMain.Size = new System.Drawing.Size(596, 491);
            this.listViewMain.TabIndex = 4;
            this.listViewMain.TileSize = new System.Drawing.Size(400, 100);
            this.listViewMain.UseCompatibleStateImageBehavior = false;
            this.listViewMain.DoubleClick += new System.EventHandler(this.ListView_DoubleClick);
            this.listViewMain.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ListView_KeyUp);
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
            this.TabPopupMenu.ResumeLayout(false);
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
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ListView listViewMain;
    }
}
