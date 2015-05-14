namespace UserInterface.Views
{
    partial class HTMLView
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HTMLView));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.ToolStrip1 = new System.Windows.Forms.ToolStrip();
            this.headingComboBox = new System.Windows.Forms.ToolStripComboBox();
            this.ToolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.ToolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.boldButton = new System.Windows.Forms.ToolStripButton();
            this.italicButton = new System.Windows.Forms.ToolStripButton();
            this.underlineButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.superscriptButton = new System.Windows.Forms.ToolStripButton();
            this.subscriptButton = new System.Windows.Forms.ToolStripButton();
            this.ToolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // richTextBox1
            // 
            this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox1.Location = new System.Drawing.Point(0, 28);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(704, 530);
            this.richTextBox1.TabIndex = 1;
            this.richTextBox1.Text = "";
            this.richTextBox1.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.OnLinkClicked);
            this.richTextBox1.SelectionChanged += new System.EventHandler(this.OnSelectionChanged);
            // 
            // ToolStrip1
            // 
            this.ToolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.headingComboBox,
            this.ToolStripSeparator1,
            this.ToolStripSeparator2,
            this.boldButton,
            this.italicButton,
            this.underlineButton,
            this.toolStripButton1,
            this.superscriptButton,
            this.subscriptButton});
            this.ToolStrip1.Location = new System.Drawing.Point(0, 0);
            this.ToolStrip1.Name = "ToolStrip1";
            this.ToolStrip1.Size = new System.Drawing.Size(704, 28);
            this.ToolStrip1.TabIndex = 2;
            this.ToolStrip1.Text = "ToolStrip1";
            // 
            // headingComboBox
            // 
            this.headingComboBox.Items.AddRange(new object[] {
            "Heading 1",
            "Heading 2",
            "Heading 3",
            "Normal"});
            this.headingComboBox.Name = "headingComboBox";
            this.headingComboBox.Size = new System.Drawing.Size(121, 28);
            this.headingComboBox.Text = "Normal";
            this.headingComboBox.TextChanged += new System.EventHandler(this.OnHeadingChanged);
            // 
            // ToolStripSeparator1
            // 
            this.ToolStripSeparator1.Name = "ToolStripSeparator1";
            this.ToolStripSeparator1.Size = new System.Drawing.Size(6, 28);
            // 
            // ToolStripSeparator2
            // 
            this.ToolStripSeparator2.Name = "ToolStripSeparator2";
            this.ToolStripSeparator2.Size = new System.Drawing.Size(6, 28);
            // 
            // boldButton
            // 
            this.boldButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.boldButton.Image = ((System.Drawing.Image)(resources.GetObject("boldButton.Image")));
            this.boldButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.boldButton.Name = "boldButton";
            this.boldButton.Size = new System.Drawing.Size(23, 25);
            this.boldButton.Text = "Bold";
            this.boldButton.Click += new System.EventHandler(this.OnBoldClick);
            // 
            // italicButton
            // 
            this.italicButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.italicButton.Image = ((System.Drawing.Image)(resources.GetObject("italicButton.Image")));
            this.italicButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.italicButton.Name = "italicButton";
            this.italicButton.Size = new System.Drawing.Size(23, 25);
            this.italicButton.Text = "Italic";
            this.italicButton.Click += new System.EventHandler(this.OnItalicClick);
            // 
            // underlineButton
            // 
            this.underlineButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.underlineButton.Image = ((System.Drawing.Image)(resources.GetObject("underlineButton.Image")));
            this.underlineButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.underlineButton.Name = "underlineButton";
            this.underlineButton.Size = new System.Drawing.Size(23, 25);
            this.underlineButton.Text = "Underline";
            this.underlineButton.Click += new System.EventHandler(this.OnUnderlineClick);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(23, 25);
            this.toolStripButton1.Text = "toolStripButton1";
            this.toolStripButton1.Click += new System.EventHandler(this.OnStrikeThroughClick);
            // 
            // superscriptButton
            // 
            this.superscriptButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.superscriptButton.Image = ((System.Drawing.Image)(resources.GetObject("superscriptButton.Image")));
            this.superscriptButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.superscriptButton.Name = "superscriptButton";
            this.superscriptButton.Size = new System.Drawing.Size(23, 25);
            this.superscriptButton.Text = "toolStripButton2";
            this.superscriptButton.Click += new System.EventHandler(this.OnSuperscriptClick);
            // 
            // subscriptButton
            // 
            this.subscriptButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.subscriptButton.Image = ((System.Drawing.Image)(resources.GetObject("subscriptButton.Image")));
            this.subscriptButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.subscriptButton.Name = "subscriptButton";
            this.subscriptButton.Size = new System.Drawing.Size(23, 25);
            this.subscriptButton.Text = "toolStripButton2";
            this.subscriptButton.Click += new System.EventHandler(this.OnSubscriptClick);
            // 
            // HTMLView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.ToolStrip1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "HTMLView";
            this.Size = new System.Drawing.Size(704, 558);
            this.ToolStrip1.ResumeLayout(false);
            this.ToolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.RichTextBox richTextBox1;
        internal System.Windows.Forms.ToolStrip ToolStrip1;
        internal System.Windows.Forms.ToolStripSeparator ToolStripSeparator1;
        internal System.Windows.Forms.ToolStripSeparator ToolStripSeparator2;
        internal System.Windows.Forms.ToolStripButton boldButton;
        internal System.Windows.Forms.ToolStripButton italicButton;
        internal System.Windows.Forms.ToolStripButton underlineButton;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripButton superscriptButton;
        private System.Windows.Forms.ToolStripButton subscriptButton;
        private System.Windows.Forms.ToolStripComboBox headingComboBox;
    }
}
