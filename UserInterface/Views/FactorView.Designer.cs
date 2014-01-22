namespace UserInterface.Views
{
    partial class FactorView
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
            this.editor = new Utility.Editor();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // editor
            // 
            this.editor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.editor.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
            this.editor.Lines = new string[] {
        ""};
            this.editor.Location = new System.Drawing.Point(20, 51);
            this.editor.Name = "editor";
            this.editor.Size = new System.Drawing.Size(558, 131);
            this.editor.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(332, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Enter the path(s) to the model or model field that this factor applies to.";
            // 
            // FactorView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.editor);
            this.Name = "FactorView";
            this.Size = new System.Drawing.Size(603, 566);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Utility.Editor editor;
        private System.Windows.Forms.Label label1;
    }
}
