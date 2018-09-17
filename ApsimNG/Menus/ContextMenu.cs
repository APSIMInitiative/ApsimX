// -----------------------------------------------------------------------
// <copyright file="ContextMenu.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.IO;
    using Commands;
    using Models;
    using Models.Core;
    using Models.Factorial;
    using Models.Soils;
    using APSIM.Shared.Utilities;
    using Models.Storage;
    using Models.Report;

    /// <summary>
    /// This class contains methods for all context menu items that the ExplorerView exposes to the user.
    /// </summary>
    public class ContextMenu
    {
        [Link(IsOptional = true)]
        IStorageReader storage = null;

        [Link(IsOptional = true)]
        IStorageWriter storageWriter = null;

        /// <summary>
        /// Reference to the ExplorerPresenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// The command that is currently being run.
        /// </summary>
        private RunCommand command = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenu" /> class.
        /// </summary>
        /// <param name="explorerPresenter">The explorer presenter to work with</param>
        public ContextMenu(ExplorerPresenter explorerPresenter)
        {
            this.explorerPresenter = explorerPresenter;
        }

        /// <summary>
        /// Empty the data store
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Empty the data store",
                     AppliesTo = new Type[] { typeof(DataStore) })]
        public void EmptyDataStore(object sender, EventArgs e)
        {
            storage.EmptyDataStore();
        }

        /// <summary>
        /// Run post simulation models.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Refresh",
                     AppliesTo = new Type[] { typeof(DataStore) })]
        public void RunPostSimulationModels(object sender, EventArgs e)
        {
            try
            {
                // Run all child model post processors.
                foreach (IPostSimulationTool tool in Apsim.FindAll(explorerPresenter.ApsimXFile, typeof(IPostSimulationTool)))
                    if ((tool as IModel).Enabled)
                        tool.Run(storage);
                this.explorerPresenter.MainPresenter.ShowMessage("Post processing models have successfully completed", Simulation.MessageType.Information);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
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
                                              typeof(Folder) },
                     ShortcutKey = "F5")]
        public void RunAPSIM(object sender, EventArgs e)
        {
            RunAPSIMInternal(multiProcessRunner: false);
        }

        /// <summary>
        /// Event handler for a User interface "Run APSIM multi-process (experimental)" action
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Run APSIM multi-process (experimental)",
                     AppliesTo = new Type[] { typeof(Simulation),
                                              typeof(Simulations),
                                              typeof(Experiment),
                                              typeof(Folder) },
                     ShortcutKey = "F6")]
        public void RunAPSIMMultiProcess(object sender, EventArgs e)
        {
            RunAPSIMInternal(multiProcessRunner: true);
        }

        [ContextMenu(MenuName = "Run on cloud",
                     AppliesTo = new Type[] { typeof(Simulation),
                                              typeof(Simulations),
                                              typeof(Experiment),
                                              typeof(Folder)
                                            }
                    )
        ]
        /// <summary>
        /// Event handler for the run on cloud action
        /// </summary>        
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        public void RunOnCloud(object sender, EventArgs e)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                object model = Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath);
                explorerPresenter.HideRightHandPanel();
                explorerPresenter.ShowInRightHandPanel(model,
                                                       "UserInterface.Views.NewAzureJobView",
                                                       "UserInterface.Presenters.NewAzureJobPresenter");
            }
            else
            {
                explorerPresenter.MainPresenter.ShowError("Microsoft Azure functionality is currently only available under Windows.");
            }

        }

        /// <summary>
        /// Event handler for generate .apsimx files option.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Generate .apsimx files",
             AppliesTo = new Type[] {   typeof(Folder),
                                        typeof(Simulations),
                                        typeof(Simulation),
                                        typeof(Experiment),
                                    }
            )
        ]
        public void OnGenerateApsimXFiles(object sender, EventArgs e)
        {
            explorerPresenter.GenerateApsimXFiles(Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath) as IModel); ;
        }
        /// <summary>
        /// User has clicked rename
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Rename", ShortcutKey = "F2", FollowsSeparator = true)]
        public void OnRename(object sender, EventArgs e)
        {
            this.explorerPresenter.Rename();
        }

        /// <summary>
        /// User has clicked Copy
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Copy", ShortcutKey = "Ctrl+C")]
        public void OnCopyClick(object sender, EventArgs e)
        {
            Model model = Apsim.Get(this.explorerPresenter.ApsimXFile, this.explorerPresenter.CurrentNodePath) as Model;
            if (model != null)
            {
                // Set the clipboard text.
                string xml = Apsim.Serialise(model);
                this.explorerPresenter.SetClipboardText(xml, "_APSIM_MODEL");
                this.explorerPresenter.SetClipboardText(xml, "CLIPBOARD");
            }
        }

        /// <summary>
        /// User has clicked Paste
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Paste", ShortcutKey = "Ctrl+V")]
        public void OnPasteClick(object sender, EventArgs e)
        {
            string internalCBText = this.explorerPresenter.GetClipboardText("_APSIM_MODEL");
            string externalCBText = this.explorerPresenter.GetClipboardText("CLIPBOARD");

            if (externalCBText == null || externalCBText == "")
                this.explorerPresenter.Add(internalCBText, this.explorerPresenter.CurrentNodePath);
            else
            {
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                try
                {
                    doc.LoadXml(externalCBText);
                    this.explorerPresenter.Add(externalCBText, this.explorerPresenter.CurrentNodePath);
                }
                catch (System.Xml.XmlException)
                {
                    // External clipboard does not contain valid xml
                    this.explorerPresenter.Add(internalCBText, this.explorerPresenter.CurrentNodePath);
                }
                catch (Exception ex)
                {
                    this.explorerPresenter.MainPresenter.ShowError(ex);
                }
            }
        }

        /// <summary>
        /// User has clicked Delete
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Delete", ShortcutKey = "Del")]
        public void OnDeleteClick(object sender, EventArgs e)
        {
            IModel model = Apsim.Get(this.explorerPresenter.ApsimXFile, this.explorerPresenter.CurrentNodePath) as IModel;
            if (model != null && model.GetType().Name != "Simulations")
                this.explorerPresenter.Delete(model);
        }

        /// <summary>
        /// Move up
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Move up", ShortcutKey = "Ctrl+Up", FollowsSeparator = true)]
        public void OnMoveUpClick(object sender, EventArgs e)
        {
            IModel model = Apsim.Get(this.explorerPresenter.ApsimXFile, this.explorerPresenter.CurrentNodePath) as IModel;
            if (model != null && model.GetType().Name != "Simulations")
                this.explorerPresenter.MoveUp(model);
        }

        /// <summary>
        /// Move down
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Move down", ShortcutKey = "Ctrl+Down")]
        public void OnMoveDownClick(object sender, EventArgs e)
        {
            IModel model = Apsim.Get(this.explorerPresenter.ApsimXFile, this.explorerPresenter.CurrentNodePath) as IModel;
            if (model != null && model.GetType().Name != "Simulations")
                this.explorerPresenter.MoveDown(model);
        }


        [ContextMenu(MenuName = "Copy path to node",
                     ShortcutKey = "Ctrl+Shift+C")]
        public void CopyPathToNode(object sender, EventArgs e)
        {
            string nodePath = explorerPresenter.CurrentNodePath;
            if (Apsim.Get(explorerPresenter.ApsimXFile, nodePath) is Models.Functions.IFunction)
            {
                nodePath += ".Value()";
            }
            explorerPresenter.SetClipboardText(Path.GetFileNameWithoutExtension(explorerPresenter.ApsimXFile.FileName) + nodePath, "CLIPBOARD");
        }

        /// <summary>Run APSIM.</summary>
        /// <param name="multiProcessRunner">Use the multi-process runner?</param>
        private void RunAPSIMInternal(bool multiProcessRunner)
        {
            if (this.explorerPresenter.Save())
            {
                List<string> duplicates = this.explorerPresenter.ApsimXFile.FindDuplicateSimulationNames();
                if (duplicates.Count > 0)
                {
                    string errorMessage = "Duplicate simulation names found " + StringUtilities.BuildString(duplicates.ToArray(), ", ");
                    explorerPresenter.MainPresenter.ShowError(errorMessage);
                }
                else
                {
                    Model model = Apsim.Get(this.explorerPresenter.ApsimXFile, this.explorerPresenter.CurrentNodePath) as Model;
                    this.command = new Commands.RunCommand(model, this.explorerPresenter, multiProcessRunner, storageWriter);
                    this.command.Do(null);
                }
            }
        }

        /// <summary>
        /// A run has completed so re-enable the run button.
        /// </summary>
        /// <returns>True when APSIM is not running</returns>
        public bool RunAPSIMEnabled()
        {
            bool isRunning = this.command != null && this.command.IsRunning;
            if (!isRunning)
            {
                this.command = null;
            }

            return !isRunning;
        }

        /// <summary>
        /// Event handler for a User interface "Run APSIM" action
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Check Soil", AppliesTo = new Type[] { typeof(Soil) })]
        public void CheckSoil(object sender, EventArgs e)
        {
            Soil currentSoil = Apsim.Get(this.explorerPresenter.ApsimXFile, this.explorerPresenter.CurrentNodePath) as Soil;
            if (currentSoil != null)
            {
                string errorMessages = currentSoil.Check(false);
                if (errorMessages != string.Empty)
                {
                    this.explorerPresenter.MainPresenter.ShowError(errorMessages);
                }
            }
        }

        /// <summary>
        /// Accept the current test output as the official baseline for future comparison. 
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Accept Tests", AppliesTo = new Type[] { typeof(Tests) })]
        public void AcceptTests(object sender, EventArgs e)
        {
            int result = explorerPresenter.MainPresenter.ShowMsgDialog("You are about to change the officially accepted stats for this model. Are you sure?", "Replace official stats?", Gtk.MessageType.Question, Gtk.ButtonsType.YesNo);
            if ((Gtk.ResponseType)result != Gtk.ResponseType.Yes)
            {
                return;
            }

            Tests test = Apsim.Get(this.explorerPresenter.ApsimXFile, this.explorerPresenter.CurrentNodePath) as Tests;
            try
            {
                test.Test(true);
            }
            catch (ApsimXException ex)
            {
                explorerPresenter.MainPresenter.ShowError(ex);
            }
            finally
            {
                this.explorerPresenter.HideRightHandPanel();
                this.explorerPresenter.ShowRightHandPanel();
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
            Model factors = Apsim.Get(this.explorerPresenter.ApsimXFile, this.explorerPresenter.CurrentNodePath) as Model;
            if (factors != null)
                this.explorerPresenter.Add("<Factor/>", this.explorerPresenter.CurrentNodePath);
        }

        /// <summary>
        /// Export the data store to EXCEL format
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Export to EXCEL",
                     AppliesTo = new Type[] { typeof(DataStore) }, FollowsSeparator = true)]
        public void ExportDataStoreToEXCEL(object sender, EventArgs e)
        {
            explorerPresenter.MainPresenter.ShowWaitCursor(true);
            List<DataTable> tables = new List<DataTable>();
            foreach (string tableName in storage.TableNames)
            {
                using (DataTable table = storage.GetData(tableName))
                {
                    table.TableName = tableName;
                    tables.Add(table);
                }
            }
            try
            {
                string fileName = Path.ChangeExtension(storage.FileName, ".xlsx");
                Utility.Excel.WriteToEXCEL(tables.ToArray(), fileName);
                explorerPresenter.MainPresenter.ShowMessage("Excel successfully created: " + fileName, Simulation.MessageType.Information);
            }
            finally
            {
                explorerPresenter.MainPresenter.ShowWaitCursor(false);
            }
        }

        /// <summary>
        /// Export output in the data store to text files
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Export output to text files",
                     AppliesTo = new Type[] { typeof(DataStore) })]
        public void ExportOutputToTextFiles(object sender, EventArgs e)
        {
            explorerPresenter.MainPresenter.ShowWaitCursor(true);
            try
            {
                Report.WriteAllTables(storage, explorerPresenter.ApsimXFile.FileName);
                string folder = Path.GetDirectoryName(explorerPresenter.ApsimXFile.FileName);
                explorerPresenter.MainPresenter.ShowMessage("Text files have been written to " + folder, Simulation.MessageType.Information);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
            finally
            {
                explorerPresenter.MainPresenter.ShowWaitCursor(false);
            }
        }

        /// <summary>
        /// Export summary in the data store to text files
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Export summary to text files",
                     AppliesTo = new Type[] { typeof(DataStore) })]
        public void ExportSummaryToTextFiles(object sender, EventArgs e)
        {
            explorerPresenter.MainPresenter.ShowWaitCursor(true);
            try
            {
                string summaryFleName = Path.ChangeExtension(explorerPresenter.ApsimXFile.FileName, ".sum");
                Summary.WriteSummaryToTextFiles(storage, summaryFleName);
                explorerPresenter.MainPresenter.ShowMessage("Summary file written: " + summaryFleName, Simulation.MessageType.Information);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
            finally
            {
                explorerPresenter.MainPresenter.ShowWaitCursor(false);
            }
        }

        /// <summary>
        /// Event handler for a Add model action
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Add model...", FollowsSeparator = true)]
        public void AddModel(object sender, EventArgs e)
        {
            object model = Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath);
            explorerPresenter.HideRightHandPanel();
            explorerPresenter.ShowInRightHandPanel(model,
                                                   "UserInterface.Views.ListButtonView",
                                                   "UserInterface.Presenters.AddModelPresenter");
        }

        /// <summary>
        /// Event handler for a Add function action
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Add function...")]
        public void AddFunction(object sender, EventArgs e)
        {
            object model = Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath);
            explorerPresenter.ShowInRightHandPanel(model,
                                                   "UserInterface.Views.ListButtonView",
                                                   "UserInterface.Presenters.AddFunctionPresenter");
        }


        public bool ShowIncludeInDocumentationChecked()
        {
            return explorerPresenter != null ? explorerPresenter.ShowIncludeInDocumentation : false;
        }

        /// <summary>
        /// Event handler for checkbox for 'Include in documentation' menu item.
        /// </summary>
        public bool IncludeInDocumentationChecked()
        {
            IModel model = Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath) as IModel;
            return (model != null) ? model.IncludeInDocumentation : false;
        }

        /// <summary>
        /// Event handler for checkbox for 'Include in documentation' menu item.
        /// </summary>
        public bool ShowPageOfGraphsChecked()
        {
            Folder folder = Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath) as Folder;
            return (folder != null) ? folder.ShowPageOfGraphs : false;
        }

        /// <summary>
        /// Event handler for 'Checkpoints' menu item
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Checkpoints", IsToggle = true,
                     AppliesTo = new Type[] { typeof(Simulations) })]
        public void ShowCheckpoints(object sender, EventArgs e)
        {
            explorerPresenter.HideRightHandPanel();
            explorerPresenter.ShowInRightHandPanel(explorerPresenter.ApsimXFile,
                                                   "UserInterface.Views.ListButtonView",
                                                   "UserInterface.Presenters.CheckpointsPresenter");
        }

        /// <summary>
        /// Event handler for 'Show Model Structure' menu item.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        [ContextMenu(MenuName = "Show Model Structure",
                     IsToggle = true,
                     AppliesTo = new Type[] { typeof(ModelCollectionFromResource) })]
        public void ShowModelStructure(object sender, EventArgs e)
        {
            IModel model = Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath) as IModel;
            if (model != null)
            {
                foreach (IModel child in Apsim.ChildrenRecursively(model))
                    child.IsHidden = !child.IsHidden;
                explorerPresenter.PopulateContextMenu(explorerPresenter.CurrentNodePath);
                explorerPresenter.Refresh();
            }
        }

        public bool ShowModelStructureChecked()
        {
            IModel model = Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath) as IModel;
            if (model.Children.Count < 1)
                return true;
            return !model.Children[0].IsHidden;
        }

        /// <summary>
        /// Event handler for 'Enabled' menu item.
        /// </summary>
        [ContextMenu(MenuName = "Enabled", IsToggle = true)]
        public void Enabled(object sender, EventArgs e)
        {
            IModel model = Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath) as IModel;
            if (model != null)
            {
                model.Enabled = !model.Enabled;
                foreach (IModel child in Apsim.ChildrenRecursively(model))
                    child.Enabled = model.Enabled;
                explorerPresenter.PopulateContextMenu(explorerPresenter.CurrentNodePath);
                explorerPresenter.Refresh();
            }
        }

        /// <summary>
        /// Event handler for checkbox for 'Enabled' menu item.
        /// </summary>
        public bool EnabledChecked()
        {
            IModel model = Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath) as IModel;
            return model.Enabled;
        }

        /// <summary>
        /// Event handler for a User interface "Create documentation" action
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Create documentation", FollowsSeparator = true)]
        public void CreateDocumentation(object sender, EventArgs e)
        {
            if (this.explorerPresenter.Save())
            {
                string destinationFolder = Path.Combine(Path.GetDirectoryName(this.explorerPresenter.ApsimXFile.FileName), "Doc");
                if (destinationFolder != null)
                {
                    explorerPresenter.MainPresenter.ShowMessage("Creating documentation...", Simulation.MessageType.Information);
                    explorerPresenter.MainPresenter.ShowWaitCursor(true);

                    try
                    {
                        ExportNodeCommand command = new ExportNodeCommand(this.explorerPresenter, this.explorerPresenter.CurrentNodePath);
                        this.explorerPresenter.CommandHistory.Add(command, true);
                        explorerPresenter.MainPresenter.ShowMessage("Finished creating documentation", Simulation.MessageType.Information);
                        Process.Start(command.FileNameWritten);
                    }
                    catch (Exception err)
                    {
                        explorerPresenter.MainPresenter.ShowError(err);
                    }
                    finally
                    {
                        explorerPresenter.MainPresenter.ShowWaitCursor(false);
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for a write debug document
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Write debug document",
                     AppliesTo = new Type[] { typeof(Simulation) })]
        public void WriteDebugDocument(object sender, EventArgs e)
        {
            try
            {
                Simulation model = Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath) as Simulation;
                WriteDebugDoc writeDocument = new WriteDebugDoc(explorerPresenter, model);
                writeDocument.Do(null);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Event handler for 'Include in documentation'
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Include in documentation", IsToggle = true)]
        public void IncludeInDocumentation(object sender, EventArgs e)
        {
            try
            {
                IModel model = Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath) as IModel;
                model.IncludeInDocumentation = !model.IncludeInDocumentation; // toggle switch

                foreach (IModel child in Apsim.ChildrenRecursively(model))
                    child.IncludeInDocumentation = model.IncludeInDocumentation;
                explorerPresenter.PopulateContextMenu(explorerPresenter.CurrentNodePath);
                explorerPresenter.Refresh();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        [ContextMenu(MenuName = "Show documentation status", IsToggle = true)]
        public void ShowIncludeInDocumentation(object sender, EventArgs e)
        {
            explorerPresenter.ShowIncludeInDocumentation = !explorerPresenter.ShowIncludeInDocumentation;
        }

        /// <summary>
        /// Event handler for 'Include in documentation'
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Show page of graphs in documentation", IsToggle = true,
                     AppliesTo = new Type[] { typeof(Folder) })]
        public void ShowPageOfGraphs(object sender, EventArgs e)
        {
            Folder folder = Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath) as Folder;
            folder.ShowPageOfGraphs = !folder.ShowPageOfGraphs;
            foreach (Folder child in Apsim.ChildrenRecursively(folder, typeof(Folder)))
                child.ShowPageOfGraphs = folder.ShowPageOfGraphs;
            explorerPresenter.PopulateContextMenu(explorerPresenter.CurrentNodePath);
        }
    }
}