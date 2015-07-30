namespace UserInterface.Views
{
    partial class ExplorerView
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
            this.Panel = new System.Windows.Forms.Panel();
            this.RightHandPanel = new System.Windows.Forms.Panel();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.TreeView = new System.Windows.Forms.TreeView();
            this.PopupMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.TreeImageList = new System.Windows.Forms.ImageList(this.components);
            this.splitter2 = new System.Windows.Forms.Splitter();
            this.StatusWindow = new System.Windows.Forms.TextBox();
            this.StatusWindowPopup = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStrip = new System.Windows.Forms.ToolStrip();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.Panel.SuspendLayout();
            this.StatusWindowPopup.SuspendLayout();
            this.SuspendLayout();
            // 
            // Panel
            // 
            this.Panel.BackColor = System.Drawing.SystemColors.Window;
            this.Panel.Controls.Add(this.RightHandPanel);
            this.Panel.Controls.Add(this.splitter1);
            this.Panel.Controls.Add(this.TreeView);
            this.Panel.Controls.Add(this.splitter2);
            this.Panel.Controls.Add(this.StatusWindow);
            this.Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Panel.Location = new System.Drawing.Point(0, 0);
            this.Panel.Margin = new System.Windows.Forms.Padding(4);
            this.Panel.Name = "Panel";
            this.Panel.Size = new System.Drawing.Size(757, 697);
            this.Panel.TabIndex = 5;
            // 
            // RightHandPanel
            // 
            this.RightHandPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RightHandPanel.Location = new System.Drawing.Point(271, 0);
            this.RightHandPanel.Margin = new System.Windows.Forms.Padding(4);
            this.RightHandPanel.Name = "RightHandPanel";
            this.RightHandPanel.Size = new System.Drawing.Size(486, 668);
            this.RightHandPanel.TabIndex = 10;
            // 
            // splitter1
            // 
            this.splitter1.BackColor = System.Drawing.SystemColors.Control;
            this.splitter1.Cursor = System.Windows.Forms.Cursors.VSplit;
            this.splitter1.Location = new System.Drawing.Point(263, 0);
            this.splitter1.Margin = new System.Windows.Forms.Padding(4);
            this.splitter1.MinExtra = 0;
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(8, 668);
            this.splitter1.TabIndex = 9;
            this.splitter1.TabStop = false;
            this.splitter1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.OnSplitterMoved);
            // 
            // TreeView
            // 
            this.TreeView.AllowDrop = true;
            this.TreeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TreeView.ContextMenuStrip = this.PopupMenu;
            this.TreeView.Dock = System.Windows.Forms.DockStyle.Left;
            this.TreeView.HideSelection = false;
            this.TreeView.ImageIndex = 0;
            this.TreeView.ImageList = this.TreeImageList;
            this.TreeView.LabelEdit = true;
            this.TreeView.Location = new System.Drawing.Point(0, 0);
            this.TreeView.Margin = new System.Windows.Forms.Padding(4);
            this.TreeView.Name = "TreeView";
            this.TreeView.SelectedImageIndex = 0;
            this.TreeView.Size = new System.Drawing.Size(263, 668);
            this.TreeView.TabIndex = 8;
            this.TreeView.BeforeLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.OnBeforeLabelEdit);
            this.TreeView.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.OnAfterLabelEdit);
            this.TreeView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.OnNodeDrag);
            this.TreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.OnAfterSelect);
            this.TreeView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.TreeView_NodeMouseClick);
            this.TreeView.DragDrop += new System.Windows.Forms.DragEventHandler(this.OnDragDrop);
            this.TreeView.DragOver += new System.Windows.Forms.DragEventHandler(this.OnDragOver);
            // 
            // PopupMenu
            // 
            this.PopupMenu.Name = "ContextMenu";
            this.PopupMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // TreeImageList
            // 
            this.TreeImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.TreeImageList.ImageSize = new System.Drawing.Size(16, 16);
            this.TreeImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // splitter2
            // 
            this.splitter2.BackColor = System.Drawing.SystemColors.Control;
            this.splitter2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitter2.Location = new System.Drawing.Point(0, 668);
            this.splitter2.Margin = new System.Windows.Forms.Padding(4);
            this.splitter2.Name = "splitter2";
            this.splitter2.Size = new System.Drawing.Size(757, 7);
            this.splitter2.TabIndex = 11;
            this.splitter2.TabStop = false;
            // 
            // StatusWindow
            // 
            this.StatusWindow.BackColor = System.Drawing.SystemColors.Info;
            this.StatusWindow.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.StatusWindow.Location = new System.Drawing.Point(0, 675);
            this.StatusWindow.Multiline = true;
            this.StatusWindow.Name = "StatusWindow";
            this.StatusWindow.ReadOnly = true;
            this.StatusWindow.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.StatusWindow.Size = new System.Drawing.Size(757, 22);
            this.StatusWindow.TabIndex = 13;
            // 
            // StatusWindowPopup
            // 
            this.StatusWindowPopup.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.closeToolStripMenuItem,
            this.clearToolStripMenuItem});
            this.StatusWindowPopup.Name = "StatusWindowPopup";
            this.StatusWindowPopup.Size = new System.Drawing.Size(115, 52);
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(114, 24);
            this.closeToolStripMenuItem.Text = "Close";
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.OnCloseStatusWindowClick);
            // 
            // clearToolStripMenuItem
            // 
            this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            this.clearToolStripMenuItem.Size = new System.Drawing.Size(114, 24);
            this.clearToolStripMenuItem.Text = "Clear";
            // 
            // ToolStrip
            // 
            this.ToolStrip.Location = new System.Drawing.Point(0, 0);
            this.ToolStrip.Name = "ToolStrip";
            this.ToolStrip.Size = new System.Drawing.Size(757, 25);
            this.ToolStrip.TabIndex = 11;
            this.ToolStrip.Text = "toolStrip1";
            this.ToolStrip.Visible = false;
            // 
            // toolTip1
            // 
            this.toolTip1.AutoPopDelay = 10000;
            this.toolTip1.InitialDelay = 500;
            this.toolTip1.ReshowDelay = 100;
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = "All files|*.*";
            // 
            // ExplorerView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this.Panel);
            this.Controls.Add(this.ToolStrip);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "ExplorerView";
            this.Size = new System.Drawing.Size(757, 697);
            this.Panel.ResumeLayout(false);
            this.Panel.PerformLayout();
            this.StatusWindowPopup.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel Panel;
        private System.Windows.Forms.ImageList TreeImageList;
        private System.Windows.Forms.Panel RightHandPanel;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.TreeView TreeView;
        private System.Windows.Forms.ContextMenuStrip PopupMenu;
        private System.Windows.Forms.ToolStrip ToolStrip;
        private System.Windows.Forms.ContextMenuStrip StatusWindowPopup;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
        private System.Windows.Forms.Splitter splitter2;
        private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.TextBox StatusWindow;
        private System.Windows.Forms.OpenFileDialog openFileDialog;



    }
}
