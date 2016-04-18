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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.StartPage = new System.Windows.Forms.TabPage();
            this.listButtonView1 = new Views.ListButtonView();
            this.ListViewImages = new System.Windows.Forms.ImageList(this.components);
            this.TabPopupMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CloseTabMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.TabImageList = new System.Windows.Forms.ImageList(this.components);
            this.OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.SaveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.tabControl.SuspendLayout();
            this.StartPage.SuspendLayout();
            this.TabPopupMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.StartPage);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(610, 523);
            this.tabControl.TabIndex = 0;
            this.tabControl.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OnTabControlMouseUp);
            // 
            // StartPage
            // 
            this.StartPage.Controls.Add(this.listButtonView1);
            this.StartPage.Location = new System.Drawing.Point(4, 22);
            this.StartPage.Name = "StartPage";
            this.StartPage.Padding = new System.Windows.Forms.Padding(3);
            this.StartPage.Size = new System.Drawing.Size(602, 497);
            this.StartPage.TabIndex = 0;
            this.StartPage.Text = "+";
            this.StartPage.UseVisualStyleBackColor = true;
            // 
            // listButtonView1
            // 
            this.listButtonView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listButtonView1.Location = new System.Drawing.Point(3, 3);
            this.listButtonView1.Margin = new System.Windows.Forms.Padding(2);
            this.listButtonView1.Name = "listButtonView1";
            this.listButtonView1.Size = new System.Drawing.Size(596, 491);
            this.listButtonView1.TabIndex = 0;
            // 
            // ListViewImages
            // 
            this.ListViewImages.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.ListViewImages.ImageSize = new System.Drawing.Size(16, 16);
            this.ListViewImages.TransparentColor = System.Drawing.Color.Transparent;
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
            // TabbedExplorerView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl);
            this.Name = "TabbedExplorerView";
            this.Size = new System.Drawing.Size(610, 523);
            this.tabControl.ResumeLayout(false);
            this.StartPage.ResumeLayout(false);
            this.TabPopupMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage StartPage;
        private System.Windows.Forms.ImageList TabImageList;
        private System.Windows.Forms.ImageList ListViewImages;
        private System.Windows.Forms.OpenFileDialog OpenFileDialog;
        private System.Windows.Forms.ContextMenuStrip TabPopupMenu;
        private System.Windows.Forms.ToolStripMenuItem CloseTabMenuItem;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.SaveFileDialog SaveFileDialog;
        private Views.ListButtonView listButtonView1;
    }
}
