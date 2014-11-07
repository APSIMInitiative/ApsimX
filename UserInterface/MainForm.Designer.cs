using UserInterface.Views;
namespace UserInterface
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;


        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.SplitContainer = new System.Windows.Forms.SplitContainer();
            this.tabbedExplorerView1 = new TabbedExplorerView();
            this.tabbedExplorerView2 = new TabbedExplorerView();
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer)).BeginInit();
            this.SplitContainer.Panel1.SuspendLayout();
            this.SplitContainer.Panel2.SuspendLayout();
            this.SplitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // SplitContainer
            // 
            this.SplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SplitContainer.Location = new System.Drawing.Point(0, 0);
            this.SplitContainer.Margin = new System.Windows.Forms.Padding(4);
            this.SplitContainer.Name = "SplitContainer";
            // 
            // SplitContainer.Panel1
            // 
            this.SplitContainer.Panel1.Controls.Add(this.tabbedExplorerView1);
            // 
            // SplitContainer.Panel2
            // 
            this.SplitContainer.Panel2.Controls.Add(this.tabbedExplorerView2);
            this.SplitContainer.Size = new System.Drawing.Size(522, 476);
            this.SplitContainer.SplitterDistance = 265;
            this.SplitContainer.TabIndex = 1;
            // 
            // tabbedExplorerView1
            // 
            this.tabbedExplorerView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabbedExplorerView1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabbedExplorerView1.Location = new System.Drawing.Point(0, 0);
            this.tabbedExplorerView1.Margin = new System.Windows.Forms.Padding(4);
            this.tabbedExplorerView1.Name = "tabbedExplorerView1";
            this.tabbedExplorerView1.Size = new System.Drawing.Size(265, 476);
            this.tabbedExplorerView1.TabIndex = 0;
            // 
            // tabbedExplorerView2
            // 
            this.tabbedExplorerView2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabbedExplorerView2.Location = new System.Drawing.Point(0, 0);
            this.tabbedExplorerView2.Margin = new System.Windows.Forms.Padding(4);
            this.tabbedExplorerView2.Name = "tabbedExplorerView2";
            this.tabbedExplorerView2.Size = new System.Drawing.Size(253, 476);
            this.tabbedExplorerView2.TabIndex = 1;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(522, 476);
            this.Controls.Add(this.SplitContainer);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.SystemColors.WindowText;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(1, 4, 1, 4);
            this.Name = "MainForm";
            this.Text = "APSIM";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnClosing);
            this.Load += new System.EventHandler(this.OnLoad);
            this.SplitContainer.Panel1.ResumeLayout(false);
            this.SplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer)).EndInit();
            this.SplitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer SplitContainer;
        private Views.TabbedExplorerView tabbedExplorerView1;
        private Views.TabbedExplorerView tabbedExplorerView2;









    }
}