namespace UserInterface.Views
{
    partial class ManagerView
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
            System.Windows.Forms.TabPage Properties;
            this.Grid = new GridView();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.Script = new System.Windows.Forms.TabPage();
            this.ScriptEditor = new Utility.Editor();
            Properties = new System.Windows.Forms.TabPage();
            Properties.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.Script.SuspendLayout();
            this.SuspendLayout();
            // 
            // Properties
            // 
            Properties.Controls.Add(this.Grid);
            Properties.Location = new System.Drawing.Point(4, 22);
            Properties.Name = "Properties";
            Properties.Padding = new System.Windows.Forms.Padding(3);
            Properties.Size = new System.Drawing.Size(618, 490);
            Properties.TabIndex = 0;
            Properties.Text = "Properties";
            Properties.UseVisualStyleBackColor = true;
            // 
            // Grid
            // 
            this.Grid.DataSource = null;
            this.Grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Grid.Location = new System.Drawing.Point(3, 3);
            this.Grid.Name = "Grid";
            this.Grid.ReadOnly = false;
            this.Grid.RowCount = 0;
            this.Grid.Size = new System.Drawing.Size(612, 484);
            this.Grid.TabIndex = 0;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(Properties);
            this.tabControl1.Controls.Add(this.Script);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(626, 516);
            this.tabControl1.TabIndex = 2;
            // 
            // Script
            // 
            this.Script.Controls.Add(this.ScriptEditor);
            this.Script.Location = new System.Drawing.Point(4, 22);
            this.Script.Name = "Script";
            this.Script.Padding = new System.Windows.Forms.Padding(3);
            this.Script.Size = new System.Drawing.Size(618, 490);
            this.Script.TabIndex = 1;
            this.Script.Text = "Script";
            this.Script.UseVisualStyleBackColor = true;
            // 
            // ScriptEditor
            // 
            this.ScriptEditor.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
            this.ScriptEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ScriptEditor.Font = new System.Drawing.Font("Courier New", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ScriptEditor.Lines = new string[] {
        "",
        ""};
            this.ScriptEditor.Location = new System.Drawing.Point(3, 3);
            this.ScriptEditor.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.ScriptEditor.Name = "ScriptEditor";
            this.ScriptEditor.Size = new System.Drawing.Size(612, 484);
            this.ScriptEditor.TabIndex = 0;
            this.ScriptEditor.ContextItemsNeeded += new System.EventHandler<Utility.Editor.NeedContextItems>(this.OnVariableListNeedItems);
            this.ScriptEditor.TextHasChangedByUser += new System.EventHandler(this.ScriptEditor_TextHasChangedByUser);
            // 
            // ManagerView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl1);
            this.Name = "ManagerView";
            this.Size = new System.Drawing.Size(626, 516);
            Properties.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.Script.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage Script;
        private Utility.Editor ScriptEditor;
        private GridView Grid;

    }
}
