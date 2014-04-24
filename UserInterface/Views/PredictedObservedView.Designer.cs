namespace UserInterface.Views
{
    partial class PredictedObservedView
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
            this.ObservedCombo = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.PredictedCombo = new System.Windows.Forms.ComboBox();
            this.FileNameLabel = new System.Windows.Forms.Label();
            this.OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Grid
            // 
            this.Grid.AutoFilterOn = false;
            this.Grid.DataSource = null;
            this.Grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Grid.Location = new System.Drawing.Point(0, 85);
            this.Grid.Name = "Grid";
            this.Grid.ReadOnly = false;
            this.Grid.RowCount = 0;
            this.Grid.Size = new System.Drawing.Size(644, 423);
            this.Grid.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.ObservedCombo);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.PredictedCombo);
            this.panel1.Controls.Add(this.FileNameLabel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(644, 85);
            this.panel1.TabIndex = 1;
            // 
            // ObservedCombo
            // 
            this.ObservedCombo.FormattingEnabled = true;
            this.ObservedCombo.Location = new System.Drawing.Point(106, 48);
            this.ObservedCombo.Name = "ObservedCombo";
            this.ObservedCombo.Size = new System.Drawing.Size(210, 21);
            this.ObservedCombo.TabIndex = 6;
            this.ObservedCombo.TextChanged += new System.EventHandler(this.OnObservedComboTextChanged);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(3, 48);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 21);
            this.label1.TabIndex = 5;
            this.label1.Text = "Observed table:";
            // 
            // PredictedCombo
            // 
            this.PredictedCombo.FormattingEnabled = true;
            this.PredictedCombo.Location = new System.Drawing.Point(106, 11);
            this.PredictedCombo.Name = "PredictedCombo";
            this.PredictedCombo.Size = new System.Drawing.Size(210, 21);
            this.PredictedCombo.TabIndex = 4;
            this.PredictedCombo.TextChanged += new System.EventHandler(this.OnPredictedComboTextChanged);
            // 
            // FileNameLabel
            // 
            this.FileNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FileNameLabel.Location = new System.Drawing.Point(3, 11);
            this.FileNameLabel.Name = "FileNameLabel";
            this.FileNameLabel.Size = new System.Drawing.Size(86, 21);
            this.FileNameLabel.TabIndex = 3;
            this.FileNameLabel.Text = "Predicted table:";
            // 
            // OpenFileDialog
            // 
            this.OpenFileDialog.Filter = "All files|*.*";
            // 
            // PredictedObservedView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Grid);
            this.Controls.Add(this.panel1);
            this.Name = "PredictedObservedView";
            this.Size = new System.Drawing.Size(644, 508);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private GridView Grid;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label FileNameLabel;
        private System.Windows.Forms.OpenFileDialog OpenFileDialog;
        private System.Windows.Forms.ComboBox ObservedCombo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox PredictedCombo;
    }
}
