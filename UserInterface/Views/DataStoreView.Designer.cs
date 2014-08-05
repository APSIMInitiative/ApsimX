// -----------------------------------------------------------------------
// <copyright file="DataStoreView.Designer.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    /// <summary>
    /// A data store view
    /// </summary>
    public partial class DataStoreView
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// List of table names.
        /// </summary>
        private System.Windows.Forms.ListView listBox1;

        /// <summary>
        /// Top panel
        /// </summary>
        private System.Windows.Forms.Panel panel1;

        /// <summary>
        /// A create button
        /// </summary>
        private System.Windows.Forms.Button createButton;

        /// <summary>
        /// A splitter
        /// </summary>
        private System.Windows.Forms.Splitter splitter1;

        /// <summary>
        /// An output grid view.
        /// </summary>
        private GridView gridView;

        /// <summary>
        /// The main tab control.
        /// </summary>
        private System.Windows.Forms.TabControl tabControl1;

        /// <summary>
        /// First tab page
        /// </summary>
        private System.Windows.Forms.TabPage tabPage1;

        /// <summary>
        /// Second tab page.
        /// </summary>
        private System.Windows.Forms.TabPage tabPage2;

        /// <summary>
        /// Summary html viewer
        /// </summary>
        private HTMLView htmlView1;

        /// <summary>
        /// Second splitter
        /// </summary>
        private System.Windows.Forms.Splitter splitter2;

        /// <summary>
        /// Simulation list view.
        /// </summary>
        private System.Windows.Forms.ListView listView2;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataStoreView));
            this.listBox1 = new System.Windows.Forms.ListView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.createButton = new System.Windows.Forms.Button();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.gridView = new GridView();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.htmlView1 = new HTMLView();
            this.splitter2 = new System.Windows.Forms.Splitter();
            this.listView2 = new System.Windows.Forms.ListView();
            this.panel1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBox1
            // 
            this.listBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.listBox1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listBox1.HideSelection = false;
            this.listBox1.Location = new System.Drawing.Point(3, 42);
            this.listBox1.MultiSelect = false;
            this.listBox1.Name = "listBox1";
            this.listBox1.ShowGroups = false;
            this.listBox1.Size = new System.Drawing.Size(451, 97);
            this.listBox1.TabIndex = 1;
            this.listBox1.UseCompatibleStateImageBehavior = false;
            this.listBox1.View = System.Windows.Forms.View.List;
            this.listBox1.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.OnTableSelectedInGrid);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.checkBox1);
            this.panel1.Controls.Add(this.createButton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(451, 39);
            this.panel1.TabIndex = 3;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(13, 10);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(139, 17);
            this.checkBox1.TabIndex = 5;
            this.checkBox1.Text = "Auto export to text files?";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.OnAutoExportCheckedChanged);
            // 
            // createButton
            // 
            this.createButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.createButton.AutoSize = true;
            this.createButton.Location = new System.Drawing.Point(323, 6);
            this.createButton.Name = "createButton";
            this.createButton.Size = new System.Drawing.Size(125, 23);
            this.createButton.TabIndex = 4;
            this.createButton.Text = "Export now";
            this.createButton.UseVisualStyleBackColor = true;
            this.createButton.Click += new System.EventHandler(this.OnExportButtonClick);
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter1.Location = new System.Drawing.Point(3, 139);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(451, 3);
            this.splitter1.TabIndex = 4;
            this.splitter1.TabStop = false;
            // 
            // gridView
            // 
            this.gridView.AutoFilterOn = false;
            this.gridView.DataSource = null;
            this.gridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridView.GetCurrentCell = null;
            this.gridView.Location = new System.Drawing.Point(3, 142);
            this.gridView.Name = "gridView";
            this.gridView.NumericFormat = null;
            this.gridView.ReadOnly = false;
            this.gridView.RowCount = 0;
            this.gridView.Size = new System.Drawing.Size(451, 317);
            this.gridView.TabIndex = 5;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(465, 488);
            this.tabControl1.TabIndex = 6;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.gridView);
            this.tabPage1.Controls.Add(this.splitter1);
            this.tabPage1.Controls.Add(this.listBox1);
            this.tabPage1.Controls.Add(this.panel1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(457, 462);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Output";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.htmlView1);
            this.tabPage2.Controls.Add(this.splitter2);
            this.tabPage2.Controls.Add(this.listView2);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(457, 462);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Summary";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // htmlView1
            // 
            this.htmlView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.htmlView1.LabelText = "";
            this.htmlView1.Location = new System.Drawing.Point(3, 103);
            this.htmlView1.MemoText = resources.GetString("htmlView1.MemoText");
            this.htmlView1.Name = "htmlView1";
            this.htmlView1.ReadOnly = false;
            this.htmlView1.Size = new System.Drawing.Size(451, 356);
            this.htmlView1.TabIndex = 2;
            // 
            // splitter2
            // 
            this.splitter2.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter2.Location = new System.Drawing.Point(3, 100);
            this.splitter2.Name = "splitter2";
            this.splitter2.Size = new System.Drawing.Size(451, 3);
            this.splitter2.TabIndex = 1;
            this.splitter2.TabStop = false;
            // 
            // listView2
            // 
            this.listView2.Dock = System.Windows.Forms.DockStyle.Top;
            this.listView2.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listView2.HideSelection = false;
            this.listView2.Location = new System.Drawing.Point(3, 3);
            this.listView2.Name = "listView2";
            this.listView2.ShowGroups = false;
            this.listView2.Size = new System.Drawing.Size(451, 97);
            this.listView2.TabIndex = 3;
            this.listView2.UseCompatibleStateImageBehavior = false;
            this.listView2.View = System.Windows.Forms.View.List;
            this.listView2.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.OnSimulationSelectedInView);
            // 
            // DataStoreView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl1);
            this.Name = "DataStoreView";
            this.Size = new System.Drawing.Size(465, 488);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private System.Windows.Forms.CheckBox checkBox1;
    }
}
