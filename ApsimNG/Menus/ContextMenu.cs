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
    using Utility;
    using Models.Core.ApsimFile;
    using Models.Core.Run;
    using System.Reflection;
    using System.Linq;
    using System.Text;
    using Models.Functions;
    using Models.Soils.Standardiser;
    using Models.Graph;

    /// <summary>
    /// This class contains methods for all context menu items that the ExplorerView exposes to the user.
    /// </summary>
    public class ContextMenu
    {
        [Link(IsOptional = true)]
        IDataStore storage = null;

        /// <summary>
        /// Reference to the ExplorerPresenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// The command that is currently being run.
        /// </summary>
        private RunCommand command = null;

        /// <summary>
        /// Maps a type to an array of fields/properties which are links.
        /// </summary>
        private static Dictionary<Type, MemberInfo[]> links = new Dictionary<Type, MemberInfo[]>();

        /// <summary>
        /// Maps a type to a list of public string properties.
        /// </summary>
        private static Dictionary<Type, PropertyInfo[]> stringProperties = new Dictionary<Type, PropertyInfo[]>();

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
            try
            {
                storage.Writer.Empty();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
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
                var runner = new Runner(explorerPresenter.ApsimXFile, runSimulations: false);
                runner.Run();
                (explorerPresenter.CurrentPresenter as DataStorePresenter).PopulateGrid();
                this.explorerPresenter.MainPresenter.ShowMessage("Post processing models have successfully completed", Simulation.MessageType.Information);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Clear graph panel cache.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Clear Cache",
                     AppliesTo = new Type[] { typeof(GraphPanel) })]
        public void ClearGraphPanelCache(object sender, EventArgs e)
        {
            try
            {
                GraphPanel panel = Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath) as GraphPanel;
                panel.Cache.Clear();
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
                                              typeof(Folder),
                                              typeof(Morris),
                                              typeof(Sobol)},
                     ShortcutKey = "F5")]
        public void RunAPSIM(object sender, EventArgs e)
        {
            try
            {
                RunAPSIMInternal(multiProcessRunner: false);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
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
                                              typeof(Folder),
                                              typeof(Morris)},
                     ShortcutKey = "F6")]
        public void RunAPSIMMultiProcess(object sender, EventArgs e)
        {
            try
            {
                RunAPSIMInternal(multiProcessRunner: true);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
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
            try
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
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
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
            try
            {
                explorerPresenter.GenerateApsimXFiles(Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath) as IModel);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has clicked rename
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Rename", ShortcutKey = "F2", FollowsSeparator = true)]
        public void OnRename(object sender, EventArgs e)
        {
            try
            {
                this.explorerPresenter.Rename();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has clicked Copy
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Copy", ShortcutKey = "Ctrl+C")]
        public void OnCopyClick(object sender, EventArgs e)
        {
            try
            {
                Model model = Apsim.Get(this.explorerPresenter.ApsimXFile, this.explorerPresenter.CurrentNodePath) as Model;
                if (model != null)
                {
                    // Set the clipboard text.
                    string st = FileFormat.WriteToString(model);
                    this.explorerPresenter.SetClipboardText(st, "_APSIM_MODEL");
                    this.explorerPresenter.SetClipboardText(st, "CLIPBOARD");
                }
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
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
            try
            {
                string internalCBText = this.explorerPresenter.GetClipboardText("_APSIM_MODEL");
                string externalCBText = this.explorerPresenter.GetClipboardText("CLIPBOARD");

                string text = string.IsNullOrEmpty(externalCBText) ? internalCBText : externalCBText;

                this.explorerPresenter.Add(text, this.explorerPresenter.CurrentNodePath);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
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
            try
            {
                IModel model = Apsim.Get(this.explorerPresenter.ApsimXFile, this.explorerPresenter.CurrentNodePath) as IModel;
                if (model != null && model.GetType().Name != "Simulations")
                    this.explorerPresenter.Delete(model);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Move up
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Move up", ShortcutKey = "Ctrl+Up", FollowsSeparator = true)]
        public void OnMoveUpClick(object sender, EventArgs e)
        {
            try
            {
                IModel model = Apsim.Get(this.explorerPresenter.ApsimXFile, this.explorerPresenter.CurrentNodePath) as IModel;
                if (model != null && model.GetType().Name != "Simulations")
                    this.explorerPresenter.MoveUp(model);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Move down
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Move down", ShortcutKey = "Ctrl+Down")]
        public void OnMoveDownClick(object sender, EventArgs e)
        {
            try
            {
                IModel model = Apsim.Get(this.explorerPresenter.ApsimXFile, this.explorerPresenter.CurrentNodePath) as IModel;
                if (model != null && model.GetType().Name != "Simulations")
                    this.explorerPresenter.MoveDown(model);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Move up
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Collapse Children", ShortcutKey = "Ctrl+Left", FollowsSeparator = true)]
        public void OnCollapseChildren(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.CollapseChildren(explorerPresenter.CurrentNodePath);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Move down
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Expand Children", ShortcutKey = "Ctrl+Right")]
        public void OnExpandChildren(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.ExpandChildren(explorerPresenter.CurrentNodePath);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        [ContextMenu(MenuName = "Copy path to node",
                     ShortcutKey = "Ctrl+Shift+C",
                     FollowsSeparator = true)]
        public void CopyPathToNode(object sender, EventArgs e)
        {
            try
            {
                string nodePath = explorerPresenter.CurrentNodePath;
                if (Apsim.Get(explorerPresenter.ApsimXFile, nodePath) is IFunction)
                    nodePath += ".Value()";

                explorerPresenter.SetClipboardText(Path.GetFileNameWithoutExtension(explorerPresenter.ApsimXFile.FileName) + nodePath, "CLIPBOARD");
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
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
                    Runner.RunTypeEnum typeOfRun = Runner.RunTypeEnum.MultiThreaded;
                    if (multiProcessRunner)
                        typeOfRun = Runner.RunTypeEnum.MultiProcess;

                    Model model = Apsim.Get(this.explorerPresenter.ApsimXFile, this.explorerPresenter.CurrentNodePath) as Model;
                    var runner = new Runner(model, runType:typeOfRun, wait: false);
                    this.command = new RunCommand(model.Name, runner, this.explorerPresenter);
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
        /// Event handler for a User interface "Check Soil" action
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Check Soil", AppliesTo = new Type[] { typeof(Soil) })]
        public void CheckSoil(object sender, EventArgs e)
        {
            try
            {
                Soil currentSoil = Apsim.Get(this.explorerPresenter.ApsimXFile, this.explorerPresenter.CurrentNodePath) as Soil;
                if (currentSoil != null)
                {

                    string errorMessages = SoilChecker.Check(currentSoil);
                    if (!string.IsNullOrEmpty(errorMessages))
                        explorerPresenter.MainPresenter.ShowError(errorMessages);
                    else
                        explorerPresenter.MainPresenter.ShowMessage("Soil water parameters are valid.", Simulation.MessageType.Information);
                }
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Event handler for a User interface "Download Soil" action
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Download Soil...", AppliesTo = new Type[] { typeof(Soil), typeof(Zone) })]
        public void DownloadSoil(object sender, EventArgs e)
        {
            try
            {
                this.explorerPresenter.DownloadSoil();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Event handler for a User interface "Download Weather" action
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Download Weather...", AppliesTo = new Type[] { typeof(Weather), typeof(Simulation) })]
        public void DownloadWeather(object sender, EventArgs e)
        {
            try
            {
                this.explorerPresenter.DownloadWeather();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
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
            try
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
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
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
            try
            {
                explorerPresenter.MainPresenter.ShowWaitCursor(true);
                List<DataTable> tables = new List<DataTable>();
                foreach (string tableName in storage.Reader.TableNames)
                {
                    using (DataTable table = storage.Reader.GetData(tableName))
                    {
                        table.TableName = tableName;
                        tables.Add(table);
                    }
                }

                string fileName = Path.ChangeExtension(storage.FileName, ".xlsx");
                Utility.Excel.WriteToEXCEL(tables.ToArray(), fileName);
                explorerPresenter.MainPresenter.ShowMessage("Excel successfully created: " + fileName, Simulation.MessageType.Information);

                try
                {
                    if (ProcessUtilities.CurrentOS.IsWindows)
                        Process.Start(fileName);
                }
                catch
                {
                    // Swallow exceptions - this was a non-critical operation.
                }
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
        /// Export output in the data store to text files
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [ContextMenu(MenuName = "Export output to text files",
                     AppliesTo = new Type[] { typeof(DataStore) })]
        public void ExportOutputToTextFiles(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.MainPresenter.ShowWaitCursor(true);
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
            try
            {
                explorerPresenter.MainPresenter.ShowWaitCursor(true);
                string summaryFleName = Path.ChangeExtension(explorerPresenter.ApsimXFile.FileName, ".sum");
                Summary.WriteSummaryToTextFiles(storage, summaryFleName, Configuration.Settings.DarkTheme);
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
            try
            {
                object model = Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath);
                explorerPresenter.HideRightHandPanel();
                explorerPresenter.ShowInRightHandPanel(model,
                                                       "ApsimNG.Resources.Glade.AddModelView.glade",
                                                       new AddModelPresenter());
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
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
            try
            {
                explorerPresenter.HideRightHandPanel();
                explorerPresenter.ShowInRightHandPanel(explorerPresenter.ApsimXFile,
                                                       "UserInterface.Views.ListButtonView",
                                                       "UserInterface.Presenters.CheckpointsPresenter");
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
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
            try
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
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
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
            try
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
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
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
        [ContextMenu(MenuName = "Create simulation documentation",
                     FollowsSeparator = true,
                     AppliesTo = new[] { typeof(Simulations) })]
        public void CreateDocumentation(object sender, EventArgs e)
        {
            try
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
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
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
            try
            {
                explorerPresenter.ShowIncludeInDocumentation = !explorerPresenter.ShowIncludeInDocumentation;
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
        [ContextMenu(MenuName = "Show page of graphs in documentation", IsToggle = true,
                     AppliesTo = new Type[] { typeof(Folder) })]
        public void ShowPageOfGraphs(object sender, EventArgs e)
        {
            try
            {
                Folder folder = Apsim.Get(explorerPresenter.ApsimXFile, explorerPresenter.CurrentNodePath) as Folder;
                folder.ShowPageOfGraphs = !folder.ShowPageOfGraphs;
                foreach (Folder child in Apsim.ChildrenRecursively(folder, typeof(Folder)))
                    child.ShowPageOfGraphs = folder.ShowPageOfGraphs;
                explorerPresenter.PopulateContextMenu(explorerPresenter.CurrentNodePath);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }


        [ContextMenu(MenuName = "Find All References",
                     ShortcutKey = "Shift + F12")]
        public void OnFindReferences(object sender, EventArgs e)
        {
            try
            {
                IModel model = Apsim.Get(this.explorerPresenter.ApsimXFile, this.explorerPresenter.CurrentNodePath) as IModel;
                if (model != null)
                {
                    string modelPath = Apsim.FullPath(model);
                    StringBuilder message = new StringBuilder($"Searching for references to model {Apsim.FullPath(model)}...");
                    List<Reference> references = new List<Reference>();
                    message.AppendLine();
                    message.AppendLine();
                    Stopwatch timer = Stopwatch.StartNew();
                    BindingFlags flags;

                    foreach (IModel child in Apsim.FindAll(model))
                    {
                        if (Apsim.FullPath(child) == Apsim.FullPath(model))
                            continue;

                        // Resolve links (this doesn't seem to work properly).
                        explorerPresenter.ApsimXFile.Links.Resolve(child);
                        MemberInfo[] members = null;
                        Type childType = child.GetType();

                        // First, find all links to the model.
                        // First, try the cache.
                        if (!links.TryGetValue(childType, out members))
                        {
                            // We haven't looked for members of this type before.
                            List<MemberInfo> localMembers = new List<MemberInfo>();

                            // Links may be static or non-static (instance), and can have any accessibility.
                            flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                            // Find all properties which are links.
                            localMembers.AddRange(child.GetType().GetProperties(flags).Where(p => ReflectionUtilities.GetAttribute(p, typeof(LinkAttribute), true) != null));

                            // Find all fields which are links.
                            localMembers.AddRange(child.GetType().GetFields(flags).Where(f => ReflectionUtilities.GetAttribute(f, typeof(LinkAttribute), true) != null));

                            members = localMembers.ToArray();

                            // Add members to cache.
                            links.Add(childType, members);
                        }

                        // Now iterate over all members of this type which are links.
                        foreach (MemberInfo member in members)
                        {
                            IModel linkValue = ReflectionUtilities.GetValueOfFieldOrProperty(member.Name, child) as IModel;
                            if (linkValue == null)
                                continue; // Silently ignore this member.

                            bool isCorrectType = model.GetType().IsAssignableFrom(linkValue.GetType());
                            bool hasCorrectPath = string.Equals(Apsim.FullPath(linkValue), modelPath, StringComparison.InvariantCulture);
                            if (isCorrectType && hasCorrectPath)
                            {
                                message.AppendLine($"Found member {member.Name} of node {Apsim.FullPath(child)}.");
                                references.Add(new Reference() { Member = member, Target = model, Model = child });
                            }
                        }

                        //if (model is IFunction && child is IFunction)
                        {
                            // Next, search all public string properties for the path to this model.
                            PropertyInfo[] properties;
                            if (!stringProperties.TryGetValue(childType, out properties))
                            {
                                flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
                                properties = childType.GetProperties(flags).Where(p => p.PropertyType == typeof(string) && p.CanRead).ToArray();
                                stringProperties.Add(childType, properties);
                            }
                            foreach (PropertyInfo property in properties)
                            {
                                string value;
                                try
                                {
                                    // An exception could be thrown here from inside the property's getter.
                                    value = property.GetValue(child) as string;
                                }
                                catch
                                {
                                    continue;
                                }
                                if (value == null)
                                    continue;

                                value = value.Replace(".Value()", "").Replace(".Value", "");
                                IModel result = null;
                                try
                                {
                                    result = Apsim.Get(child, value) as IModel;
                                }
                                catch
                                {
                                    continue;
                                }
                                if (result == null)
                                    continue;
                                bool correctType = model.GetType().IsAssignableFrom(result.GetType());
                                bool correctPath = string.Equals(Apsim.FullPath(result), modelPath, StringComparison.InvariantCulture);
                                if (correctType && correctPath)
                                {
                                    message.AppendLine($"Found reference in string property {property.Name} of node {Apsim.FullPath(child)}.");
                                    references.Add(new Reference() { Member = property, Target = model, Model = child });
                                }
                            }
                        }
                    }
                    timer.Stop();
                    message.AppendLine();
                    message.AppendLine($"Finished. Elapsed time: {timer.Elapsed.TotalSeconds.ToString("#.00")} seconds");
                    explorerPresenter.MainPresenter.ShowMessage(message.ToString(), Simulation.MessageType.Information);
                    var dialog = new Utility.FindAllReferencesDialog(model, references, explorerPresenter);
                }
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }
    }
}