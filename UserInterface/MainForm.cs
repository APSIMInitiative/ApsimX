using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using UserInterface.Commands;
using UserInterface.Presenters;
using System.Reflection;
using APSIM.Shared.Utilities;

namespace UserInterface
{
    public partial class MainForm : Form
    {
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

            // Adjust font size for MONO.
            if (Environment.OSVersion.Platform != PlatformID.Win32NT &&
                Environment.OSVersion.Platform != PlatformID.Win32Windows)
            {
                this.Font = new Font(this.Font.FontFamily, 10.2F);
            }
            tabbedExplorerView1.Font = this.Font;
            tabbedExplorerView2.Font = this.Font;

            Presenter1 = new TabbedExplorerPresenter();
            Presenter1.Attach(tabbedExplorerView1);
        
            Presenter2 = new TabbedExplorerPresenter();
            Presenter2.Attach(tabbedExplorerView2);
        
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
            if (WindowState == FormWindowState.Normal)
            {
                Utility.Configuration.Settings.MainFormLocation = this.Location;
                Utility.Configuration.Settings.MainFormSize = this.Size;
            }
            else
            {
                Utility.Configuration.Settings.MainFormLocation = RestoreBounds.Location;
                Utility.Configuration.Settings.MainFormSize = RestoreBounds.Size;
            }
            Utility.Configuration.Settings.MainFormWindowState = WindowState;
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
                Location = Utility.Configuration.Settings.MainFormLocation;
                Size = Utility.Configuration.Settings.MainFormSize;
                WindowState = Utility.Configuration.Settings.MainFormWindowState;
            }
            catch (System.Exception)
            {
                WindowState = FormWindowState.Maximized;
            }

            ResumeLayout();

            // Look for version information to display.
            Version version = new Version(Application.ProductVersion);
            if (version.Major == 0)
                this.Text = "APSIM (Custom Build)";
            else
                this.Text = "APSIM " + version.ToString();                

            // Look for a script specified on the command line.
            if (commandLineArguments != null && commandLineArguments.Length > 0)
            {
                if (commandLineArguments[0].EndsWith(".cs"))
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
                else if (commandLineArguments[0].EndsWith(".apsimx"))
                {
                    Presenter1.OpenApsimXFileInTab(commandLineArguments[0]);
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
            Assembly compiledAssembly = ReflectionUtilities.CompileTextToAssembly(code, null);

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
