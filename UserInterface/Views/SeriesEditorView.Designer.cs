namespace UserInterface.Views
{
    partial class SeriesEditorView
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
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.comboBox3 = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.comboBox4 = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.button1 = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.checkBox5 = new System.Windows.Forms.CheckBox();
            this.xComboBox = new System.Windows.Forms.ComboBox();
            this.yComboBox = new System.Windows.Forms.ComboBox();
            this.x2ComboBox = new System.Windows.Forms.ComboBox();
            this.y2ComboBox = new System.Windows.Forms.ComboBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.checkBox4 = new System.Windows.Forms.CheckBox();
            this.cumulativeCheckBox = new System.Windows.Forms.CheckBox();
            this.checkBox6 = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(88, 52);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(21, 17);
            this.label2.TabIndex = 4;
            this.label2.Text = "X:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(88, 84);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(21, 17);
            this.label3.TabIndex = 6;
            this.label3.Text = "Y:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(80, 116);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(29, 17);
            this.label4.TabIndex = 8;
            this.label4.Text = "X2:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(80, 148);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(29, 17);
            this.label5.TabIndex = 10;
            this.label5.Text = "Y2:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(65, 186);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(44, 17);
            this.label6.TabIndex = 11;
            this.label6.Text = "Type:";
            // 
            // comboBox2
            // 
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Items.AddRange(new object[] {
            "Scatter",
            "Bar",
            "Area"});
            this.comboBox2.Location = new System.Drawing.Point(119, 182);
            this.comboBox2.Margin = new System.Windows.Forms.Padding(4);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(113, 24);
            this.comboBox2.TabIndex = 12;
            this.comboBox2.Text = "Scatter";
            this.comboBox2.TextChanged += new System.EventHandler(this.OnComboBox2Changed);
            // 
            // comboBox3
            // 
            this.comboBox3.FormattingEnabled = true;
            this.comboBox3.Items.AddRange(new object[] {
            "Solid",
            "Dash",
            "Dot",
            "DashDot",
            "None"});
            this.comboBox3.Location = new System.Drawing.Point(120, 208);
            this.comboBox3.Margin = new System.Windows.Forms.Padding(4);
            this.comboBox3.Name = "comboBox3";
            this.comboBox3.Size = new System.Drawing.Size(112, 24);
            this.comboBox3.TabIndex = 14;
            this.comboBox3.Text = "Solid";
            this.comboBox3.TextChanged += new System.EventHandler(this.OnComboBox3Changed);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(40, 211);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(70, 17);
            this.label7.TabIndex = 13;
            this.label7.Text = "Line type:";
            // 
            // comboBox4
            // 
            this.comboBox4.FormattingEnabled = true;
            this.comboBox4.Items.AddRange(new object[] {
            "None",
            "FilledCircle",
            "FilledDiamond",
            "FilledSquare",
            "FilledTriangle",
            "Circle",
            "Diamond",
            "Square",
            "Triangle",
            "Cross",
            "Plus",
            "Star"});
            this.comboBox4.Location = new System.Drawing.Point(120, 235);
            this.comboBox4.Margin = new System.Windows.Forms.Padding(4);
            this.comboBox4.Name = "comboBox4";
            this.comboBox4.Size = new System.Drawing.Size(112, 24);
            this.comboBox4.TabIndex = 16;
            this.comboBox4.Text = "None";
            this.comboBox4.TextChanged += new System.EventHandler(this.OnComboBox4Changed);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(25, 238);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(87, 17);
            this.label8.TabIndex = 15;
            this.label8.Text = "Marker type:";
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.button1.Location = new System.Drawing.Point(265, 203);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(36, 28);
            this.button1.TabIndex = 17;
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.OnButton1Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(391, 52);
            this.checkBox1.Margin = new System.Windows.Forms.Padding(4);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(70, 21);
            this.checkBox1.TabIndex = 18;
            this.checkBox1.Text = "on top";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.OnCheckBox1Changed);
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Location = new System.Drawing.Point(391, 85);
            this.checkBox2.Margin = new System.Windows.Forms.Padding(4);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(78, 21);
            this.checkBox2.TabIndex = 19;
            this.checkBox2.Text = "on right";
            this.checkBox2.UseVisualStyleBackColor = true;
            this.checkBox2.CheckedChanged += new System.EventHandler(this.OnCheckBox2Changed);
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Location = new System.Drawing.Point(335, 175);
            this.checkBox3.Margin = new System.Windows.Forms.Padding(4);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(154, 21);
            this.checkBox3.TabIndex = 21;
            this.checkBox3.Text = "Overall regression?";
            this.checkBox3.UseVisualStyleBackColor = true;
            this.checkBox3.CheckedChanged += new System.EventHandler(this.OnCheckBox3Changed);
            // 
            // checkBox5
            // 
            this.checkBox5.AutoSize = true;
            this.checkBox5.Location = new System.Drawing.Point(335, 217);
            this.checkBox5.Margin = new System.Windows.Forms.Padding(4);
            this.checkBox5.Name = "checkBox5";
            this.checkBox5.Size = new System.Drawing.Size(126, 21);
            this.checkBox5.TabIndex = 26;
            this.checkBox5.Text = "Show in legend";
            this.checkBox5.UseVisualStyleBackColor = true;
            this.checkBox5.CheckedChanged += new System.EventHandler(this.OnCheckBox5Changed);
            // 
            // xComboBox
            // 
            this.xComboBox.FormattingEnabled = true;
            this.xComboBox.Location = new System.Drawing.Point(119, 48);
            this.xComboBox.Margin = new System.Windows.Forms.Padding(4);
            this.xComboBox.Name = "xComboBox";
            this.xComboBox.Size = new System.Drawing.Size(263, 24);
            this.xComboBox.TabIndex = 29;
            this.xComboBox.TextChanged += new System.EventHandler(this.xComboBox_TextChanged);
            // 
            // yComboBox
            // 
            this.yComboBox.FormattingEnabled = true;
            this.yComboBox.Location = new System.Drawing.Point(119, 80);
            this.yComboBox.Margin = new System.Windows.Forms.Padding(4);
            this.yComboBox.Name = "yComboBox";
            this.yComboBox.Size = new System.Drawing.Size(263, 24);
            this.yComboBox.TabIndex = 30;
            this.yComboBox.TextChanged += new System.EventHandler(this.yComboBox_TextChanged);
            // 
            // x2ComboBox
            // 
            this.x2ComboBox.FormattingEnabled = true;
            this.x2ComboBox.Location = new System.Drawing.Point(119, 112);
            this.x2ComboBox.Margin = new System.Windows.Forms.Padding(4);
            this.x2ComboBox.Name = "x2ComboBox";
            this.x2ComboBox.Size = new System.Drawing.Size(263, 24);
            this.x2ComboBox.TabIndex = 31;
            this.x2ComboBox.TextChanged += new System.EventHandler(this.x2ComboBox_TextChanged);
            // 
            // y2ComboBox
            // 
            this.y2ComboBox.FormattingEnabled = true;
            this.y2ComboBox.Location = new System.Drawing.Point(119, 144);
            this.y2ComboBox.Margin = new System.Windows.Forms.Padding(4);
            this.y2ComboBox.Name = "y2ComboBox";
            this.y2ComboBox.Size = new System.Drawing.Size(263, 24);
            this.y2ComboBox.TabIndex = 32;
            this.y2ComboBox.TextChanged += new System.EventHandler(this.y2ComboBox_TextChanged);
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(119, 14);
            this.comboBox1.Margin = new System.Windows.Forms.Padding(4);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(263, 24);
            this.comboBox1.TabIndex = 33;
            this.comboBox1.TextChanged += new System.EventHandler(this.OnComboBox1Changed);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 17);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(89, 17);
            this.label1.TabIndex = 34;
            this.label1.Text = "Data source:";
            // 
            // checkBox4
            // 
            this.checkBox4.AutoSize = true;
            this.checkBox4.Location = new System.Drawing.Point(335, 196);
            this.checkBox4.Margin = new System.Windows.Forms.Padding(4);
            this.checkBox4.Name = "checkBox4";
            this.checkBox4.Size = new System.Drawing.Size(208, 21);
            this.checkBox4.TabIndex = 35;
            this.checkBox4.Text = "Regression for each series?";
            this.checkBox4.UseVisualStyleBackColor = true;
            this.checkBox4.CheckedChanged += new System.EventHandler(this.OnCheckBox4Changed);
            // 
            // cumulativeCheckBox
            // 
            this.cumulativeCheckBox.AutoSize = true;
            this.cumulativeCheckBox.Location = new System.Drawing.Point(477, 85);
            this.cumulativeCheckBox.Margin = new System.Windows.Forms.Padding(4);
            this.cumulativeCheckBox.Name = "cumulativeCheckBox";
            this.cumulativeCheckBox.Size = new System.Drawing.Size(105, 21);
            this.cumulativeCheckBox.TabIndex = 36;
            this.cumulativeCheckBox.Text = "cumulative?";
            this.cumulativeCheckBox.UseVisualStyleBackColor = true;
            this.cumulativeCheckBox.CheckedChanged += new System.EventHandler(this.OnCheckedChanged);
            // 
            // checkBox6
            // 
            this.checkBox6.AutoSize = true;
            this.checkBox6.Location = new System.Drawing.Point(335, 238);
            this.checkBox6.Margin = new System.Windows.Forms.Padding(4);
            this.checkBox6.Name = "checkBox6";
            this.checkBox6.Size = new System.Drawing.Size(187, 21);
            this.checkBox6.TabIndex = 37;
            this.checkBox6.Text = "Include in documentation";
            this.checkBox6.UseVisualStyleBackColor = true;
            this.checkBox6.CheckedChanged += new System.EventHandler(this.OnIncludeInDocumentationChanged);
            // 
            // SeriesEditorView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.checkBox6);
            this.Controls.Add(this.cumulativeCheckBox);
            this.Controls.Add(this.checkBox4);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.y2ComboBox);
            this.Controls.Add(this.x2ComboBox);
            this.Controls.Add(this.yComboBox);
            this.Controls.Add(this.xComboBox);
            this.Controls.Add(this.checkBox5);
            this.Controls.Add(this.checkBox3);
            this.Controls.Add(this.checkBox2);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.comboBox4);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.comboBox3);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.comboBox2);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "SeriesEditorView";
            this.Size = new System.Drawing.Size(608, 271);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.ComboBox comboBox3;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox comboBox4;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.CheckBox checkBox5;
        private System.Windows.Forms.ComboBox xComboBox;
        private System.Windows.Forms.ComboBox yComboBox;
        private System.Windows.Forms.ComboBox x2ComboBox;
        private System.Windows.Forms.ComboBox y2ComboBox;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox checkBox4;
        private System.Windows.Forms.CheckBox cumulativeCheckBox;
        private System.Windows.Forms.CheckBox checkBox6;
    }
}
