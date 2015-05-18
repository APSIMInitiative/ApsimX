namespace UserInterface.Views
{
    partial class FunctionView
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
            
            this.grid = new Views.GridView();
            this.graph = new Views.GraphView();
            this.SuspendLayout();
            // 
            // grid
            // 
            this.grid.AutoFilterOn = false;
            this.grid.DataSource = null;
            this.grid.GetCurrentCell = null;
            this.grid.Location = new System.Drawing.Point(16, 15);
            this.grid.Margin = new System.Windows.Forms.Padding(4);
            this.grid.Name = "grid";
            this.grid.NumericFormat = null;
            this.grid.ReadOnly = false;
            this.grid.RowCount = 0;
            this.grid.Size = new System.Drawing.Size(472, 184);
            this.grid.TabIndex = 0;
            // 
            // graph
            // 
            this.graph.LeftRightPadding = 0;
            this.graph.Location = new System.Drawing.Point(16, 206);
            this.graph.Name = "graph";
            this.graph.Size = new System.Drawing.Size(472, 265);
            this.graph.TabIndex = 1;
            // 
            // FunctionView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.graph);
            this.Controls.Add(this.grid);
            this.Name = "FunctionView";
            this.Size = new System.Drawing.Size(523, 511);
            this.ResumeLayout(false);

        }

        #endregion

        private GridView grid;
        private GraphView graph;
    }
}
