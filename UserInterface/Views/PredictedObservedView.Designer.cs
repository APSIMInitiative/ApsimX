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
            this.OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.ObservedCombo = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.PredictedCombo = new System.Windows.Forms.ComboBox();
            this.FileNameLabel = new System.Windows.Forms.Label();
            this.ColumnNameCombo = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // OpenFileDialog
            // 
            this.OpenFileDialog.Filter = "All files|*.*";
            // 
            // ObservedCombo
            // 
            this.ObservedCombo.FormattingEnabled = true;
            this.ObservedCombo.Location = new System.Drawing.Point(117, 51);
            this.ObservedCombo.Name = "ObservedCombo";
            this.ObservedCombo.Size = new System.Drawing.Size(210, 21);
            this.ObservedCombo.TabIndex = 10;
            this.ObservedCombo.TextChanged += new System.EventHandler(this.OnObservedComboTextChanged);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(14, 51);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 21);
            this.label1.TabIndex = 9;
            this.label1.Text = "Observed table:";
            // 
            // PredictedCombo
            // 
            this.PredictedCombo.FormattingEnabled = true;
            this.PredictedCombo.Location = new System.Drawing.Point(117, 14);
            this.PredictedCombo.Name = "PredictedCombo";
            this.PredictedCombo.Size = new System.Drawing.Size(210, 21);
            this.PredictedCombo.TabIndex = 8;
            this.PredictedCombo.TextChanged += new System.EventHandler(this.OnPredictedComboTextChanged);
            // 
            // FileNameLabel
            // 
            this.FileNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FileNameLabel.Location = new System.Drawing.Point(14, 14);
            this.FileNameLabel.Name = "FileNameLabel";
            this.FileNameLabel.Size = new System.Drawing.Size(86, 21);
            this.FileNameLabel.TabIndex = 7;
            this.FileNameLabel.Text = "Predicted table:";
            // 
            // ColumnNameCombo
            // 
            this.ColumnNameCombo.FormattingEnabled = true;
            this.ColumnNameCombo.Location = new System.Drawing.Point(117, 89);
            this.ColumnNameCombo.Name = "ColumnNameCombo";
            this.ColumnNameCombo.Size = new System.Drawing.Size(210, 21);
            this.ColumnNameCombo.TabIndex = 12;
            this.ColumnNameCombo.TextChanged += new System.EventHandler(this.ColumnNameCombo_TextChanged);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Location = new System.Drawing.Point(14, 89);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(86, 21);
            this.label2.TabIndex = 11;
            this.label2.Text = "Column name:";
            // 
            // PredictedObservedView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ColumnNameCombo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.ObservedCombo);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.PredictedCombo);
            this.Controls.Add(this.FileNameLabel);
            this.Name = "PredictedObservedView";
            this.Size = new System.Drawing.Size(644, 508);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog OpenFileDialog;
        private System.Windows.Forms.ComboBox ObservedCombo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox PredictedCombo;
        private System.Windows.Forms.Label FileNameLabel;
        private System.Windows.Forms.ComboBox ColumnNameCombo;
        private System.Windows.Forms.Label label2;
    }
}
