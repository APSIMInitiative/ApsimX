namespace UserInterface.Views
{
    partial class SupplementView
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
            this.tbSulph = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.tbPhos = new System.Windows.Forms.TextBox();
            this.tbADIP2CP = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.tbProtDegrad = new System.Windows.Forms.TextBox();
            this.tbEE = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.tbCP = new System.Windows.Forms.TextBox();
            this.tbME = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.tbDMD = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.tbDM = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label19 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.cbxRoughage = new System.Windows.Forms.CheckBox();
            this.tbAmount = new System.Windows.Forms.TextBox();
            this.lblAmount = new System.Windows.Forms.Label();
            this.tbName = new System.Windows.Forms.TextBox();
            this.lblName = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lvSupps = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnResetAll = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.lbDefaultNames = new System.Windows.Forms.ListBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tbSulph
            // 
            this.tbSulph.Location = new System.Drawing.Point(245, 208);
            this.tbSulph.Name = "tbSulph";
            this.tbSulph.Size = new System.Drawing.Size(37, 20);
            this.tbSulph.TabIndex = 50;
            this.toolTip1.SetToolTip(this.tbSulph, "Enter the fraction of the dry weight which is sulphur for the current supplement");
            this.tbSulph.Validating += new System.ComponentModel.CancelEventHandler(this.RealEditValidator);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(160, 206);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(43, 13);
            this.label15.TabIndex = 49;
            this.label15.Text = "Sulphur";
            // 
            // tbPhos
            // 
            this.tbPhos.Location = new System.Drawing.Point(80, 206);
            this.tbPhos.Name = "tbPhos";
            this.tbPhos.Size = new System.Drawing.Size(37, 20);
            this.tbPhos.TabIndex = 48;
            this.toolTip1.SetToolTip(this.tbPhos, "Enter the fraction of the crude prodein which is insoluble in acid detergent for " +
        "the current supplement");
            this.tbPhos.Validating += new System.ComponentModel.CancelEventHandler(this.RealEditValidator);
            // 
            // tbADIP2CP
            // 
            this.tbADIP2CP.Location = new System.Drawing.Point(80, 180);
            this.tbADIP2CP.Name = "tbADIP2CP";
            this.tbADIP2CP.Size = new System.Drawing.Size(37, 20);
            this.tbADIP2CP.TabIndex = 47;
            this.toolTip1.SetToolTip(this.tbADIP2CP, "Enter the fraction of the crude prodein which is insoluble in acid detergent for " +
        "the current supplement");
            this.tbADIP2CP.Validating += new System.ComponentModel.CancelEventHandler(this.RealEditValidator);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(11, 209);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(63, 13);
            this.label11.TabIndex = 46;
            this.label11.Text = "Phosphorus";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(12, 187);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(49, 13);
            this.label10.TabIndex = 45;
            this.label10.Text = "ADIP:CP";
            // 
            // tbProtDegrad
            // 
            this.tbProtDegrad.Location = new System.Drawing.Point(245, 152);
            this.tbProtDegrad.Name = "tbProtDegrad";
            this.tbProtDegrad.Size = new System.Drawing.Size(37, 20);
            this.tbProtDegrad.TabIndex = 44;
            this.toolTip1.SetToolTip(this.tbProtDegrad, "Enter the fraction of the crude protein in the current supplement which is rumen-" +
        "degradable");
            this.tbProtDegrad.Validating += new System.ComponentModel.CancelEventHandler(this.RealEditValidator);
            // 
            // tbEE
            // 
            this.tbEE.Location = new System.Drawing.Point(245, 128);
            this.tbEE.Name = "tbEE";
            this.tbEE.Size = new System.Drawing.Size(37, 20);
            this.tbEE.TabIndex = 43;
            this.toolTip1.SetToolTip(this.tbEE, "Enter the fraction of the dry weight of the current supplement which is extractab" +
        "le in ether (i.e. fats && oils)");
            this.tbEE.Validating += new System.ComponentModel.CancelEventHandler(this.RealEditValidator);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(160, 131);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(67, 13);
            this.label13.TabIndex = 41;
            this.label13.Text = "Ether extract";
            // 
            // tbCP
            // 
            this.tbCP.Location = new System.Drawing.Point(80, 157);
            this.tbCP.Name = "tbCP";
            this.tbCP.Size = new System.Drawing.Size(37, 20);
            this.tbCP.TabIndex = 40;
            this.toolTip1.SetToolTip(this.tbCP, "Enter the fraction of the dry weight which is crude protein for the current suppl" +
        "ement");
            this.tbCP.Validating += new System.ComponentModel.CancelEventHandler(this.RealEditValidator);
            // 
            // tbME
            // 
            this.tbME.Location = new System.Drawing.Point(80, 131);
            this.tbME.Name = "tbME";
            this.tbME.Size = new System.Drawing.Size(37, 20);
            this.tbME.TabIndex = 39;
            this.toolTip1.SetToolTip(this.tbME, "Enter the quantity of metabolizable energy (ME) per unit weight for the current s" +
        "upplement");
            this.tbME.Validating += new System.ComponentModel.CancelEventHandler(this.RealEditValidator);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(11, 160);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(70, 13);
            this.label9.TabIndex = 38;
            this.label9.Text = "Crude protein";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(11, 131);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(43, 13);
            this.label8.TabIndex = 37;
            this.label8.Text = "ME:DM";
            // 
            // tbDMD
            // 
            this.tbDMD.Location = new System.Drawing.Point(245, 102);
            this.tbDMD.Name = "tbDMD";
            this.tbDMD.Size = new System.Drawing.Size(37, 20);
            this.tbDMD.TabIndex = 36;
            this.toolTip1.SetToolTip(this.tbDMD, "Enter the dry matter digestibility for the current supplement");
            this.tbDMD.Validating += new System.ComponentModel.CancelEventHandler(this.RealEditValidator);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(160, 106);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(79, 13);
            this.label12.TabIndex = 35;
            this.label12.Text = "DM Digestibility";
            // 
            // tbDM
            // 
            this.tbDM.Location = new System.Drawing.Point(80, 103);
            this.tbDM.Name = "tbDM";
            this.tbDM.Size = new System.Drawing.Size(37, 20);
            this.tbDM.TabIndex = 34;
            this.toolTip1.SetToolTip(this.tbDM, "Enter the ratio of dry weight to fresh weight for the current supplement");
            this.tbDM.Validating += new System.ComponentModel.CancelEventHandler(this.RealEditValidator);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label19);
            this.groupBox1.Controls.Add(this.label18);
            this.groupBox1.Controls.Add(this.label17);
            this.groupBox1.Controls.Add(this.label16);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.tbSulph);
            this.groupBox1.Controls.Add(this.label15);
            this.groupBox1.Controls.Add(this.tbPhos);
            this.groupBox1.Controls.Add(this.tbADIP2CP);
            this.groupBox1.Controls.Add(this.label11);
            this.groupBox1.Controls.Add(this.label10);
            this.groupBox1.Controls.Add(this.tbProtDegrad);
            this.groupBox1.Controls.Add(this.tbEE);
            this.groupBox1.Controls.Add(this.label14);
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.tbCP);
            this.groupBox1.Controls.Add(this.tbME);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.tbDMD);
            this.groupBox1.Controls.Add(this.label12);
            this.groupBox1.Controls.Add(this.tbDM);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.cbxRoughage);
            this.groupBox1.Controls.Add(this.tbAmount);
            this.groupBox1.Controls.Add(this.lblAmount);
            this.groupBox1.Controls.Add(this.tbName);
            this.groupBox1.Controls.Add(this.lblName);
            this.groupBox1.Location = new System.Drawing.Point(250, 18);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(314, 244);
            this.groupBox1.TabIndex = 35;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Composition of currrently selected supplement";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(288, 211);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(15, 13);
            this.label19.TabIndex = 61;
            this.label19.Text = "%";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(288, 155);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(15, 13);
            this.label18.TabIndex = 60;
            this.label18.Text = "%";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(288, 130);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(15, 13);
            this.label17.TabIndex = 59;
            this.label17.Text = "%";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(288, 105);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(15, 13);
            this.label16.TabIndex = 58;
            this.label16.Text = "%";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(123, 208);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(15, 13);
            this.label6.TabIndex = 57;
            this.label6.Text = "%";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(123, 183);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(15, 13);
            this.label5.TabIndex = 56;
            this.label5.Text = "%";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(123, 160);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(15, 13);
            this.label4.TabIndex = 55;
            this.label4.Text = "%";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(123, 134);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(15, 13);
            this.label3.TabIndex = 54;
            this.label3.Text = "%";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(123, 106);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(15, 13);
            this.label2.TabIndex = 53;
            this.label2.Text = "%";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(190, 52);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(19, 13);
            this.label1.TabIndex = 52;
            this.label1.Text = "kg";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(160, 155);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(78, 13);
            this.label14.TabIndex = 42;
            this.label14.Text = "Protein Degrad";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 103);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(56, 13);
            this.label7.TabIndex = 33;
            this.label7.Text = "Dry Matter";
            // 
            // cbxRoughage
            // 
            this.cbxRoughage.AutoSize = true;
            this.cbxRoughage.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.cbxRoughage.Location = new System.Drawing.Point(11, 77);
            this.cbxRoughage.Name = "cbxRoughage";
            this.cbxRoughage.Size = new System.Drawing.Size(76, 17);
            this.cbxRoughage.TabIndex = 32;
            this.cbxRoughage.Text = "Roughage";
            this.cbxRoughage.UseVisualStyleBackColor = true;
            this.cbxRoughage.CheckedChanged += new System.EventHandler(this.cbxRoughage_CheckedChanged);
            // 
            // tbAmount
            // 
            this.tbAmount.Location = new System.Drawing.Point(96, 51);
            this.tbAmount.Name = "tbAmount";
            this.tbAmount.Size = new System.Drawing.Size(86, 20);
            this.tbAmount.TabIndex = 17;
            this.toolTip1.SetToolTip(this.tbAmount, "Enter the initial amount of supplement available for feeding out (fresh weight ba" +
        "sis)");
            this.tbAmount.Validating += new System.ComponentModel.CancelEventHandler(this.tbAmount_Validating);
            // 
            // lblAmount
            // 
            this.lblAmount.AutoSize = true;
            this.lblAmount.Location = new System.Drawing.Point(12, 54);
            this.lblAmount.Name = "lblAmount";
            this.lblAmount.Size = new System.Drawing.Size(75, 13);
            this.lblAmount.TabIndex = 16;
            this.lblAmount.Text = "Amount stored";
            // 
            // tbName
            // 
            this.tbName.Location = new System.Drawing.Point(96, 25);
            this.tbName.Name = "tbName";
            this.tbName.Size = new System.Drawing.Size(186, 20);
            this.tbName.TabIndex = 15;
            this.toolTip1.SetToolTip(this.tbName, "Enter the name of the supplement here (it will be converted to lower-case)");
            this.tbName.Validating += new System.ComponentModel.CancelEventHandler(this.tbName_Validating);
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(12, 28);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(35, 13);
            this.lblName.TabIndex = 14;
            this.lblName.Text = "Name";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.lvSupps);
            this.groupBox2.Controls.Add(this.btnResetAll);
            this.groupBox2.Controls.Add(this.btnReset);
            this.groupBox2.Controls.Add(this.btnDelete);
            this.groupBox2.Controls.Add(this.btnAdd);
            this.groupBox2.Location = new System.Drawing.Point(13, 18);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(231, 244);
            this.groupBox2.TabIndex = 36;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Create a list of supplements";
            // 
            // lvSupps
            // 
            this.lvSupps.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.lvSupps.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lvSupps.HideSelection = false;
            this.lvSupps.Location = new System.Drawing.Point(9, 21);
            this.lvSupps.MultiSelect = false;
            this.lvSupps.Name = "lvSupps";
            this.lvSupps.Size = new System.Drawing.Size(216, 184);
            this.lvSupps.TabIndex = 6;
            this.lvSupps.UseCompatibleStateImageBehavior = false;
            this.lvSupps.View = System.Windows.Forms.View.Details;
            this.lvSupps.SelectedIndexChanged += new System.EventHandler(this.lvSupps_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Width = 210;
            // 
            // btnResetAll
            // 
            this.btnResetAll.CausesValidation = false;
            this.btnResetAll.Location = new System.Drawing.Point(160, 211);
            this.btnResetAll.Name = "btnResetAll";
            this.btnResetAll.Size = new System.Drawing.Size(65, 23);
            this.btnResetAll.TabIndex = 5;
            this.btnResetAll.Text = "Reset All";
            this.toolTip1.SetToolTip(this.btnResetAll, "Click this button to reset all supplements to their default composition");
            this.btnResetAll.UseVisualStyleBackColor = true;
            this.btnResetAll.Click += new System.EventHandler(this.btnResetAll_Click);
            // 
            // btnReset
            // 
            this.btnReset.CausesValidation = false;
            this.btnReset.Location = new System.Drawing.Point(113, 211);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(45, 23);
            this.btnReset.TabIndex = 4;
            this.btnReset.Text = "Reset";
            this.toolTip1.SetToolTip(this.btnReset, "Click this button to reset the currently selected supplement to its default compo" +
        "sition");
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.CausesValidation = false;
            this.btnDelete.Location = new System.Drawing.Point(49, 211);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(58, 23);
            this.btnDelete.TabIndex = 3;
            this.btnDelete.Text = "Delete";
            this.toolTip1.SetToolTip(this.btnDelete, "Click this button to delete the selected supplement from the list of available su" +
        "pplements");
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(0, 211);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(43, 23);
            this.btnAdd.TabIndex = 2;
            this.btnAdd.Text = "Add";
            this.toolTip1.SetToolTip(this.btnAdd, "Click this button to add a new supplement type to the list of available supplemen" +
        "ts");
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // lbDefaultNames
            // 
            this.lbDefaultNames.ColumnWidth = 110;
            this.lbDefaultNames.FormattingEnabled = true;
            this.lbDefaultNames.Location = new System.Drawing.Point(3, 268);
            this.lbDefaultNames.MultiColumn = true;
            this.lbDefaultNames.Name = "lbDefaultNames";
            this.lbDefaultNames.Size = new System.Drawing.Size(550, 147);
            this.lbDefaultNames.Sorted = true;
            this.lbDefaultNames.TabIndex = 37;
            this.toolTip1.SetToolTip(this.lbDefaultNames, "Click a supplement to select it from the list");
            this.lbDefaultNames.Visible = false;
            this.lbDefaultNames.Click += new System.EventHandler(this.lbDefaultNames_Click);
            this.lbDefaultNames.Leave += new System.EventHandler(this.lbDefaultNames_Leave);
            // 
            // SupplementView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lbDefaultNames);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "SupplementView";
            this.Size = new System.Drawing.Size(579, 489);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox tbSulph;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox tbPhos;
        private System.Windows.Forms.TextBox tbADIP2CP;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox tbProtDegrad;
        private System.Windows.Forms.TextBox tbEE;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox tbCP;
        private System.Windows.Forms.TextBox tbME;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox tbDMD;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox tbDM;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox cbxRoughage;
        private System.Windows.Forms.TextBox tbAmount;
        private System.Windows.Forms.Label lblAmount;
        private System.Windows.Forms.TextBox tbName;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnResetAll;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListView lvSupps;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ListBox lbDefaultNames;
    }
}
