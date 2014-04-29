namespace UserInterface.Views
{
    partial class LegendView
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
            this.label1 = new System.Windows.Forms.Label();
            this.LegendPositionCombo = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Position:";
            // 
            // LegendPositionCombo
            // 
            this.LegendPositionCombo.FormattingEnabled = true;
            this.LegendPositionCombo.Location = new System.Drawing.Point(73, 16);
            this.LegendPositionCombo.Name = "LegendPositionCombo";
            this.LegendPositionCombo.Size = new System.Drawing.Size(179, 21);
            this.LegendPositionCombo.TabIndex = 3;
            this.LegendPositionCombo.TextChanged += new System.EventHandler(this.OnPositionComboChanged);
            this.LegendPositionCombo.Enter += new System.EventHandler(this.OnTitleTextBoxEnter);
            // 
            // LegendView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.LegendPositionCombo);
            this.Controls.Add(this.label1);
            this.Name = "LegendView";
            this.Size = new System.Drawing.Size(264, 98);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox LegendPositionCombo;
    }
}
