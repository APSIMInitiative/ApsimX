
namespace UserInterface.Views
{
    partial class TabbedMetDataView
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
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.BrowsePanelControl = new System.Windows.Forms.Panel();
            this.WorksheetNamesControl = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.FileNameControl = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.GraphOptionPanelControl = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.GraphShowYearsControl = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.GraphStartYearControl = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabSummary = new System.Windows.Forms.TabPage();
            this.panel2 = new System.Windows.Forms.Panel();
            this.graphViewSummary = new GraphView();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.tabData = new System.Windows.Forms.TabPage();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.tabRainfall = new System.Windows.Forms.TabPage();
            this.panel6 = new System.Windows.Forms.Panel();
            this.graphViewRainfall = new GraphView();
            this.tabRainfallMonth = new System.Windows.Forms.TabPage();
            this.graphViewMonthlyRainfall = new GraphView();
            this.tabTemperature = new System.Windows.Forms.TabPage();
            this.graphViewTemperature = new GraphView();
            this.tabRadiation = new System.Windows.Forms.TabPage();
            this.graphViewRadiation = new GraphView();
            this.BrowsePanelControl.SuspendLayout();
            this.GraphOptionPanelControl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GraphShowYearsControl)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GraphStartYearControl)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabSummary.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tabData.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.tabRainfall.SuspendLayout();
            this.panel6.SuspendLayout();
            this.tabRainfallMonth.SuspendLayout();
            this.tabTemperature.SuspendLayout();
            this.tabRadiation.SuspendLayout();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Filter = "APSIM Weather file (*.met, *.xlsx)|*.met;*.xlsx";
            this.openFileDialog1.Title = "Open an APSIM Weather file";
            // 
            // BrowsePanelControl
            // 
            this.BrowsePanelControl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.BrowsePanelControl.Controls.Add(this.WorksheetNamesControl);
            this.BrowsePanelControl.Controls.Add(this.label5);
            this.BrowsePanelControl.Controls.Add(this.FileNameControl);
            this.BrowsePanelControl.Controls.Add(this.button2);
            this.BrowsePanelControl.Dock = System.Windows.Forms.DockStyle.Top;
            this.BrowsePanelControl.Location = new System.Drawing.Point(0, 0);
            this.BrowsePanelControl.Margin = new System.Windows.Forms.Padding(3, 3, 3, 6);
            this.BrowsePanelControl.Name = "BrowsePanelControl";
            this.BrowsePanelControl.Size = new System.Drawing.Size(550, 38);
            this.BrowsePanelControl.TabIndex = 0;
            // 
            // WorksheetNamesControl
            // 
            this.WorksheetNamesControl.FormattingEnabled = true;
            this.WorksheetNamesControl.Location = new System.Drawing.Point(167, 36);
            this.WorksheetNamesControl.Name = "WorksheetNamesControl";
            this.WorksheetNamesControl.Size = new System.Drawing.Size(183, 21);
            this.WorksheetNamesControl.TabIndex = 3;
            this.WorksheetNamesControl.SelectedValueChanged += new System.EventHandler(this.WorkSheetNameControl_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(82, 39);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(69, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "Sheet names";
            // 
            // FileNameControl
            // 
            this.FileNameControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.FileNameControl.Location = new System.Drawing.Point(79, 9);
            this.FileNameControl.Name = "FileNameControl";
            this.FileNameControl.Size = new System.Drawing.Size(453, 18);
            this.FileNameControl.TabIndex = 1;
            this.FileNameControl.Text = "File name";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(3, 4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(60, 23);
            this.button2.TabIndex = 0;
            this.button2.Text = "Browse...";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.OnButton1Click);
            // 
            // GraphOptionPanelControl
            // 
            this.GraphOptionPanelControl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.GraphOptionPanelControl.Controls.Add(this.label1);
            this.GraphOptionPanelControl.Controls.Add(this.label2);
            this.GraphOptionPanelControl.Controls.Add(this.GraphShowYearsControl);
            this.GraphOptionPanelControl.Controls.Add(this.label3);
            this.GraphOptionPanelControl.Controls.Add(this.GraphStartYearControl);
            this.GraphOptionPanelControl.Controls.Add(this.label4);
            this.GraphOptionPanelControl.Dock = System.Windows.Forms.DockStyle.Top;
            this.GraphOptionPanelControl.Location = new System.Drawing.Point(0, 38);
            this.GraphOptionPanelControl.Margin = new System.Windows.Forms.Padding(3, 3, 3, 6);
            this.GraphOptionPanelControl.Name = "GraphOptionPanelControl";
            this.GraphOptionPanelControl.Size = new System.Drawing.Size(550, 36);
            this.GraphOptionPanelControl.TabIndex = 3;
            this.GraphOptionPanelControl.Visible = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(48, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(78, 13);
            this.label1.TabIndex = 32;
            this.label1.Text = "Graph Options:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(469, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 13);
            this.label2.TabIndex = 31;
            this.label2.Text = "years";
            // 
            // GraphShowYearsControl
            // 
            this.GraphShowYearsControl.AllowDrop = true;
            this.GraphShowYearsControl.Location = new System.Drawing.Point(418, 8);
            this.GraphShowYearsControl.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.GraphShowYearsControl.Name = "GraphShowYearsControl";
            this.GraphShowYearsControl.Size = new System.Drawing.Size(45, 20);
            this.GraphShowYearsControl.TabIndex = 30;
            this.GraphShowYearsControl.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.GraphShowYearsControl.ValueChanged += new System.EventHandler(this.uxGraphShowYears_ValueChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(380, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(32, 13);
            this.label3.TabIndex = 29;
            this.label3.Text = "show";
            // 
            // GraphStartYearControl
            // 
            this.GraphStartYearControl.AllowDrop = true;
            this.GraphStartYearControl.Location = new System.Drawing.Point(274, 8);
            this.GraphStartYearControl.Maximum = new decimal(new int[] {
            3000,
            0,
            0,
            0});
            this.GraphStartYearControl.Minimum = new decimal(new int[] {
            1899,
            0,
            0,
            0});
            this.GraphStartYearControl.Name = "GraphStartYearControl";
            this.GraphStartYearControl.Size = new System.Drawing.Size(57, 20);
            this.GraphStartYearControl.TabIndex = 28;
            this.GraphStartYearControl.Value = new decimal(new int[] {
            1900,
            0,
            0,
            0});
            this.GraphStartYearControl.ValueChanged += new System.EventHandler(this.uxGraphStartYear_ValueChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(164, 9);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(104, 13);
            this.label4.TabIndex = 27;
            this.label4.Text = "Start year for Graphs";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabSummary);
            this.tabControl1.Controls.Add(this.tabData);
            this.tabControl1.Controls.Add(this.tabRainfall);
            this.tabControl1.Controls.Add(this.tabRainfallMonth);
            this.tabControl1.Controls.Add(this.tabTemperature);
            this.tabControl1.Controls.Add(this.tabRadiation);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 74);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.Padding = new System.Drawing.Point(8, 3);
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(550, 313);
            this.tabControl1.TabIndex = 4;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.TabControl1_SelectedIndexChanged);
            // 
            // tabSummary
            // 
            this.tabSummary.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabSummary.Controls.Add(this.panel2);
            this.tabSummary.Location = new System.Drawing.Point(4, 22);
            this.tabSummary.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.tabSummary.Name = "tabSummary";
            this.tabSummary.Padding = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.tabSummary.Size = new System.Drawing.Size(542, 287);
            this.tabSummary.TabIndex = 1;
            this.tabSummary.Text = "Summary";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.graphViewSummary);
            this.panel2.Controls.Add(this.splitter1);
            this.panel2.Controls.Add(this.richTextBox1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(3, 3);
            this.panel2.MinimumSize = new System.Drawing.Size(0, 150);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(536, 284);
            this.panel2.TabIndex = 3;
            // 
            // graphViewSummary
            // 
            this.graphViewSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphViewSummary.IsLegendVisible = true;
            this.graphViewSummary.LeftRightPadding = 0;
            this.graphViewSummary.Location = new System.Drawing.Point(0, 143);
            this.graphViewSummary.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.graphViewSummary.Name = "graphViewSummary";
            this.graphViewSummary.Size = new System.Drawing.Size(536, 141);
            this.graphViewSummary.TabIndex = 2;
            // 
            // splitter1
            // 
            this.splitter1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter1.Location = new System.Drawing.Point(0, 137);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(536, 6);
            this.splitter1.TabIndex = 3;
            this.splitter1.TabStop = false;
            // 
            // richTextBox1
            // 
            this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.richTextBox1.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox1.Location = new System.Drawing.Point(0, 0);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(536, 137);
            this.richTextBox1.TabIndex = 4;
            this.richTextBox1.Text = "";
            // 
            // tabData
            // 
            this.tabData.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabData.Controls.Add(this.dataGridView1);
            this.tabData.Location = new System.Drawing.Point(4, 22);
            this.tabData.Name = "tabData";
            this.tabData.Padding = new System.Windows.Forms.Padding(3);
            this.tabData.Size = new System.Drawing.Size(542, 287);
            this.tabData.TabIndex = 0;
            this.tabData.Text = "Data";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(3, 3);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.Size = new System.Drawing.Size(536, 281);
            this.dataGridView1.TabIndex = 0;
            // 
            // tabRainfall
            // 
            this.tabRainfall.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabRainfall.Controls.Add(this.panel6);
            this.tabRainfall.Location = new System.Drawing.Point(4, 22);
            this.tabRainfall.Name = "tabRainfall";
            this.tabRainfall.Size = new System.Drawing.Size(542, 287);
            this.tabRainfall.TabIndex = 2;
            this.tabRainfall.Text = "Rainfall Chart";
            // 
            // panel6
            // 
            this.panel6.Controls.Add(this.graphViewRainfall);
            this.panel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel6.Location = new System.Drawing.Point(0, 0);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(542, 287);
            this.panel6.TabIndex = 2;
            // 
            // graphViewRainfall
            // 
            this.graphViewRainfall.AccessibleDescription = "";
            this.graphViewRainfall.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphViewRainfall.IsLegendVisible = true;
            this.graphViewRainfall.LeftRightPadding = 40;
            this.graphViewRainfall.Location = new System.Drawing.Point(0, 0);
            this.graphViewRainfall.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.graphViewRainfall.Name = "graphViewRainfall";
            this.graphViewRainfall.Size = new System.Drawing.Size(542, 287);
            this.graphViewRainfall.TabIndex = 4;
            // 
            // tabRainfallMonth
            // 
            this.tabRainfallMonth.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabRainfallMonth.Controls.Add(this.graphViewMonthlyRainfall);
            this.tabRainfallMonth.Location = new System.Drawing.Point(4, 22);
            this.tabRainfallMonth.Name = "tabRainfallMonth";
            this.tabRainfallMonth.Size = new System.Drawing.Size(542, 287);
            this.tabRainfallMonth.TabIndex = 6;
            this.tabRainfallMonth.Text = "Monthly Rainfall";
            // 
            // graphViewMonthlyRainfall
            // 
            this.graphViewMonthlyRainfall.AccessibleDescription = "";
            this.graphViewMonthlyRainfall.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphViewMonthlyRainfall.IsLegendVisible = true;
            this.graphViewMonthlyRainfall.LeftRightPadding = 40;
            this.graphViewMonthlyRainfall.Location = new System.Drawing.Point(0, 0);
            this.graphViewMonthlyRainfall.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.graphViewMonthlyRainfall.Name = "graphViewMonthlyRainfall";
            this.graphViewMonthlyRainfall.Size = new System.Drawing.Size(542, 287);
            this.graphViewMonthlyRainfall.TabIndex = 4;
            // 
            // tabTemperature
            // 
            this.tabTemperature.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabTemperature.Controls.Add(this.graphViewTemperature);
            this.tabTemperature.Location = new System.Drawing.Point(4, 22);
            this.tabTemperature.Name = "tabTemperature";
            this.tabTemperature.Size = new System.Drawing.Size(542, 287);
            this.tabTemperature.TabIndex = 4;
            this.tabTemperature.Text = "Temperature";
            // 
            // graphViewTemperature
            // 
            this.graphViewTemperature.AccessibleDescription = "";
            this.graphViewTemperature.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphViewTemperature.IsLegendVisible = true;
            this.graphViewTemperature.LeftRightPadding = 40;
            this.graphViewTemperature.Location = new System.Drawing.Point(0, 0);
            this.graphViewTemperature.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.graphViewTemperature.Name = "graphViewTemperature";
            this.graphViewTemperature.Size = new System.Drawing.Size(542, 287);
            this.graphViewTemperature.TabIndex = 4;
            // 
            // tabRadiation
            // 
            this.tabRadiation.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabRadiation.Controls.Add(this.graphViewRadiation);
            this.tabRadiation.Location = new System.Drawing.Point(4, 22);
            this.tabRadiation.Name = "tabRadiation";
            this.tabRadiation.Size = new System.Drawing.Size(542, 287);
            this.tabRadiation.TabIndex = 5;
            this.tabRadiation.Text = "Radiation";
            // 
            // graphViewRadiation
            // 
            this.graphViewRadiation.AccessibleDescription = "";
            this.graphViewRadiation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graphViewRadiation.IsLegendVisible = true;
            this.graphViewRadiation.LeftRightPadding = 40;
            this.graphViewRadiation.Location = new System.Drawing.Point(0, 0);
            this.graphViewRadiation.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.graphViewRadiation.Name = "graphViewRadiation";
            this.graphViewRadiation.Size = new System.Drawing.Size(542, 287);
            this.graphViewRadiation.TabIndex = 5;
            // 
            // TabbedMetDataView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.GraphOptionPanelControl);
            this.Controls.Add(this.BrowsePanelControl);
            this.Name = "TabbedMetDataView";
            this.Size = new System.Drawing.Size(550, 387);
            this.BrowsePanelControl.ResumeLayout(false);
            this.BrowsePanelControl.PerformLayout();
            this.GraphOptionPanelControl.ResumeLayout(false);
            this.GraphOptionPanelControl.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GraphShowYearsControl)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GraphStartYearControl)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabSummary.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.tabData.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.tabRainfall.ResumeLayout(false);
            this.panel6.ResumeLayout(false);
            this.tabRainfallMonth.ResumeLayout(false);
            this.tabTemperature.ResumeLayout(false);
            this.tabRadiation.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Panel BrowsePanelControl;
        private System.Windows.Forms.Label FileNameControl;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Panel GraphOptionPanelControl;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown GraphShowYearsControl;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown GraphStartYearControl;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabSummary;
        private System.Windows.Forms.Panel panel2;
        private GraphView graphViewSummary;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.TabPage tabData;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.TabPage tabRainfall;
        private System.Windows.Forms.Panel panel6;
        private GraphView graphViewRainfall;
        private System.Windows.Forms.TabPage tabRainfallMonth;
        private GraphView graphViewMonthlyRainfall;
        private System.Windows.Forms.TabPage tabTemperature;
        private GraphView graphViewTemperature;
        private System.Windows.Forms.TabPage tabRadiation;
        private GraphView graphViewRadiation;
        private System.Windows.Forms.ComboBox WorksheetNamesControl;
        private System.Windows.Forms.Label label5;
    }
}
