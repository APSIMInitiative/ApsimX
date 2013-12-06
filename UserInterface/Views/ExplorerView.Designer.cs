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
            this.StatusWindow = new System.Windows.Forms.RichTextBox();
            this.StatusWindowPopup = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStrip = new System.Windows.Forms.ToolStrip();
            this.SaveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.splitter2 = new System.Windows.Forms.Splitter();
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
            this.Panel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Panel.Location = new System.Drawing.Point(0, 0);
            this.Panel.Name = "Panel";
            this.Panel.Size = new System.Drawing.Size(600, 600);
            this.Panel.TabIndex = 5;
            // 
            // RightHandPanel
            // 
            this.RightHandPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RightHandPanel.Location = new System.Drawing.Point(203, 0);
            this.RightHandPanel.Name = "RightHandPanel";
            this.RightHandPanel.Size = new System.Drawing.Size(397, 501);
            this.RightHandPanel.TabIndex = 10;
            // 
            // splitter1
            // 
            this.splitter1.BackColor = System.Drawing.SystemColors.Control;
            this.splitter1.Cursor = System.Windows.Forms.Cursors.VSplit;
            this.splitter1.Location = new System.Drawing.Point(197, 0);
            this.splitter1.MinExtra = 0;
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(6, 501);
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
            this.TreeView.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TreeView.HideSelection = false;
            this.TreeView.ImageIndex = 0;
            this.TreeView.ImageList = this.TreeImageList;
            this.TreeView.LabelEdit = true;
            this.TreeView.Location = new System.Drawing.Point(0, 0);
            this.TreeView.Name = "TreeView";
            this.TreeView.SelectedImageIndex = 0;
            this.TreeView.Size = new System.Drawing.Size(197, 501);
            this.TreeView.TabIndex = 8;
            this.TreeView.BeforeLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.OnBeforeLabelEdit);
            this.TreeView.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.OnAfterLabelEdit);
            this.TreeView.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.OnBeforeExpand);
            this.TreeView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.OnNodeDrag);
            this.TreeView.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this.OnTreeViewBeforeSelect);
            this.TreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TreeView_AfterSelect);
            this.TreeView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.TreeViewNodeMouseClick);
            this.TreeView.DragDrop += new System.Windows.Forms.DragEventHandler(this.OnDragDrop);
            this.TreeView.DragOver += new System.Windows.Forms.DragEventHandler(this.OnDragOver);
            // 
            // PopupMenu
            // 
            this.PopupMenu.Name = "ContextMenu";
            this.PopupMenu.Size = new System.Drawing.Size(61, 4);
            this.PopupMenu.Opening += new System.ComponentModel.CancelEventHandler(this.OnPopupMenuOpening);
            // 
            // TreeImageList
            // 
            this.TreeImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.TreeImageList.ImageSize = new System.Drawing.Size(16, 16);
            this.TreeImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // StatusWindow
            // 
            this.StatusWindow.BackColor = System.Drawing.SystemColors.Info;
            this.StatusWindow.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.StatusWindow.ContextMenuStrip = this.StatusWindowPopup;
            this.StatusWindow.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.StatusWindow.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.StatusWindow.Location = new System.Drawing.Point(0, 504);
            this.StatusWindow.Name = "StatusWindow";
            this.StatusWindow.ReadOnly = true;
            this.StatusWindow.Size = new System.Drawing.Size(600, 96);
            this.StatusWindow.TabIndex = 0;
            this.StatusWindow.Text = "";
            // 
            // StatusWindowPopup
            // 
            this.StatusWindowPopup.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.closeToolStripMenuItem});
            this.StatusWindowPopup.Name = "StatusWindowPopup";
            this.StatusWindowPopup.Size = new System.Drawing.Size(104, 26);
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.closeToolStripMenuItem.Text = "Close";
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.OnCloseStatusWindowClick);
            // 
            // ToolStrip
            // 
            this.ToolStrip.Location = new System.Drawing.Point(0, 0);
            this.ToolStrip.Name = "ToolStrip";
            this.ToolStrip.Size = new System.Drawing.Size(600, 25);
            this.ToolStrip.TabIndex = 11;
            this.ToolStrip.Text = "toolStrip1";
            this.ToolStrip.Visible = false;
            // 
            // SaveFileDialog
            // 
            this.SaveFileDialog.DefaultExt = "apsimx";
            this.SaveFileDialog.Filter = "*.apsimx|*.apsimx";
            // 
            // splitter2
            // 
            this.splitter2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitter2.Location = new System.Drawing.Point(0, 501);
            this.splitter2.Name = "splitter2";
            this.splitter2.Size = new System.Drawing.Size(600, 3);
            this.splitter2.TabIndex = 11;
            this.splitter2.TabStop = false;
            // 
            // ExplorerView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this.Panel);
            this.Controls.Add(this.ToolStrip);
            this.Name = "ExplorerView";
            this.Size = new System.Drawing.Size(600, 600);
            this.Load += new System.EventHandler(this.OnLoad);
            this.Panel.ResumeLayout(false);
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
        private System.Windows.Forms.SaveFileDialog SaveFileDialog;
        private System.Windows.Forms.RichTextBox StatusWindow;
        private System.Windows.Forms.ContextMenuStrip StatusWindowPopup;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
        private System.Windows.Forms.Splitter splitter2;



    }
}
