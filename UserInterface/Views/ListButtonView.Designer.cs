namespace UserInterface.Views
{
    partial class ListButtonView
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
            this.listBoxView1 = new ListBoxView();
            this.buttonView1 = new ButtonView();
            this.SuspendLayout();
            // 
            // listBoxView1
            // 
            this.listBoxView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxView1.AutoSize = true;
            this.listBoxView1.IsVisible = true;
            this.listBoxView1.Location = new System.Drawing.Point(3, 17);
            this.listBoxView1.Name = "listBoxView1";
            this.listBoxView1.SelectedValue = null;
            this.listBoxView1.Size = new System.Drawing.Size(335, 530);
            this.listBoxView1.TabIndex = 0;
            this.listBoxView1.Values = new string[0];
            // 
            // buttonView1
            // 
            this.buttonView1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonView1.IsVisible = true;
            this.buttonView1.Location = new System.Drawing.Point(347, 17);
            this.buttonView1.Name = "buttonView1";
            this.buttonView1.Size = new System.Drawing.Size(97, 36);
            this.buttonView1.TabIndex = 1;
            this.buttonView1.Value = "button1";
            // 
            // ListButtonView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.buttonView1);
            this.Controls.Add(this.listBoxView1);
            this.Name = "ListButtonView";
            this.Size = new System.Drawing.Size(454, 560);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ListBoxView listBoxView1;
        private ButtonView buttonView1;
    }
}
