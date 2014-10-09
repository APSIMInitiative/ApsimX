namespace UserInterface.Views
{
    partial class HTMLView
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
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tooledControl1 = new ModelText.ModelEditControl.TooledControl();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // tooledControl1
            // 
            this.tooledControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tooledControl1.Location = new System.Drawing.Point(0, 0);
            this.tooledControl1.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.tooledControl1.Name = "tooledControl1";
            this.tooledControl1.Size = new System.Drawing.Size(847, 620);
            this.tooledControl1.TabIndex = 2;
            this.tooledControl1.Leave += new System.EventHandler(this.richTextBox1_Leave);
            // 
            // HTMLView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tooledControl1);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "HTMLView";
            this.Size = new System.Drawing.Size(847, 620);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private ModelText.ModelEditControl.TooledControl tooledControl1;
    }
}
