// -----------------------------------------------------------------------
// <copyright file="GraphView.Designer.cs" company="CSIRO">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    public partial class GraphView
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// The graph's plot
        /// </summary>
        private OxyPlot.WindowsForms.PlotView plot1;

        /// <summary>
        /// The bottom panel that holds the editors.
        /// </summary>
        private System.Windows.Forms.Panel bottomPanel;

        /// <summary>
        /// The close button to hide the bottom panel.
        /// </summary>
        private System.Windows.Forms.Button closeButton;

        /// <summary>
        /// The context menu
        /// </summary>
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
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
            this.plot1 = new OxyPlot.WindowsForms.PlotView();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.bottomPanel = new System.Windows.Forms.Panel();
            this.closeButton = new System.Windows.Forms.Button();
            this.captionLabel = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.splitter = new System.Windows.Forms.Splitter();
            this.bottomPanel.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // plot1
            // 
            this.plot1.ContextMenuStrip = this.contextMenuStrip1;
            this.plot1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plot1.Location = new System.Drawing.Point(0, 0);
            this.plot1.Name = "plot1";
            this.plot1.PanCursor = System.Windows.Forms.Cursors.Hand;
            this.plot1.Size = new System.Drawing.Size(568, 294);
            this.plot1.TabIndex = 1;
            this.plot1.Text = "plot1";
            this.plot1.ZoomHorizontalCursor = System.Windows.Forms.Cursors.SizeWE;
            this.plot1.ZoomRectangleCursor = System.Windows.Forms.Cursors.SizeNWSE;
            this.plot1.ZoomVerticalCursor = System.Windows.Forms.Cursors.SizeNS;
            this.plot1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.OnMouseDoubleClick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // bottomPanel
            // 
            this.bottomPanel.Controls.Add(this.closeButton);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.Location = new System.Drawing.Point(0, 307);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Size = new System.Drawing.Size(568, 194);
            this.bottomPanel.TabIndex = 3;
            // 
            // closeButton
            // 
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.Image = global::UserInterface.Properties.Resources.Close;
            this.closeButton.Location = new System.Drawing.Point(546, 1);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(22, 23);
            this.closeButton.TabIndex = 0;
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.OnCloseEditorPanel);
            // 
            // captionLabel
            // 
            this.captionLabel.AutoSize = true;
            this.captionLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.captionLabel.Location = new System.Drawing.Point(0, 294);
            this.captionLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.captionLabel.MaximumSize = new System.Drawing.Size(375, 41);
            this.captionLabel.Name = "captionLabel";
            this.captionLabel.Size = new System.Drawing.Size(0, 13);
            this.captionLabel.TabIndex = 4;
            this.captionLabel.DoubleClick += new System.EventHandler(this.OnCaptionLabelDoubleClick);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.plot1);
            this.panel1.Controls.Add(this.captionLabel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(568, 307);
            this.panel1.TabIndex = 5;
            // 
            // splitter
            // 
            this.splitter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitter.Location = new System.Drawing.Point(0, 305);
            this.splitter.Margin = new System.Windows.Forms.Padding(2);
            this.splitter.Name = "splitter";
            this.splitter.Size = new System.Drawing.Size(568, 2);
            this.splitter.TabIndex = 6;
            this.splitter.TabStop = false;
            // 
            // GraphView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitter);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.bottomPanel);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "GraphView";
            this.Size = new System.Drawing.Size(568, 501);
            this.bottomPanel.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label captionLabel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Splitter splitter;
    }
}
