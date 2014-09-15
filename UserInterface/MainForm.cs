using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using UserInterface.Commands;
using UserInterface.Presenters;
using System.Reflection;

namespace UserInterface
{
    public partial class MainForm : Form
    {
        private Utility.Configuration Configuration;
        private TabbedExplorerPresenter Presenter1;
        private TabbedExplorerPresenter Presenter2;
        private string[] commandLineArguments;

        /// <summary>
        /// The error message will be set if an error results from a startup script.
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public MainForm(string[] args)
        {
            InitializeComponent();
            Application.EnableVisualStyles();

            Configuration = new Utility.Configuration();
            
            Presenter1 = new TabbedExplorerPresenter();
            Presenter1.Attach(tabbedExplorerView1);
            Presenter1.config = Configuration;

            Presenter2 = new TabbedExplorerPresenter();
            Presenter2.Attach(tabbedExplorerView2);
            Presenter2.config = Configuration;

            SplitContainer.Panel2Collapsed = true;
            commandLineArguments = args;
        }

        public void ToggleSecondExplorerViewVisible()
        {
            SplitContainer.Panel2Collapsed = !SplitContainer.Panel2Collapsed;
        }
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            Configuration.Settings.MainFormLocation = Location; 
            Configuration.Settings.MainFormSize = Size;
            Configuration.Settings.MainFormWindowState = WindowState;
            //store settings on closure
            Configuration.Save();

            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        
        /// <summary>
        /// Read the previous main form sizing values from the configuration file.
        /// </summary>
        private void OnLoad(object sender, EventArgs e)
        {
            SuspendLayout();

            try
            {
                Location = Configuration.Settings.MainFormLocation;
                Size = Configuration.Settings.MainFormSize;
                WindowState = Configuration.Settings.MainFormWindowState;
            }
            catch (System.Exception)
            {
                WindowState = FormWindowState.Maximized;
            }

            ResumeLayout();

            // Look for a script specified on the command line.
            if (commandLineArguments != null && commandLineArguments.Length > 0 &&
                commandLineArguments[0].EndsWith(".cs"))
            {
                try
                {

                    ProcessStartupScript(commandLineArguments[0]);
                }
                catch (Exception err)
                {
                    ErrorMessage = err.Message;
                    if (err.InnerException != null)
                        ErrorMessage += "\r\n" + err.InnerException.Message;
                    ErrorMessage += "\r\n" + err.StackTrace;
                    DialogResult = System.Windows.Forms.DialogResult.Cancel;
                    Close();
                }
            }
        }

        /// <summary>
        /// User wants to close the form.
        /// </summary>
        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == System.Windows.Forms.DialogResult.Cancel)
                e.Cancel = false;
            else
                e.Cancel = !Presenter1.AllowClose() || !Presenter2.AllowClose();
        }

        /// <summary>
        /// User has specified a startup script - execute it.
        /// </summary>
        private void ProcessStartupScript(string fileName)
        {
            StreamReader reader = new StreamReader(fileName);
            string code = reader.ReadToEnd();
            reader.Close();
            Assembly compiledAssembly = Utility.Reflection.CompileTextToAssembly(code, null);

            // Get the script 'Type' from the compiled assembly.
            Type scriptType = compiledAssembly.GetType("Script");
            if (scriptType == null)
                throw new Exception("Cannot find a public class called 'Script'");

            // Look for a method called Execute
            MethodInfo executeMethod = scriptType.GetMethod("Execute");
            if (executeMethod == null)
                throw new Exception("Cannot find a method Script.Execute");

            // Create a new script model.
            object script = compiledAssembly.CreateInstance("Script");
            
            // Call Execute on our newly created script instance.
            object[] arguments = new object[] { Presenter1 };
            executeMethod.Invoke(script, arguments);
        }

    }
     
}
