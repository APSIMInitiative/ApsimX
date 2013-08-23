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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.DataCombo = new System.Windows.Forms.ComboBox();
            this.DataGrid = new System.Windows.Forms.DataGridView();
            this.XYYYRadio = new System.Windows.Forms.RadioButton();
            this.YXXXRadio = new System.Windows.Forms.RadioButton();
            this.SeriesGrid = new System.Windows.Forms.DataGridView();
            this.PopupMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.DataGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SeriesGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // DataCombo
            // 
            this.DataCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DataCombo.FormattingEnabled = true;
            this.DataCombo.Location = new System.Drawing.Point(16, 194);
            this.DataCombo.Name = "DataCombo";
            this.DataCombo.Size = new System.Drawing.Size(327, 21);
            this.DataCombo.TabIndex = 0;
            this.DataCombo.TextChanged += new System.EventHandler(this.OnDataComboChanged);
            // 
            // DataGrid
            // 
            this.DataGrid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DataGrid.BackgroundColor = System.Drawing.SystemColors.Window;
            this.DataGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.DataGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.DataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DataGrid.Location = new System.Drawing.Point(16, 221);
            this.DataGrid.Name = "DataGrid";
            this.DataGrid.RowHeadersVisible = false;
            this.DataGrid.Size = new System.Drawing.Size(327, 120);
            this.DataGrid.TabIndex = 3;
            this.DataGrid.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.DataGrid_ColumnHeaderMouseClick);
            // 
            // XYYYRadio
            // 
            this.XYYYRadio.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.XYYYRadio.AutoSize = true;
            this.XYYYRadio.Checked = true;
            this.XYYYRadio.Location = new System.Drawing.Point(16, 169);
            this.XYYYRadio.Name = "XYYYRadio";
            this.XYYYRadio.Size = new System.Drawing.Size(45, 17);
            this.XYYYRadio.TabIndex = 4;
            this.XYYYRadio.TabStop = true;
            this.XYYYRadio.Text = "xyyy";
            this.XYYYRadio.UseVisualStyleBackColor = true;
            // 
            // YXXXRadio
            // 
            this.YXXXRadio.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.YXXXRadio.AutoSize = true;
            this.YXXXRadio.Location = new System.Drawing.Point(82, 169);
            this.YXXXRadio.Name = "YXXXRadio";
            this.YXXXRadio.Size = new System.Drawing.Size(45, 17);
            this.YXXXRadio.TabIndex = 5;
            this.YXXXRadio.Text = "yxxx";
            this.YXXXRadio.UseVisualStyleBackColor = true;
            // 
            // SeriesGrid
            // 
            this.SeriesGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SeriesGrid.BackgroundColor = System.Drawing.SystemColors.Window;
            this.SeriesGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.SeriesGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.SeriesGrid.ContextMenuStrip = this.PopupMenu;
            this.SeriesGrid.Location = new System.Drawing.Point(16, 13);
            this.SeriesGrid.Name = "SeriesGrid";
            this.SeriesGrid.RowHeadersVisible = false;
            this.SeriesGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.SeriesGrid.Size = new System.Drawing.Size(327, 150);
            this.SeriesGrid.TabIndex = 6;
            this.SeriesGrid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnCellEndEdit);
            // 
            // PopupMenu
            // 
            this.PopupMenu.Name = "ContextMenu";
            this.PopupMenu.Size = new System.Drawing.Size(153, 26);
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "X";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.HeaderText = "Y";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            // 
            // SeriesView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.SeriesGrid);
            this.Controls.Add(this.YXXXRadio);
            this.Controls.Add(this.XYYYRadio);
            this.Controls.Add(this.DataGrid);
            this.Controls.Add(this.DataCombo);
            this.Name = "SeriesView";
            this.Size = new System.Drawing.Size(360, 354);
            ((System.ComponentModel.ISupportInitialize)(this.DataGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SeriesGrid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox DataCombo;
        private System.Windows.Forms.DataGridView DataGrid;
        private System.Windows.Forms.RadioButton XYYYRadio;
        private System.Windows.Forms.RadioButton YXXXRadio;
        private System.Windows.Forms.DataGridView SeriesGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.ContextMenuStrip PopupMenu;
    }
}
