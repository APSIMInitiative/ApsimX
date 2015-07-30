// -----------------------------------------------------------------------
// <copyright file="InitialWaterView.Designer.cs" company="CSIRO">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    public partial class XYPairsView
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// The splitter between the graph plot and the editors.
        /// </summary>
        private System.Windows.Forms.Splitter splitter;

        /// <summary>
        /// Left hand panel.
        /// </summary>
        private System.Windows.Forms.Panel panel1;

        /// <summary>
        /// Initial water graph
        /// </summary>
        private GraphView graphView;

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
            this.splitter = new System.Windows.Forms.Splitter();
            this.panel1 = new System.Windows.Forms.Panel();
            this.graphView = new GraphView();
            this.gridView = new GridView();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitter
            // 
            this.splitter.BackColor = System.Drawing.SystemColors.Control;
            this.splitter.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter.Location = new System.Drawing.Point(0, 171);
            this.splitter.Name = "splitter";
            this.splitter.Size = new System.Drawing.Size(568, 5);
            this.splitter.TabIndex = 2;
            this.splitter.TabStop = false;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.gridView);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(568, 171);
            this.panel1.TabIndex = 3;
            // 
            // graphView
            // 
            this.graphView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphView.LeftRightPadding = 0;
            this.graphView.Location = new System.Drawing.Point(0, 176);
            this.graphView.Margin = new System.Windows.Forms.Padding(2);
            this.graphView.Name = "graphView";
            this.graphView.Size = new System.Drawing.Size(568, 325);
            this.graphView.TabIndex = 4;
            // 
            // gridView
            // 
            this.gridView.AutoFilterOn = false;
            this.gridView.DataSource = null;
            this.gridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridView.GetCurrentCell = null;
            this.gridView.Location = new System.Drawing.Point(0, 0);
            this.gridView.Margin = new System.Windows.Forms.Padding(4);
            this.gridView.ModelName = null;
            this.gridView.Name = "gridView";
            this.gridView.NumericFormat = null;
            this.gridView.ReadOnly = false;
            this.gridView.RowCount = 0;
            this.gridView.Size = new System.Drawing.Size(568, 171);
            this.gridView.TabIndex = 5;
            // 
            // XYPairsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.graphView);
            this.Controls.Add(this.splitter);
            this.Controls.Add(this.panel1);
            this.Name = "XYPairsView";
            this.Size = new System.Drawing.Size(568, 501);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private GridView gridView;

    }
}
