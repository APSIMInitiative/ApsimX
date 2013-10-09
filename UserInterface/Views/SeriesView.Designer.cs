namespace UserInterface.Views
{
    partial class SeriesView
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.DataSourceCombo = new System.Windows.Forms.ComboBox();
            this.DataGridView = new GridView();
            this.SeriesGridView = new GridView();
            this.XRadio = new System.Windows.Forms.RadioButton();
            this.YRadio = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 160);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 13);
            this.label1.TabIndex = 51;
            this.label1.Text = "Data source:";
            // 
            // DataSourceCombo
            // 
            this.DataSourceCombo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.DataSourceCombo.FormattingEnabled = true;
            this.DataSourceCombo.Location = new System.Drawing.Point(88, 157);
            this.DataSourceCombo.Name = "DataSourceCombo";
            this.DataSourceCombo.Size = new System.Drawing.Size(287, 21);
            this.DataSourceCombo.TabIndex = 52;
            this.DataSourceCombo.TextChanged += new System.EventHandler(this.OnDataSourceComboChanged);
            // 
            // DataGridView
            // 
            this.DataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DataGridView.DataSource = null;
            this.DataGridView.Location = new System.Drawing.Point(14, 239);
            this.DataGridView.Name = "DataGridView";
            this.DataGridView.ReadOnly = false;
            this.DataGridView.RowCount = 0;
            this.DataGridView.Size = new System.Drawing.Size(677, 129);
            this.DataGridView.TabIndex = 64;
            // 
            // SeriesGridView
            // 
            this.SeriesGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SeriesGridView.DataSource = null;
            this.SeriesGridView.Location = new System.Drawing.Point(14, 38);
            this.SeriesGridView.Name = "SeriesGridView";
            this.SeriesGridView.ReadOnly = false;
            this.SeriesGridView.RowCount = 0;
            this.SeriesGridView.Size = new System.Drawing.Size(677, 113);
            this.SeriesGridView.TabIndex = 63;
            // 
            // XRadio
            // 
            this.XRadio.AutoSize = true;
            this.XRadio.Location = new System.Drawing.Point(17, 216);
            this.XRadio.Name = "XRadio";
            this.XRadio.Size = new System.Drawing.Size(73, 17);
            this.XRadio.TabIndex = 65;
            this.XRadio.TabStop = true;
            this.XRadio.Text = "Click on X";
            this.XRadio.UseVisualStyleBackColor = true;
            // 
            // YRadio
            // 
            this.YRadio.AutoSize = true;
            this.YRadio.Location = new System.Drawing.Point(140, 216);
            this.YRadio.Name = "YRadio";
            this.YRadio.Size = new System.Drawing.Size(73, 17);
            this.YRadio.TabIndex = 66;
            this.YRadio.TabStop = true;
            this.YRadio.Text = "Click on Y";
            this.YRadio.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.BackColor = System.Drawing.SystemColors.Info;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.InfoText;
            this.label2.Location = new System.Drawing.Point(14, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(677, 23);
            this.label2.TabIndex = 67;
            this.label2.Text = "The top grid lists all graph series. You can change the values in the grid.";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.BackColor = System.Drawing.SystemColors.Info;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.InfoText;
            this.label3.Location = new System.Drawing.Point(14, 190);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(677, 23);
            this.label3.TabIndex = 68;
            this.label3.Text = "You can add series to the graph by clicking on the column headings below.";
            // 
            // SeriesView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.YRadio);
            this.Controls.Add(this.XRadio);
            this.Controls.Add(this.DataGridView);
            this.Controls.Add(this.SeriesGridView);
            this.Controls.Add(this.DataSourceCombo);
            this.Controls.Add(this.label1);
            this.Name = "SeriesView";
            this.Size = new System.Drawing.Size(710, 383);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox DataSourceCombo;
        private GridView SeriesGridView;
        private GridView DataGridView;
        private System.Windows.Forms.RadioButton XRadio;
        private System.Windows.Forms.RadioButton YRadio;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}