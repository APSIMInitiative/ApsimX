namespace UserInterface.Views
{
    partial class HtmlView
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
            this.HtmlControl = new System.Windows.Forms.WebBrowser();
            this.TextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // HtmlControl
            // 
            this.HtmlControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.HtmlControl.Location = new System.Drawing.Point(0, 0);
            this.HtmlControl.MinimumSize = new System.Drawing.Size(20, 20);
            this.HtmlControl.Name = "HtmlControl";
            this.HtmlControl.Size = new System.Drawing.Size(754, 773);
            this.HtmlControl.TabIndex = 0;
            // 
            // TextBox
            // 
            this.TextBox.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TextBox.Location = new System.Drawing.Point(0, 0);
            this.TextBox.MaxLength = 1000000;
            this.TextBox.Multiline = true;
            this.TextBox.Name = "TextBox";
            this.TextBox.ReadOnly = true;
            this.TextBox.Size = new System.Drawing.Size(100, 20);
            this.TextBox.TabIndex = 1;
            // 
            // HtmlView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.TextBox);
            this.Controls.Add(this.HtmlControl);
            this.Name = "HtmlView";
            this.Size = new System.Drawing.Size(754, 773);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.WebBrowser HtmlControl;
        private System.Windows.Forms.TextBox TextBox;
    }
}
