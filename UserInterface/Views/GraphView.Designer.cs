namespace UserInterface.Views
{
    partial class GraphView
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
            this.plot1 = new OxyPlot.WindowsForms.Plot();
            this.Splitter = new System.Windows.Forms.Splitter();
            this.BottomPanel = new System.Windows.Forms.Panel();
            this.CloseButton = new System.Windows.Forms.Button();
            this.BottomPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // plot1
            // 
            this.plot1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plot1.KeyboardPanHorizontalStep = 0.1D;
            this.plot1.KeyboardPanVerticalStep = 0.1D;
            this.plot1.Location = new System.Drawing.Point(0, 0);
            this.plot1.Name = "plot1";
            this.plot1.PanCursor = System.Windows.Forms.Cursors.Hand;
            this.plot1.Size = new System.Drawing.Size(568, 302);
            this.plot1.TabIndex = 1;
            this.plot1.Text = "plot1";
            this.plot1.ZoomHorizontalCursor = System.Windows.Forms.Cursors.SizeWE;
            this.plot1.ZoomRectangleCursor = System.Windows.Forms.Cursors.SizeNWSE;
            this.plot1.ZoomVerticalCursor = System.Windows.Forms.Cursors.SizeNS;
            this.plot1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.OnMouseDoubleClick);
            // 
            // Splitter
            // 
            this.Splitter.BackColor = System.Drawing.SystemColors.Control;
            this.Splitter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.Splitter.Location = new System.Drawing.Point(0, 302);
            this.Splitter.Name = "Splitter";
            this.Splitter.Size = new System.Drawing.Size(568, 5);
            this.Splitter.TabIndex = 2;
            this.Splitter.TabStop = false;
            // 
            // BottomPanel
            // 
            this.BottomPanel.Controls.Add(this.CloseButton);
            this.BottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.BottomPanel.Location = new System.Drawing.Point(0, 307);
            this.BottomPanel.Name = "BottomPanel";
            this.BottomPanel.Size = new System.Drawing.Size(568, 194);
            this.BottomPanel.TabIndex = 3;
            // 
            // CloseButton
            // 
            this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseButton.Image = global::UserInterface.Properties.Resources.Close;
            this.CloseButton.Location = new System.Drawing.Point(546, 1);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(22, 23);
            this.CloseButton.TabIndex = 0;
            this.CloseButton.UseVisualStyleBackColor = true;
            this.CloseButton.Click += new System.EventHandler(this.CloseEditorPanel);
            // 
            // GraphView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.plot1);
            this.Controls.Add(this.Splitter);
            this.Controls.Add(this.BottomPanel);
            this.Name = "GraphView";
            this.Size = new System.Drawing.Size(568, 501);
            this.BottomPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private OxyPlot.WindowsForms.Plot plot1;
        private System.Windows.Forms.Splitter Splitter;
        private System.Windows.Forms.Panel BottomPanel;
        private System.Windows.Forms.Button CloseButton;



    }
}
