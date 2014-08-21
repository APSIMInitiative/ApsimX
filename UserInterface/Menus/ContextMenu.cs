// -----------------------------------------------------------------------
// <copyright file="ContextMenu.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using Commands;
    using Interfaces;
    using Microsoft.Win32;
    using Models;
    using Models.Core;
    using Models.Factorial;
    using Models.Soils;
    
    /// <summary>
    /// This class contains methods for all context menu items that the ExplorerView exposes to the user.
    /// </summary>
    public class ContextMenu
    {
        /// <summary>
        /// Reference to the ExplorerPresenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenu" /> class.
        /// </summary>
        /// <param name="explorerPresenter">The explorer presenter to work with</param>
        public ContextMenu(ExplorerPresenter explorerPresenter)
        {
            this.explorerPresenter = explorerPresenter;
        }

        /// <summary>
        /// User has clicked Copy
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Copy")]
        public void OnCopyClick(object sender, EventArgs e)
        {
            Model model = this.explorerPresenter.ApsimXFile.Get(this.explorerPresenter.CurrentNodePath) as Model;
            if (model != null)
            {
                // Set the clipboard text.
                System.Windows.Forms.Clipboard.SetText(model.Serialise());
            }
        }

        /// <summary>
        /// User has clicked Paste
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Paste")]
        public void OnPasteClick(object sender, EventArgs e)
        {
            try
            {
                XmlDocument document = new XmlDocument();
                document.LoadXml(System.Windows.Forms.Clipboard.GetText());
                object newModel = Utility.Xml.Deserialise(document.DocumentElement);

                // See if the presenter is happy with this model being added.
                Model parentModel = this.explorerPresenter.ApsimXFile.Get(this.explorerPresenter.CurrentNodePath) as Model;
                AllowDropArgs allowDropArgs = new AllowDropArgs();
                allowDropArgs.NodePath = this.explorerPresenter.CurrentNodePath;
                allowDropArgs.DragObject = new DragObject()
                {
                    NodePath = null,
                    ModelType = newModel.GetType(),
                    Xml = System.Windows.Forms.Clipboard.GetText()
                };
                this.explorerPresenter.OnAllowDrop(null, allowDropArgs);

                // If it is happy then issue an AddModelCommand.
                if (allowDropArgs.Allow)
                {
                    AddModelCommand command = new AddModelCommand(System.Windows.Forms.Clipboard.GetText(), parentModel);
                    this.explorerPresenter.CommandHistory.Add(command, true);
                }
            }
            catch (Exception exception)
            {
                this.explorerPresenter.ShowMessage(exception.Message, DataStore.ErrorLevel.Error);
            }
        }

        /// <summary>
        /// User has clicked Delete
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Delete")]
        public void OnDeleteClick(object sender, EventArgs e)
        {
            Model model = this.explorerPresenter.ApsimXFile.Get(this.explorerPresenter.CurrentNodePath) as Model;
            if (model != null && model.GetType().Name != "Simulations")
            {
                DeleteModelCommand command = new DeleteModelCommand(model);
                this.explorerPresenter.CommandHistory.Add(command, true);
            }
        }

        /// <summary>
        /// Event handler for a User interface "Run APSIM" action
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Run APSIM",
                     AppliesTo = new Type[] { typeof(Simulation),
                                              typeof(Simulations),
                                              typeof(Experiment),
                                              typeof(Folder) })]
        public void RunAPSIM(object sender, EventArgs e)
        {
            this.explorerPresenter.Save();
            Model model = this.explorerPresenter.ApsimXFile.Get(this.explorerPresenter.CurrentNodePath) as Model;
            RunCommand command = new Commands.RunCommand(this.explorerPresenter.ApsimXFile, model, this.explorerPresenter);
            command.Do(null);
        }

        /// <summary>
        /// Event handler for a User interface "Run APSIM" action
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Check Soil", AppliesTo = new Type[] { typeof(Soil) })]
        public void CheckSoil(object sender, EventArgs e)
        {
            Soil currentSoil = this.explorerPresenter.ApsimXFile.Get(this.explorerPresenter.CurrentNodePath) as Soil;
            if (currentSoil != null)
            {
                string errorMessages = currentSoil.Check(false);
                if (errorMessages != string.Empty)
                {
                    this.explorerPresenter.ShowMessage(errorMessages, DataStore.ErrorLevel.Error);
                }
            }
        }

        /// <summary>
        /// Event handler for a User interface "Run APSIM" action
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Run Tests", AppliesTo = new Type[] { typeof(Tests) })]
        public void RunTests(object sender, EventArgs e)
        {
            RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"Software\R-core\R", false);
            if (registryKey != null)
            {
                // Will need to make this work on 32bit machines
                string pathToR = (string)registryKey.GetValue("InstallPath", string.Empty);
                pathToR += "\\Bin\\x64\\rscript.exe";

                string binFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string scriptFileName = Path.Combine(new string[] 
                    {
                        binFolder, 
                        "..", 
                        "Tests", 
                        "RTestSuite",
                        "RunTest.R"
                    });

                string workingFolder = Path.Combine(new string[] { binFolder, ".." });

                Process process = new Process();
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WorkingDirectory = workingFolder;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.FileName = pathToR;
                process.StartInfo.Arguments = "\"" + scriptFileName + "\" " + "\"" + this.explorerPresenter.ApsimXFile.FileName + "\"";
                process.Start();
                process.WaitForExit();
                this.explorerPresenter.ShowMessage(process.StandardOutput.ReadToEnd(), DataStore.ErrorLevel.Information);
                this.explorerPresenter.ShowMessage(process.StandardError.ReadToEnd(), DataStore.ErrorLevel.Warning);
            }
            else
            {
                this.explorerPresenter.ShowMessage("Could not find R installation.", DataStore.ErrorLevel.Warning);
            }

            {
            string binFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string apsimxFolder = Path.Combine(binFolder, "..");
            string scriptFileName = Path.Combine(new string[] 
            {
                binFolder, 
                "..", 
                "Tests", 
                "RTestSuite",
                "RunTest.Bat"
            });
            string workingFolder = apsimxFolder;
            Process process = Utility.Process.RunProcess(scriptFileName, this.explorerPresenter.ApsimXFile.FileName, workingFolder);
            string errorMessages = Utility.Process.CheckProcessExitedProperly(process);
            }
        }

        /// <summary>
        /// Event handler for adding a factor value
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Add factor value",
                     AppliesTo = new Type[] { typeof(Factor),
                                              typeof(FactorValue) })]
        public void AddFactorValue(object sender, EventArgs e)
        {
            Model factor = this.explorerPresenter.ApsimXFile.Get(this.explorerPresenter.CurrentNodePath) as Model;
            if (factor != null)
            {
                AddModelCommand command = new AddModelCommand("<FactorValue/>", factor);
                this.explorerPresenter.CommandHistory.Add(command, true);
            }
        }

        /// <summary>
        /// Event handler for adding a factor
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Add factor", AppliesTo = new Type[] { typeof(Factors) })]
        public void AddFactor(object sender, EventArgs e)
        {
            Model factors = this.explorerPresenter.ApsimXFile.Get(this.explorerPresenter.CurrentNodePath) as Model;
            if (factors != null)
            {
                AddModelCommand command = new AddModelCommand("<Factor/>", factors);
                this.explorerPresenter.CommandHistory.Add(command, true);
            }
        }

        /// <summary>
        /// Event handler for a User interface "Export" action
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Export to HTML",
                     AppliesTo = new Type[] { typeof(Simulation),
                                              typeof(Folder),
                                              typeof(Experiment),
                                              typeof(Simulations) })]
        public void ExportToHTML(object sender, EventArgs e)
        {
            string destinationFolder = this.explorerPresenter.AskUserForFolder("Select folder to export to");
            if (destinationFolder != null)
            {
                ExportNodeCommand command = new ExportNodeCommand(this.explorerPresenter, this.explorerPresenter.CurrentNodePath, destinationFolder);
                this.explorerPresenter.CommandHistory.Add(command, true);
            }
        }

        /// <summary>
        /// Run post simulation models.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Run post simulation models",
                     AppliesTo = new Type[] { typeof(DataStore) })]
        public void RunPostSimulationModels(object sender, EventArgs e)
        {
            DataStore dataStore = this.explorerPresenter.ApsimXFile.Get(this.explorerPresenter.CurrentNodePath) as DataStore;
            if (dataStore != null)
            {
                try
                {
                    // Run all child model post processors.
                    dataStore.RunPostProcessingTools();
                    this.explorerPresenter.ShowMessage("Post processing models have successfully completed", Models.DataStore.ErrorLevel.Information);
                }
                catch (Exception err)
                {
                    this.explorerPresenter.ShowMessage("Error: " + err.Message, Models.DataStore.ErrorLevel.Error);
                }
            }
        }
    }
}