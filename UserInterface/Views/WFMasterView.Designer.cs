namespace UserInterface.Views
{
    partial class WFMasterView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WFMasterView));
            this.panel1 = new System.Windows.Forms.Panel();
            this.HelpLinkLabel = new System.Windows.Forms.LinkLabel();
            this.DescriptionLabel = new System.Windows.Forms.Label();
            this.ModelTypeLabel = new System.Windows.Forms.Label();
            this.LowerPanel = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.AutoSize = true;
            this.panel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panel1.BackColor = System.Drawing.SystemColors.Control;
            this.panel1.Controls.Add(this.HelpLinkLabel);
            this.panel1.Controls.Add(this.DescriptionLabel);
            this.panel1.Controls.Add(this.ModelTypeLabel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(804, 118);
            this.panel1.TabIndex = 2;
            // 
            // HelpLinkLabel
            // 
            this.HelpLinkLabel.AutoSize = true;
            this.HelpLinkLabel.BackColor = System.Drawing.SystemColors.Control;
            this.HelpLinkLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.HelpLinkLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.HelpLinkLabel.LinkColor = System.Drawing.Color.DodgerBlue;
            this.HelpLinkLabel.Location = new System.Drawing.Point(0, 97);
            this.HelpLinkLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.HelpLinkLabel.Name = "HelpLinkLabel";
            this.HelpLinkLabel.Padding = new System.Windows.Forms.Padding(4, 0, 0, 8);
            this.HelpLinkLabel.Size = new System.Drawing.Size(104, 21);
            this.HelpLinkLabel.TabIndex = 5;
            this.HelpLinkLabel.TabStop = true;
            this.HelpLinkLabel.Text = "more information";
            this.HelpLinkLabel.Click += new System.EventHandler(this.HelpLinkLabel_Click);
            // 
            // DescriptionLabel
            // 
            this.DescriptionLabel.BackColor = System.Drawing.SystemColors.Control;
            this.DescriptionLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.DescriptionLabel.Location = new System.Drawing.Point(0, 31);
            this.DescriptionLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.DescriptionLabel.Name = "DescriptionLabel";
            this.DescriptionLabel.Padding = new System.Windows.Forms.Padding(4, 0, 0, 8);
            this.DescriptionLabel.Size = new System.Drawing.Size(804, 66);
            this.DescriptionLabel.TabIndex = 6;
            this.DescriptionLabel.Text = resources.GetString("DescriptionLabel.Text");
            // 
            // ModelTypeLabel
            // 
            this.ModelTypeLabel.BackColor = System.Drawing.SystemColors.Control;
            this.ModelTypeLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.ModelTypeLabel.Font = new System.Drawing.Font("Candara", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ModelTypeLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(153)))), ((int)(((byte)(153)))));
            this.ModelTypeLabel.Location = new System.Drawing.Point(0, 0);
            this.ModelTypeLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ModelTypeLabel.MaximumSize = new System.Drawing.Size(0, 31);
            this.ModelTypeLabel.Name = "ModelTypeLabel";
            this.ModelTypeLabel.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.ModelTypeLabel.Size = new System.Drawing.Size(804, 31);
            this.ModelTypeLabel.TabIndex = 4;
            this.ModelTypeLabel.Text = "Activities.RumiantActivityExample";
            // 
            // LowerPanel
            // 
            this.LowerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LowerPanel.Location = new System.Drawing.Point(0, 118);
            this.LowerPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.LowerPanel.Name = "LowerPanel";
            this.LowerPanel.Size = new System.Drawing.Size(804, 634);
            this.LowerPanel.TabIndex = 3;
            // 
            // WFMasterView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.LowerPanel);
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "WFMasterView";
            this.Size = new System.Drawing.Size(804, 752);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.LinkLabel HelpLinkLabel;
        private System.Windows.Forms.Label ModelTypeLabel;
        private System.Windows.Forms.Panel LowerPanel;
        private System.Windows.Forms.Label DescriptionLabel;
    }
}
