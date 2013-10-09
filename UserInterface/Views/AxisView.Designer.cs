namespace UserInterface.Views
{
    partial class AxisView
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
            this.InvertedCheckBox = new System.Windows.Forms.CheckBox();
            this.TitleTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // InvertedCheckBox
            // 
            this.InvertedCheckBox.AutoSize = true;
            this.InvertedCheckBox.Location = new System.Drawing.Point(19, 54);
            this.InvertedCheckBox.Name = "InvertedCheckBox";
            this.InvertedCheckBox.Size = new System.Drawing.Size(65, 17);
            this.InvertedCheckBox.TabIndex = 0;
            this.InvertedCheckBox.Text = "Inverted";
            this.InvertedCheckBox.UseVisualStyleBackColor = true;
            this.InvertedCheckBox.CheckedChanged += new System.EventHandler(this.OnCheckedChanged);
            // 
            // TitleTextBox
            // 
            this.TitleTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TitleTextBox.Location = new System.Drawing.Point(68, 13);
            this.TitleTextBox.Name = "TitleTextBox";
            this.TitleTextBox.Size = new System.Drawing.Size(151, 20);
            this.TitleTextBox.TabIndex = 1;
            this.TitleTextBox.Enter += new System.EventHandler(this.OnTitleTextBoxEnter);
            this.TitleTextBox.Leave += new System.EventHandler(this.OnTitleTextBoxLeave);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(30, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Title:";
            // 
            // AxisView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.TitleTextBox);
            this.Controls.Add(this.InvertedCheckBox);
            this.Name = "AxisView";
            this.Size = new System.Drawing.Size(264, 98);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox InvertedCheckBox;
        private System.Windows.Forms.TextBox TitleTextBox;
        private System.Windows.Forms.Label label1;
    }
}
