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
            this.listBoxView1 = new Views.ListBoxView();
            this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.SuspendLayout();
            // 
            // listBoxView1
            // 
            this.listBoxView1.AutoSize = true;
            this.listBoxView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBoxView1.IsVisible = true;
            this.listBoxView1.Location = new System.Drawing.Point(0, 0);
            this.listBoxView1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.listBoxView1.Name = "listBoxView1";
            this.listBoxView1.SelectedValue = null;
            this.listBoxView1.Size = new System.Drawing.Size(340, 455);
            this.listBoxView1.TabIndex = 0;
            this.listBoxView1.Values = new string[0];
            // 
            // buttonPanel
            // 
            this.buttonPanel.AutoSize = true;
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.buttonPanel.Location = new System.Drawing.Point(0, 0);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(340, 0);
            this.buttonPanel.TabIndex = 2;
            // 
            // ListButtonView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.listBoxView1);
            this.Controls.Add(this.buttonPanel);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "ListButtonView";
            this.Size = new System.Drawing.Size(340, 455);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ListBoxView listBoxView1;
        private System.Windows.Forms.FlowLayoutPanel buttonPanel;
    }
}
