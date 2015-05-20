namespace UserInterface.Views
{
    partial class GenericPMFView
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
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.gridDependencies = new Views.GridView();
            this.label2 = new System.Windows.Forms.Label();
            this.gridParamters = new Views.GridView();
            this.label3 = new System.Windows.Forms.Label();
            this.gridProperties = new Views.GridView();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.label1);
            this.flowLayoutPanel1.Controls.Add(this.gridDependencies);
            this.flowLayoutPanel1.Controls.Add(this.label2);
            this.flowLayoutPanel1.Controls.Add(this.gridParamters);
            this.flowLayoutPanel1.Controls.Add(this.label3);
            this.flowLayoutPanel1.Controls.Add(this.gridProperties);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(1010, 899);
            this.flowLayoutPanel1.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(15, 15);
            this.label1.Margin = new System.Windows.Forms.Padding(15, 15, 3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(992, 25);
            this.label1.TabIndex = 0;
            this.label1.Text = "Dependencies:";
            // 
            // gridDependencies
            // 
            this.gridDependencies.AutoFilterOn = false;
            this.gridDependencies.DataSource = null;
            this.gridDependencies.GetCurrentCell = null;
            this.gridDependencies.Location = new System.Drawing.Point(4, 44);
            this.gridDependencies.Margin = new System.Windows.Forms.Padding(4);
            this.gridDependencies.Name = "gridDependencies";
            this.gridDependencies.NumericFormat = null;
            this.gridDependencies.ReadOnly = false;
            this.gridDependencies.RowCount = 0;
            this.gridDependencies.Size = new System.Drawing.Size(1002, 199);
            this.gridDependencies.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(15, 262);
            this.label2.Margin = new System.Windows.Forms.Padding(15, 15, 3, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(991, 25);
            this.label2.TabIndex = 4;
            this.label2.Text = "Parameters:";
            // 
            // gridParamters
            // 
            this.gridParamters.AutoFilterOn = false;
            this.gridParamters.DataSource = null;
            this.gridParamters.GetCurrentCell = null;
            this.gridParamters.Location = new System.Drawing.Point(4, 291);
            this.gridParamters.Margin = new System.Windows.Forms.Padding(4);
            this.gridParamters.Name = "gridParamters";
            this.gridParamters.NumericFormat = null;
            this.gridParamters.ReadOnly = false;
            this.gridParamters.RowCount = 0;
            this.gridParamters.Size = new System.Drawing.Size(1002, 213);
            this.gridParamters.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(15, 523);
            this.label3.Margin = new System.Windows.Forms.Padding(15, 15, 3, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(991, 25);
            this.label3.TabIndex = 6;
            this.label3.Text = "Properties:";
            // 
            // gridProperties
            // 
            this.gridProperties.AutoFilterOn = false;
            this.gridProperties.DataSource = null;
            this.gridProperties.GetCurrentCell = null;
            this.gridProperties.Location = new System.Drawing.Point(4, 552);
            this.gridProperties.Margin = new System.Windows.Forms.Padding(4);
            this.gridProperties.Name = "gridProperties";
            this.gridProperties.NumericFormat = null;
            this.gridProperties.ReadOnly = false;
            this.gridProperties.RowCount = 0;
            this.gridProperties.Size = new System.Drawing.Size(1002, 209);
            this.gridProperties.TabIndex = 7;
            // 
            // GenericPMFView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.flowLayoutPanel1);
            this.Name = "GenericPMFView";
            this.Size = new System.Drawing.Size(1010, 899);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private GridView gridDependencies;
        private System.Windows.Forms.Label label2;
        private GridView gridParamters;
        private System.Windows.Forms.Label label3;
        private GridView gridProperties;

    }
}
