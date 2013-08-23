namespace UserInterface
{
    partial class StartPageView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StartPageView));
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem("Open a simulation in a new tab...", 0);
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem("Open standard toolbox", 1);
            System.Windows.Forms.ListViewItem listViewItem3 = new System.Windows.Forms.ListViewItem("Open graph toolbox", 2);
            System.Windows.Forms.ListViewItem listViewItem4 = new System.Windows.Forms.ListViewItem("Open management toolbox", 3);
            this.ListViewImages = new System.Windows.Forms.ImageList(this.components);
            this.ListView = new System.Windows.Forms.ListView();
            this.OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            // 
            // ListViewImages
            // 
            this.ListViewImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ListViewImages.ImageStream")));
            this.ListViewImages.TransparentColor = System.Drawing.Color.Transparent;
            this.ListViewImages.Images.SetKeyName(0, "open_file-icon.gif");
            this.ListViewImages.Images.SetKeyName(1, "chest.png");
            this.ListViewImages.Images.SetKeyName(2, "chart.png");
            this.ListViewImages.Images.SetKeyName(3, "user1.png");
            // 
            // ListView
            // 
            this.ListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ListView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ListView.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ListView.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2,
            listViewItem3,
            listViewItem4});
            this.ListView.LargeImageList = this.ListViewImages;
            this.ListView.Location = new System.Drawing.Point(12, 13);
            this.ListView.MultiSelect = false;
            this.ListView.Name = "ListView";
            this.ListView.Size = new System.Drawing.Size(599, 515);
            this.ListView.TabIndex = 1;
            this.ListView.TileSize = new System.Drawing.Size(400, 100);
            this.ListView.UseCompatibleStateImageBehavior = false;
            this.ListView.SelectedIndexChanged += new System.EventHandler(this.ListView_SelectedIndexChanged);
            // 
            // OpenFileDialog
            // 
            this.OpenFileDialog.DefaultExt = "apsimx";
            this.OpenFileDialog.Filter = "ApsimX files|*.apsimx|All files|*.*";
            this.OpenFileDialog.RestoreDirectory = true;
            this.OpenFileDialog.Title = "Open file...";
            // 
            // StartPageView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this.ListView);
            this.Name = "StartPageView";
            this.Size = new System.Drawing.Size(624, 544);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ImageList ListViewImages;
        private System.Windows.Forms.ListView ListView;
        private System.Windows.Forms.OpenFileDialog OpenFileDialog;
    }
}
