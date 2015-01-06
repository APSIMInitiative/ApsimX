namespace UserInterface.Views
{
    partial class InputView
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
            this.Grid = new GridView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.FileNameLabel = new System.Windows.Forms.Label();
            this.BrowseButton = new System.Windows.Forms.Button();
            this.OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.warningText = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Grid
            // 
            this.Grid.AutoFilterOn = false;
            this.Grid.DataSource = null;
            this.Grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Grid.Location = new System.Drawing.Point(0, 32);
            this.Grid.Name = "Grid";
            this.Grid.ReadOnly = false;
            this.Grid.RowCount = 0;
            this.Grid.Size = new System.Drawing.Size(644, 476);
            this.Grid.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.FileNameLabel);
            this.panel1.Controls.Add(this.BrowseButton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(644, 32);
            this.panel1.TabIndex = 1;
            // 
            // FileNameLabel
            // 
            this.FileNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FileNameLabel.Location = new System.Drawing.Point(73, 9);
            this.FileNameLabel.Name = "FileNameLabel";
            this.FileNameLabel.Size = new System.Drawing.Size(469, 14);
            this.FileNameLabel.TabIndex = 3;
            this.FileNameLabel.Text = "File name";
            // 
            // BrowseButton
            // 
            this.BrowseButton.Location = new System.Drawing.Point(7, 4);
            this.BrowseButton.Name = "BrowseButton";
            this.BrowseButton.Size = new System.Drawing.Size(60, 23);
            this.BrowseButton.TabIndex = 2;
            this.BrowseButton.Text = "Browse...";
            this.BrowseButton.UseVisualStyleBackColor = true;
            this.BrowseButton.Click += new System.EventHandler(this.OnBrowseButtonClick);
            // 
            // OpenFileDialog
            // 
            this.OpenFileDialog.Filter = "All files|*.*";
            this.OpenFileDialog.Multiselect = true;
            // 
            // warningText
            // 
            this.warningText.AutoSize = true;
            this.warningText.ForeColor = System.Drawing.Color.Red;
            this.warningText.Location = new System.Drawing.Point(3, 35);
            this.warningText.Name = "warningText";
            this.warningText.Size = new System.Drawing.Size(35, 13);
            this.warningText.TabIndex = 2;
            this.warningText.Text = "label1";
            this.warningText.Visible = false;
            // 
            // InputView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.warningText);
            this.Controls.Add(this.Grid);
            this.Controls.Add(this.panel1);
            this.Name = "InputView";
            this.Size = new System.Drawing.Size(644, 508);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private GridView Grid;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label FileNameLabel;
        private System.Windows.Forms.Button BrowseButton;
        private System.Windows.Forms.OpenFileDialog OpenFileDialog;
        private System.Windows.Forms.Label warningText;
    }
}
