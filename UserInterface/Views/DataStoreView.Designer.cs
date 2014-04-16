namespace UserInterface.Views
{
    partial class DataStoreView
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
            this.TableList = new System.Windows.Forms.ListView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.CreateButton = new System.Windows.Forms.Button();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.GridView = new Views.GridView();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // TableList
            // 
            this.TableList.Dock = System.Windows.Forms.DockStyle.Top;
            this.TableList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.TableList.HideSelection = false;
            this.TableList.Location = new System.Drawing.Point(0, 39);
            this.TableList.MultiSelect = false;
            this.TableList.Name = "TableList";
            this.TableList.ShowGroups = false;
            this.TableList.Size = new System.Drawing.Size(465, 97);
            this.TableList.TabIndex = 1;
            this.TableList.UseCompatibleStateImageBehavior = false;
            this.TableList.View = System.Windows.Forms.View.List;
            this.TableList.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.OnTableSelectedInGrid);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.CreateButton);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(465, 39);
            this.panel1.TabIndex = 3;
            // 
            // CreateButton
            // 
            this.CreateButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.CreateButton.AutoSize = true;
            this.CreateButton.Location = new System.Drawing.Point(3, 3);
            this.CreateButton.Name = "CreateButton";
            this.CreateButton.Size = new System.Drawing.Size(125, 23);
            this.CreateButton.TabIndex = 4;
            this.CreateButton.Text = "Write output file now";
            this.CreateButton.UseVisualStyleBackColor = true;
            this.CreateButton.Click += new System.EventHandler(this.OnCreateButtonClick);
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter1.Location = new System.Drawing.Point(0, 136);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(465, 3);
            this.splitter1.TabIndex = 4;
            this.splitter1.TabStop = false;
            // 
            // GridView
            // 
            this.GridView.DataSource = null;
            this.GridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridView.Location = new System.Drawing.Point(0, 139);
            this.GridView.Name = "GridView";
            this.GridView.ReadOnly = false;
            this.GridView.RowCount = 0;
            this.GridView.Size = new System.Drawing.Size(465, 349);
            this.GridView.TabIndex = 5;
            // 
            // DataStoreView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.GridView);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.TableList);
            this.Controls.Add(this.panel1);
            this.Name = "DataStoreView";
            this.Size = new System.Drawing.Size(465, 488);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView TableList;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button CreateButton;
        private System.Windows.Forms.Splitter splitter1;
        private GridView GridView;
    }
}
