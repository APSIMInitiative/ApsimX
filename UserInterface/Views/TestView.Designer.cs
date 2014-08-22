namespace UserInterface.Views
{
    partial class TestView
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
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.gridView1 = new GridView();
            this.editorView1 = new EditorView();
            this.SuspendLayout();
            // 
            // comboBox1
            // 
            this.comboBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(94, 16);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(544, 21);
            this.comboBox1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Table name:";
            // 
            // gridView1
            // 
            this.gridView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridView1.AutoFilterOn = false;
            this.gridView1.DataSource = null;
            this.gridView1.GetCurrentCell = null;
            this.gridView1.Location = new System.Drawing.Point(16, 43);
            this.gridView1.Name = "gridView1";
            this.gridView1.NumericFormat = null;
            this.gridView1.ReadOnly = false;
            this.gridView1.RowCount = 0;
            this.gridView1.Size = new System.Drawing.Size(622, 173);
            this.gridView1.TabIndex = 2;
            // 
            // editorView1
            // 
            this.editorView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.editorView1.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
            this.editorView1.IntelliSenseChars = ".";
            this.editorView1.Lines = new string[] {
        "textEditorControl1"};
            this.editorView1.Location = new System.Drawing.Point(16, 223);
            this.editorView1.Name = "editorView1";
            this.editorView1.Size = new System.Drawing.Size(622, 255);
            this.editorView1.TabIndex = 3;
            // 
            // TestView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.editorView1);
            this.Controls.Add(this.gridView1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBox1);
            this.Name = "TestView";
            this.Size = new System.Drawing.Size(652, 494);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label1;
        private GridView gridView1;
        private EditorView editorView1;
    }
}
