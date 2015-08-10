namespace UserInterface.Forms
{
    partial class UpgradeForm
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem(new string[] {
            "2015-06-07",
            "text"}, -1);
            this.label1 = new System.Windows.Forms.Label();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.viewMoreDetailToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.upgradeToThisVersionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label2 = new System.Windows.Forms.Label();
            this.firstNameBox = new System.Windows.Forms.TextBox();
            this.lastNameBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.organisationBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.emailBox = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.stateBox = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cityBox = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.address2Box = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.address1Box = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.postcodeBox = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.countryBox = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.htmlView1 = new Views.HTMLView();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(706, 46);
            this.label1.TabIndex = 1;
            this.label1.Text = "You are currently at version xxx. Newer versions are listed below.";
            // 
            // listView1
            // 
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.listView1.ContextMenuStrip = this.contextMenuStrip1;
            this.listView1.FullRowSelect = true;
            this.listView1.GridLines = true;
            this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listView1.HideSelection = false;
            this.listView1.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem2});
            this.listView1.Location = new System.Drawing.Point(13, 49);
            this.listView1.Name = "listView1";
            this.listView1.Scrollable = false;
            this.listView1.ShowGroups = false;
            this.listView1.Size = new System.Drawing.Size(595, 217);
            this.listView1.TabIndex = 3;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Version";
            this.columnHeader1.Width = 150;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Description";
            this.columnHeader2.Width = 583;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.viewMoreDetailToolStripMenuItem,
            this.upgradeToThisVersionToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(239, 56);
            // 
            // viewMoreDetailToolStripMenuItem
            // 
            this.viewMoreDetailToolStripMenuItem.Name = "viewMoreDetailToolStripMenuItem";
            this.viewMoreDetailToolStripMenuItem.Size = new System.Drawing.Size(238, 26);
            this.viewMoreDetailToolStripMenuItem.Text = "View more detail";
            this.viewMoreDetailToolStripMenuItem.Click += new System.EventHandler(this.OnViewMoreDetail);
            // 
            // upgradeToThisVersionToolStripMenuItem
            // 
            this.upgradeToThisVersionToolStripMenuItem.Name = "upgradeToThisVersionToolStripMenuItem";
            this.upgradeToThisVersionToolStripMenuItem.Size = new System.Drawing.Size(238, 26);
            this.upgradeToThisVersionToolStripMenuItem.Text = "Upgrade to this version";
            this.upgradeToThisVersionToolStripMenuItem.Click += new System.EventHandler(this.OnUpgrade);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 280);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(87, 17);
            this.label2.TabIndex = 6;
            this.label2.Text = "First name *:";
            // 
            // firstNameBox
            // 
            this.firstNameBox.Location = new System.Drawing.Point(111, 280);
            this.firstNameBox.Name = "firstNameBox";
            this.firstNameBox.Size = new System.Drawing.Size(142, 22);
            this.firstNameBox.TabIndex = 7;
            // 
            // lastNameBox
            // 
            this.lastNameBox.Location = new System.Drawing.Point(111, 308);
            this.lastNameBox.Name = "lastNameBox";
            this.lastNameBox.Size = new System.Drawing.Size(142, 22);
            this.lastNameBox.TabIndex = 9;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 308);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(87, 17);
            this.label3.TabIndex = 8;
            this.label3.Text = "Last name *:";
            // 
            // organisationBox
            // 
            this.organisationBox.Location = new System.Drawing.Point(111, 336);
            this.organisationBox.Name = "organisationBox";
            this.organisationBox.Size = new System.Drawing.Size(142, 22);
            this.organisationBox.TabIndex = 11;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 336);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(93, 17);
            this.label4.TabIndex = 10;
            this.label4.Text = "Organisation:";
            // 
            // emailBox
            // 
            this.emailBox.Location = new System.Drawing.Point(111, 364);
            this.emailBox.Name = "emailBox";
            this.emailBox.Size = new System.Drawing.Size(142, 22);
            this.emailBox.TabIndex = 13;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 364);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(55, 17);
            this.label5.TabIndex = 12;
            this.label5.Text = "Email *:";
            // 
            // stateBox
            // 
            this.stateBox.Location = new System.Drawing.Point(614, 336);
            this.stateBox.Name = "stateBox";
            this.stateBox.Size = new System.Drawing.Size(91, 22);
            this.stateBox.TabIndex = 21;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(537, 339);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(45, 17);
            this.label6.TabIndex = 20;
            this.label6.Text = "State:";
            // 
            // cityBox
            // 
            this.cityBox.Location = new System.Drawing.Point(380, 336);
            this.cityBox.Name = "cityBox";
            this.cityBox.Size = new System.Drawing.Size(113, 22);
            this.cityBox.TabIndex = 19;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(281, 336);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(35, 17);
            this.label7.TabIndex = 18;
            this.label7.Text = "City:";
            // 
            // address2Box
            // 
            this.address2Box.Location = new System.Drawing.Point(380, 308);
            this.address2Box.Name = "address2Box";
            this.address2Box.Size = new System.Drawing.Size(325, 22);
            this.address2Box.TabIndex = 17;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(281, 308);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(72, 17);
            this.label8.TabIndex = 16;
            this.label8.Text = "Address2:";
            // 
            // address1Box
            // 
            this.address1Box.Location = new System.Drawing.Point(380, 280);
            this.address1Box.Name = "address1Box";
            this.address1Box.Size = new System.Drawing.Size(325, 22);
            this.address1Box.TabIndex = 15;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(281, 280);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(72, 17);
            this.label9.TabIndex = 14;
            this.label9.Text = "Address1:";
            // 
            // postcodeBox
            // 
            this.postcodeBox.Location = new System.Drawing.Point(614, 364);
            this.postcodeBox.Name = "postcodeBox";
            this.postcodeBox.Size = new System.Drawing.Size(91, 22);
            this.postcodeBox.TabIndex = 23;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(537, 367);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(71, 17);
            this.label10.TabIndex = 22;
            this.label10.Text = "Postcode:";
            // 
            // countryBox
            // 
            this.countryBox.Location = new System.Drawing.Point(380, 367);
            this.countryBox.Name = "countryBox";
            this.countryBox.Size = new System.Drawing.Size(113, 22);
            this.countryBox.TabIndex = 25;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(281, 367);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(70, 17);
            this.label11.TabIndex = 24;
            this.label11.Text = "Country *:";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(15, 412);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(376, 21);
            this.checkBox1.TabIndex = 27;
            this.checkBox1.Text = "Do you agree to the terms of the APSIM license below?";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // htmlView1
            // 
            this.htmlView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.htmlView1.Location = new System.Drawing.Point(13, 440);
            this.htmlView1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.htmlView1.Name = "htmlView1";
            this.htmlView1.Size = new System.Drawing.Size(692, 261);
            this.htmlView1.TabIndex = 26;
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Location = new System.Drawing.Point(630, 49);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 51);
            this.button1.TabIndex = 28;
            this.button1.Text = "Upgrade";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.OnUpgrade);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(630, 113);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 51);
            this.button2.TabIndex = 29;
            this.button2.Text = "View detail";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.OnViewMoreDetail);
            // 
            // UpgradeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(719, 714);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.htmlView1);
            this.Controls.Add(this.countryBox);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.postcodeBox);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.stateBox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cityBox);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.address2Box);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.address1Box);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.emailBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.organisationBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lastNameBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.firstNameBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.label1);
            this.Name = "UpgradeForm";
            this.Text = "APSIM Upgrade Form";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing);
            this.Shown += new System.EventHandler(this.OnShown);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem viewMoreDetailToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem upgradeToThisVersionToolStripMenuItem;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox firstNameBox;
        private System.Windows.Forms.TextBox lastNameBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox organisationBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox emailBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox stateBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox cityBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox address2Box;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox address1Box;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox postcodeBox;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox countryBox;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckBox checkBox1;
        private Views.HTMLView htmlView1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}