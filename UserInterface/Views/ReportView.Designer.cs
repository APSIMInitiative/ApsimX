namespace UserInterface.Views
{
    partial class ReportView
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.VariableEditor = new Views.EditorView();
            this.label2 = new System.Windows.Forms.Label();
            this.FrequencyEditor = new Views.EditorView();
            this.label1 = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.lResults = new System.Windows.Forms.Label();
            this.bHome = new System.Windows.Forms.Button();
            this.bStart = new System.Windows.Forms.Button();
            this.bForward = new System.Windows.Forms.Button();
            this.bEnd = new System.Windows.Forms.Button();
            this.GridView = new Views.GridView();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.VariableEditor);
            this.splitContainer1.Panel1.Controls.Add(this.label2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.FrequencyEditor);
            this.splitContainer1.Panel2.Controls.Add(this.label1);
            this.splitContainer1.Size = new System.Drawing.Size(592, 455);
            this.splitContainer1.SplitterDistance = 299;
            this.splitContainer1.TabIndex = 7;
            // 
            // VariableEditor
            // 
            this.VariableEditor.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
            this.VariableEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.VariableEditor.IntelliSenseChars = ".";
            this.VariableEditor.Lines = new string[] {
        "textEditorControl1"};
            this.VariableEditor.Location = new System.Drawing.Point(0, 13);
            this.VariableEditor.Name = "VariableEditor";
            this.VariableEditor.Size = new System.Drawing.Size(592, 286);
            this.VariableEditor.TabIndex = 8;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Location = new System.Drawing.Point(0, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(101, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Reporting variables:";
            // 
            // FrequencyEditor
            // 
            this.FrequencyEditor.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.FrequencyEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FrequencyEditor.IntelliSenseChars = ".";
            this.FrequencyEditor.Lines = new string[] {
        ""};
            this.FrequencyEditor.Location = new System.Drawing.Point(0, 13);
            this.FrequencyEditor.Name = "FrequencyEditor";
            this.FrequencyEditor.Size = new System.Drawing.Size(592, 139);
            this.FrequencyEditor.TabIndex = 7;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(106, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Reporting frequency:";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(606, 487);
            this.tabControl1.TabIndex = 9;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.GridView);
            this.tabPage1.Controls.Add(this.flowLayoutPanel1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(598, 461);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Data";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.Controls.Add(this.textBox1);
            this.flowLayoutPanel1.Controls.Add(this.lResults);
            this.flowLayoutPanel1.Controls.Add(this.bHome);
            this.flowLayoutPanel1.Controls.Add(this.bStart);
            this.flowLayoutPanel1.Controls.Add(this.bForward);
            this.flowLayoutPanel1.Controls.Add(this.bEnd);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 429);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(592, 29);
            this.flowLayoutPanel1.TabIndex = 7;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(3, 3);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(38, 20);
            this.textBox1.TabIndex = 1;
            this.textBox1.Text = "100";
            // 
            // lResults
            // 
            this.lResults.AutoSize = true;
            this.lResults.Location = new System.Drawing.Point(47, 0);
            this.lResults.Name = "lResults";
            this.lResults.Size = new System.Drawing.Size(87, 13);
            this.lResults.TabIndex = 2;
            this.lResults.Text = "Results per page";
            // 
            // bHome
            // 
            this.bHome.Location = new System.Drawing.Point(140, 3);
            this.bHome.Name = "bHome";
            this.bHome.Size = new System.Drawing.Size(40, 23);
            this.bHome.TabIndex = 3;
            this.bHome.Text = "<<";
            this.bHome.UseVisualStyleBackColor = true;
            // 
            // bStart
            // 
            this.bStart.Location = new System.Drawing.Point(186, 3);
            this.bStart.Name = "bStart";
            this.bStart.Size = new System.Drawing.Size(40, 23);
            this.bStart.TabIndex = 4;
            this.bStart.Text = "<";
            this.bStart.UseVisualStyleBackColor = true;
            // 
            // bForward
            // 
            this.bForward.Location = new System.Drawing.Point(232, 3);
            this.bForward.Name = "bForward";
            this.bForward.Size = new System.Drawing.Size(40, 23);
            this.bForward.TabIndex = 5;
            this.bForward.Text = ">";
            this.bForward.UseVisualStyleBackColor = true;
            // 
            // bEnd
            // 
            this.bEnd.Location = new System.Drawing.Point(278, 3);
            this.bEnd.Name = "bEnd";
            this.bEnd.Size = new System.Drawing.Size(40, 23);
            this.bEnd.TabIndex = 6;
            this.bEnd.Text = ">>";
            this.bEnd.UseVisualStyleBackColor = true;
            // 
            // GridView
            // 
            this.GridView.AutoFilterOn = false;
            this.GridView.DataSource = null;
            this.GridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridView.GetCurrentCell = null;
            this.GridView.Location = new System.Drawing.Point(3, 3);
            this.GridView.ModelName = null;
            this.GridView.Name = "GridView";
            this.GridView.NumericFormat = null;
            this.GridView.ReadOnly = false;
            this.GridView.RowCount = 0;
            this.GridView.Size = new System.Drawing.Size(592, 426);
            this.GridView.TabIndex = 0;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.splitContainer1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(598, 461);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Properties";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // ReportView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl1);
            this.Name = "ReportView";
            this.Size = new System.Drawing.Size(606, 487);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private EditorView FrequencyEditor;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private EditorView VariableEditor;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private GridView GridView;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button bEnd;
        private System.Windows.Forms.Button bForward;
        private System.Windows.Forms.Button bStart;
        private System.Windows.Forms.Button bHome;
        private System.Windows.Forms.Label lResults;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
    }
}