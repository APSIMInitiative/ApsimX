namespace UserInterface.Views
{
    partial class SummaryView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SummaryView));
            this.TextBox = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.StateVariablesCheckBox = new System.Windows.Forms.CheckBox();
            this.HTMLCheckBox = new System.Windows.Forms.CheckBox();
            this.CreateButton = new System.Windows.Forms.Button();
            this.AutoCreateCheckBox = new System.Windows.Forms.CheckBox();
            this.HtmlControl = new HTMLView();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // TextBox
            // 
            this.TextBox.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TextBox.Location = new System.Drawing.Point(0, 45);
            this.TextBox.MaxLength = 1000000;
            this.TextBox.Multiline = true;
            this.TextBox.Name = "TextBox";
            this.TextBox.ReadOnly = true;
            this.TextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.TextBox.Size = new System.Drawing.Size(100, 20);
            this.TextBox.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.StateVariablesCheckBox);
            this.panel1.Controls.Add(this.HTMLCheckBox);
            this.panel1.Controls.Add(this.CreateButton);
            this.panel1.Controls.Add(this.AutoCreateCheckBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(754, 39);
            this.panel1.TabIndex = 4;
            // 
            // StateVariablesCheckBox
            // 
            this.StateVariablesCheckBox.AutoSize = true;
            this.StateVariablesCheckBox.Location = new System.Drawing.Point(349, 3);
            this.StateVariablesCheckBox.Name = "StateVariablesCheckBox";
            this.StateVariablesCheckBox.Size = new System.Drawing.Size(130, 17);
            this.StateVariablesCheckBox.TabIndex = 7;
            this.StateVariablesCheckBox.Text = "Show state variables?";
            this.StateVariablesCheckBox.UseVisualStyleBackColor = true;
            this.StateVariablesCheckBox.CheckedChanged += new System.EventHandler(this.OnStateVariablesCheckBoxChanged);
            // 
            // HTMLCheckBox
            // 
            this.HTMLCheckBox.AutoSize = true;
            this.HTMLCheckBox.Location = new System.Drawing.Point(3, 19);
            this.HTMLCheckBox.Name = "HTMLCheckBox";
            this.HTMLCheckBox.Size = new System.Drawing.Size(62, 17);
            this.HTMLCheckBox.TabIndex = 6;
            this.HTMLCheckBox.Text = "HTML?";
            this.HTMLCheckBox.UseVisualStyleBackColor = true;
            this.HTMLCheckBox.CheckedChanged += new System.EventHandler(this.OnHTMLCheckBoxChanged);
            // 
            // CreateButton
            // 
            this.CreateButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.CreateButton.AutoSize = true;
            this.CreateButton.Location = new System.Drawing.Point(626, 0);
            this.CreateButton.Name = "CreateButton";
            this.CreateButton.Size = new System.Drawing.Size(125, 23);
            this.CreateButton.TabIndex = 4;
            this.CreateButton.Text = "Write summary file now";
            this.CreateButton.UseVisualStyleBackColor = true;
            this.CreateButton.Click += new System.EventHandler(this.OnCreateButtonClick);
            // 
            // AutoCreateCheckBox
            // 
            this.AutoCreateCheckBox.AutoSize = true;
            this.AutoCreateCheckBox.Location = new System.Drawing.Point(3, 3);
            this.AutoCreateCheckBox.Name = "AutoCreateCheckBox";
            this.AutoCreateCheckBox.Size = new System.Drawing.Size(187, 17);
            this.AutoCreateCheckBox.TabIndex = 5;
            this.AutoCreateCheckBox.Text = "Automatically create summary file?";
            this.AutoCreateCheckBox.UseVisualStyleBackColor = true;
            this.AutoCreateCheckBox.CheckedChanged += new System.EventHandler(this.OnAutoCreateCheckBoxChanged);
            // 
            // HtmlControl
            // 
            this.HtmlControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.HtmlControl.LabelText = "Add text to the simulation notes";
            this.HtmlControl.Location = new System.Drawing.Point(0, 39);
            this.HtmlControl.MemoText = resources.GetString("HtmlControl.MemoText");
            this.HtmlControl.Name = "HtmlControl";
            this.HtmlControl.ReadOnly = false;
            this.HtmlControl.Size = new System.Drawing.Size(754, 734);
            this.HtmlControl.TabIndex = 5;
            // 
            // SummaryView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.HtmlControl);
            this.Controls.Add(this.TextBox);
            this.Controls.Add(this.panel1);
            this.Name = "SummaryView";
            this.Size = new System.Drawing.Size(754, 773);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox TextBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox HTMLCheckBox;
        private System.Windows.Forms.Button CreateButton;
        private System.Windows.Forms.CheckBox AutoCreateCheckBox;
        private System.Windows.Forms.CheckBox StateVariablesCheckBox;
        private HTMLView HtmlControl;
    }
}
