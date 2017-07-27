// -----------------------------------------------------------------------
// <copyright file="TabbedExplorerPresenter.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using Importer;
    using Interfaces;
    using Models.Core;
    using Views;
    using System.Reflection;
    using Models;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// This presenter class provides the functionality behind a TabbedExplorerView 
    /// which is a tab control where each tabs represent an .apsimx file. Each tab
    /// then has an ExplorerPresenter and ExplorerView created when the tab is
    /// created.
    /// </summary>
    public class MainPresenter
    {
        /// <summary>A private reference to the view this presenter will talk to.</summary>
        private IMainView view;

        /// <summary>The path last used to open the examples</summary>
        private string lastExamplesPath;

        /// <summary>A list of presenters for tabs on the left.</summary>
        public List<ExplorerPresenter> presenters1 = new List<ExplorerPresenter>();

        /// <summary>A list of presenters for tabs on the right.</summary>
        private List<ExplorerPresenter> presenters2 = new List<ExplorerPresenter>();

        /// <summary>Attach this presenter with a view. Can throw if there are errors during startup.</summary>
        /// <param name="view">The view to attach</param>
        /// <param name="commandLineArguments">Optional command line arguments - can be null</param>
        public void Attach(object view, string[] commandLineArguments)
        {
            this.view = view as IMainView;

            // Set the main window location and size.
            this.view.WindowLocation = Utility.Configuration.Settings.MainFormLocation;
            this.view.WindowSize = Utility.Configuration.Settings.MainFormSize;
            this.view.WindowMaximised = Utility.Configuration.Settings.MainFormMaximized;

            // Set the main window caption with version information.
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version.Major == 0)
                this.view.WindowCaption = "APSIM (Custom Build)";
            else
                this.view.WindowCaption = "APSIM " + version;

            // Cleanup the recent file list
            Utility.Configuration.Settings.CleanMruList();

            // Populate the 2 start pages.
            PopulateStartPage(this.view.StartPage1);
            PopulateStartPage(this.view.StartPage2);

            // Trap some events.
            this.view.AllowClose += OnClosing;
            this.view.StartPage1.List.DoubleClicked += OnFileDoubleClicked;
            this.view.StartPage2.List.DoubleClicked += OnFileDoubleClicked;
            this.view.TabClosing += OnTabClosing;
            this.view.Show();

            // Process command line.
            ProcessCommandLineArguments(commandLineArguments);
        }

        /// <summary>Detach this presenter from the view.</summary>
        /// <param name="view">The view used for this object</param>
        public void Detach(object view)
        {
            this.view.AllowClose -= OnClosing;
            this.view.StartPage1.List.DoubleClicked -= OnFileDoubleClicked;
            this.view.StartPage2.List.DoubleClicked -= OnFileDoubleClicked;
            this.view.TabClosing -= OnTabClosing;
        }

        /// <summary>Allow the form to close?</summary>
        /// <returns>True if can be closed</returns>
        public bool AllowClose()
        {
            bool ok = true;

            foreach (ExplorerPresenter presenter in this.presenters1)
                ok = presenter.SaveIfChanged() && ok;

            foreach (ExplorerPresenter presenter in this.presenters2)
                ok = presenter.SaveIfChanged() && ok;

            return ok;
        }

        /// <summary>Toggle split screen view.</summary>
        public void ToggleSecondExplorerViewVisible()
        {
            view.SplitWindowOn = !view.SplitWindowOn;
        }

        /// <summary>Execute the specified script, returning any error messages or NULL if all OK.</summary>
        public string ProcessStartupScript(string code)
        {
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
            object[] arguments = new object[] { this };
            executeMethod.Invoke(script, arguments);
            return null;
        }

        /// <summary>
        /// Add a status message. A message of null will clear the status message.
        /// </summary>
        /// <param name="message"></param>
        public void ShowMessage(string message, Simulation.ErrorLevel errorLevel)
        {
            view.ShowMessage(message, errorLevel);
        }

        /// <summary>Show a message in a dialog box</summary>
        /// <param name="message">The message.</param>
        /// <param name="errorLevel">The error level.</param>
        public int ShowMsgDialog(string message, string title, Gtk.MessageType msgType, Gtk.ButtonsType buttonType)
        {
            return view.ShowMsgDialog(message, title, msgType, buttonType);
        }

        /// <summary>
        /// Show progress bar with the specified percent.
        /// </summary>
        /// <param name="percent"></param>
        public void ShowProgress(int percent)
        {
            view.ShowProgress(percent);
        }

        /// <summary>
        /// Add a handler for the "stop" button of the view
        /// </summary>
        /// <param name="handler">The handler to be added</param>
        public void AddStopHandler(EventHandler<EventArgs> handler)
        {
            this.view.StopSimulation += handler;
        }

        /// <summary>
        /// Remove a handler for the "stop" button of the view
        /// </summary>
        /// <param name="handler">The handler to be removed</param>
        public void RemoveStopHandler(EventHandler<EventArgs> handler)
        {
            this.view.StopSimulation -= handler;
        }

        /// <summary>Show the wait cursor</summary>
        /// <param name="wait">If true will show the wait cursor otherwise the normal cursor.</param>
        public void ShowWaitCursor(bool wait)
        {
            view.ShowWaitCursor(wait);
        }

        /// <summary>Change the text of a tab.</summary>
        /// <param name="currentTabName">Current tab text.</param>
        /// <param name="newTabName">New text of the tab.</param>
        public void ChangeTabText(object ownerView, string newTabName, string tooltip)
        {
            view.ChangeTabText(ownerView, newTabName, tooltip);
        }

        /// <summary>Close the application</summary>
        public void Close(bool askToSave)
        {
            view.Close();
        }

        /// <summary>Ask the user a question</summary>
        /// <param name="message">The message to show the user.</param>
        public QuestionResponseEnum AskQuestion(string message)
        {
            return view.AskQuestion(message);
        }

        /// <summary>Ask user for a filename to open.</summary>
        /// <param name="fileSpec">The file specification to use to filter the files.</param>
        /// <param name="initialDirectory">Optional Initial starting directory</param>
        public string AskUserForOpenFileName(string fileSpec, string initialDirectory = "")
        {
            return view.AskUserForOpenFileName(fileSpec, initialDirectory);
        }

        /// <summary>
        /// A helper function that asks user for a SaveAs name and returns their new choice.
        /// </summary>
        /// <param name="fileSpec">The file specification to filter the files.</param>
        /// <param name="oldFilename">The current file name.</param>
        /// <returns>Returns the new file name or null if action cancelled by user.</returns>
        public string AskUserForSaveFileName(string fileSpec, string oldFilename)
        {
            return view.AskUserForSaveFileName(fileSpec, oldFilename);
        }

        /// <summary>Populate the view for the first time. Will throw if there are errors on startup.</summary>
        /// <param name="startPage">The start page to populate.</param>
        private void PopulateStartPage(IListButtonView startPage)
        {
            // Add the buttons into the main window.
            startPage.AddButton("Open APSIM File",
                                          new Gtk.Image(null, "ApsimNG.Resources.Toolboxes.OpenFile.png"),
                                          OnOpenApsimXFile);

            startPage.AddButton("Open an example",
                                          new Gtk.Image(null, "ApsimNG.Resources.Toolboxes.OpenExample.png"),
                                          OnExample);

            startPage.AddButton("Standard toolbox",
                                          new Gtk.Image(null, "ApsimNG.Resources.Toolboxes.Toolbox.png"),
                                          OnStandardToolboxClick);

            startPage.AddButton("Management toolbox",
                                          new Gtk.Image(null, "ApsimNG.Resources.Toolboxes.Toolbox.png"),
                                          OnManagementToolboxClick);

            startPage.AddButton("Training toolbox",
                                          new Gtk.Image(null, "ApsimNG.Resources.Toolboxes.Toolbox.png"),
                                          OnTrainingToolboxClick);

            startPage.AddButton("Import old .apsim file",
                                          new Gtk.Image(null, "ApsimNG.Resources.Toolboxes.Import.png"),
                                          OnImport);

            startPage.AddButton("Upgrade",
                              new Gtk.Image(null, "ApsimNG.Resources.MenuImages.Upgrade.png"),
                              OnUpgrade);
            
            // Populate the view's listview.
            startPage.List.Values = Utility.Configuration.Settings.MruList.ToArray();

            PopulatePopup(startPage);
        }

        /// <summary>
        /// Defines the list of items to be included in a popup menu for the
        /// most-recently-used file display
        /// </summary>
        /// <param name="startPage">The page to which the menu will be added</param>
        private void PopulatePopup(IListButtonView startPage)
        {
            List<MenuDescriptionArgs> descriptions = new List<MenuDescriptionArgs>();
            MenuDescriptionArgs descOpen = new MenuDescriptionArgs();
            descOpen.Name = "Open";
            descOpen.Enabled = true;
            descOpen.OnClick = OnOpen;
            descriptions.Add(descOpen);

            MenuDescriptionArgs descRemove = new MenuDescriptionArgs();
            descRemove.Name = "Remove from recent file list";
            descRemove.Enabled = true;
            descRemove.OnClick = OnRemove;
            descriptions.Add(descRemove);

            MenuDescriptionArgs descClear = new MenuDescriptionArgs();
            descClear.Name = "Clear recent file list";
            descClear.Enabled = true;
            descClear.OnClick = OnClear;
            descriptions.Add(descClear);

            MenuDescriptionArgs descRename = new MenuDescriptionArgs();
            descRename.Name = "Rename";
            descRename.Enabled = true;
            descRename.OnClick = OnRename;
            descriptions.Add(descRename);

            MenuDescriptionArgs descCopy = new MenuDescriptionArgs();
            descCopy.Name = "Copy";
            descCopy.Enabled = true;
            descCopy.OnClick = OnCopy;
            descriptions.Add(descCopy);

            MenuDescriptionArgs descDelete = new MenuDescriptionArgs();
            descDelete.Name = "Delete";
            descDelete.Enabled = true;
            descDelete.OnClick = OnDelete;
            descriptions.Add(descDelete);

            startPage.List.PopulateContextMenu(descriptions);
        }

        /// <summary>
        /// Handles the Open menu item of the MRU context menu
        /// </summary>
        /// <param name="obj">The object issuing the event</param>
        /// <param name="args">Event parameters</param>
        private void OnOpen(object obj, EventArgs args)
        {
            string fileName = this.view.GetMenuItemFileName(obj);
            if (fileName != null)
            {
                this.OpenApsimXFileInTab(fileName, this.view.IsControlOnLeft(obj));
                Utility.Configuration.Settings.PreviousFolder = Path.GetDirectoryName(fileName);
            }
        }

        /// <summary>
        /// Handles the Remove menu item of the MRU context menu
        /// </summary>
        /// <param name="obj">The object issuing the event</param>
        /// <param name="args">Event parameters</param>
        private void OnRemove(object obj, EventArgs args)
        {
            string fileName = this.view.GetMenuItemFileName(obj);
            if (!String.IsNullOrEmpty(fileName))
            {
                Utility.Configuration.Settings.DelMruFile(fileName);
                UpdateMRUDisplay();
            }
        }

        /// <summary>
        /// Handles the Clear menu item of the MRU context menu
        /// </summary>
        /// <param name="obj">The object issuing the event</param>
        /// <param name="args">Event parameters</param>
        private void OnClear(object obj, EventArgs args)
        {
            if (AskQuestion("Are you sure you want to completely clear the list of recently used files?") == QuestionResponseEnum.Yes)
            {
                string[] MRUFiles = Utility.Configuration.Settings.MruList.ToArray();
                foreach (string fileName in MRUFiles)
                    Utility.Configuration.Settings.DelMruFile(fileName);
                UpdateMRUDisplay();
            }
        }

        /// <summary>
        /// Handles the Rename menu item of the MRU context menu
        /// </summary>
        /// <param name="obj">The object issuing the event</param>
        /// <param name="args">Event parameters</param>
        private void OnRename(object obj, EventArgs args)
        {
            string fileName = this.view.GetMenuItemFileName(obj);
            if (!String.IsNullOrEmpty(fileName))
            {
                string newName = AskUserForSaveFileName("ApsimX files|*.apsimx", fileName);
                if (!String.IsNullOrEmpty(newName) && newName != fileName)
                {
                    try
                    {
                        File.Move(fileName, newName);
                        Utility.Configuration.Settings.RenameMruFile(fileName, newName);
                        UpdateMRUDisplay();
                    }
                    catch (Exception)
                    {
                        view.ShowMessage("Error renaming file!", DataStore.ErrorLevel.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Copy menu item of the MRU context menu
        /// </summary>
        /// <param name="obj">The object issuing the event</param>
        /// <param name="args">Event parameters</param>
        private void OnCopy(object obj, EventArgs args)
        {
            string fileName = this.view.GetMenuItemFileName(obj);
            if (!String.IsNullOrEmpty(fileName))
            {
                string newFileName = "Copy of " + Path.GetFileName(fileName);
                string newFilePath = Path.Combine(Path.GetDirectoryName(fileName), newFileName);
                string copyName = AskUserForSaveFileName("ApsimX files|*.apsimx", newFilePath);
                if (!String.IsNullOrEmpty(copyName))
                {
                    try
                    {
                        File.Copy(fileName, copyName);
                        Utility.Configuration.Settings.AddMruFile(copyName);
                        UpdateMRUDisplay();
                    }
                    catch (Exception)
                    {
                        view.ShowMessage("Error creating copy of file!", DataStore.ErrorLevel.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Delete menu item of the MRU context menu
        /// </summary>
        /// <param name="obj">The object issuing the event</param>
        /// <param name="args">Event parameters</param>
        private void OnDelete(object obj, EventArgs args)
        {
            string fileName = this.view.GetMenuItemFileName(obj);
            if (!String.IsNullOrEmpty(fileName))
            {
                if (AskQuestion("Are you sure you want to completely delete the file " + fileName + "?") == QuestionResponseEnum.Yes)
                {
                    try
                    {
                        File.Delete(fileName);
                        Utility.Configuration.Settings.DelMruFile(fileName);
                        UpdateMRUDisplay();
                    }
                    catch (Exception)
                    {
                        view.ShowMessage("Error deleting file!", DataStore.ErrorLevel.Error);
                    }
                }
            }
        }

        /// <summary>Process the specified command line arguments. Will throw if there are errors during startup.</summary>
        /// <param name="commandLineArguments">Optional command line arguments - can be null</param>
        private void ProcessCommandLineArguments(string[] commandLineArguments)
        {
            // Look for a script specified on the command line.
            foreach (string argument in commandLineArguments)
            {
                if (Path.GetExtension(argument) == ".cs")
                    ProcessStartupScript(File.ReadAllText(argument));

                else if (Path.GetExtension(argument) == ".apsimx")
                    OpenApsimXFileInTab(argument, onLeftTabControl: true);
            }
        }

        /// <summary>
        /// Returns the ExplorerPresenter for the specified file name, 
        /// or null if the file is not currently open
        /// </summary>
        /// <param name="fileName">The file name being sought</param>
        /// <param name="onLeftTabControl">If true, search the left screen, else search the right</param>
        /// <returns></returns>
        ExplorerPresenter PresenterForFile(string fileName, bool onLeftTabControl)
        {
            List<ExplorerPresenter> presenters = onLeftTabControl ? presenters1 : presenters2;
            foreach (ExplorerPresenter presenter in presenters)
            {
                if (presenter.ApsimXFile.FileName == fileName)
                    return presenter;
            }
            return null;
        }

        /// <summary>Open an .apsimx file into the current tab.</summary>
        /// <param name="fileName">The file to open</param>
        /// <param name="onLeftTabControl">If true a tab will be added to the left hand tab control.</param>
        public ExplorerPresenter OpenApsimXFileInTab(string fileName, bool onLeftTabControl)
        {
            ExplorerPresenter presenter = null;
            if (fileName != null)
            {
                presenter = PresenterForFile(fileName, onLeftTabControl);
                if (presenter != null)
                {
                    view.SelectTabContaining(presenter.GetView().MainWidget);
                    return presenter;
                }
                view.ShowWaitCursor(true);
                try
                {
                    Simulations simulations = Simulations.Read(fileName);
                    presenter = CreateNewTab(fileName, simulations, onLeftTabControl);
                    // Add to MRU list and update display
                    Utility.Configuration.Settings.AddMruFile(fileName);
                    UpdateMRUDisplay();
                }

                catch (Exception err)
                {
                    this.view.ShowMessage(err.Message, Simulation.ErrorLevel.Error);
                }

                view.ShowWaitCursor(false);
            }

            return presenter;
        }

        /// <summary>
        /// Updates display of the list of most-recently-used files
        /// </summary>
        public void UpdateMRUDisplay()
        {
            view.StartPage1.List.Values = Utility.Configuration.Settings.MruList.ToArray();
            view.StartPage2.List.Values = Utility.Configuration.Settings.MruList.ToArray();
            Utility.Configuration.Settings.Save();
        }

        /// <summary>Open an .apsimx file into the current tab.</summary>
        /// <param name="name">Name of the simulation</param>
        /// <param name="contents">The xml content</param>
        /// <param name="onLeftTabControl">If true a tab will be added to the left hand tab control.</param>
        private void OpenApsimXFromMemoryInTab(string name, string contents, bool onLeftTabControl)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(contents);
            Simulations simulations = Simulations.Read(doc.DocumentElement);
            CreateNewTab(name, simulations, onLeftTabControl);
        }

        /// <summary>Create a new tab.</summary>
        /// <param name="name">Name of the simulation</param>
        /// <param name="simulations">The simulations object to add to tab.</param>
        /// <param name="onLeftTabControl">If true a tab will be added to the left hand tab control.</param>
        private ExplorerPresenter CreateNewTab(string name, Simulations simulations, bool onLeftTabControl)
        {
            this.view.ShowMessage(" ", Simulation.ErrorLevel.Information); // Clear the message window
            ExplorerView explorerView = new ExplorerView(null);
            ExplorerPresenter presenter = new ExplorerPresenter(this);
            if (onLeftTabControl)
                presenters1.Add(presenter);
            else
                presenters2.Add(presenter);

            XmlDocument doc = new XmlDocument();
            presenter.Attach(simulations, explorerView, null);

            this.view.AddTab(name, new Gtk.Image(null, "ApsimNG.Resources.apsim logo32.png"), explorerView.MainWidget, onLeftTabControl);

            // restore the simulation tree width on the form
            if (simulations.ExplorerWidth == 0)
                presenter.TreeWidth = 250;
            else
                presenter.TreeWidth = simulations.ExplorerWidth;

            return presenter;
        }

        /// <summary>
        /// Event handler invoked when user clicks on 'Open ApsimX file'
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event parameters</param>
        private void OnOpenApsimXFile(object sender, EventArgs e)
        {
            string fileName = view.AskUserForOpenFileName("*.apsimx|*.apsimx");
            if (fileName != null)
            {
                bool onLeftTabControl = this.view.IsControlOnLeft(sender);
                this.OpenApsimXFileInTab(fileName, onLeftTabControl);
                Utility.Configuration.Settings.PreviousFolder = Path.GetDirectoryName(fileName);
            }
        }

        /// <summary>
        /// Open a recently used file
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnFileDoubleClicked(object sender, EventArgs e)
        {
            bool onLeftTabControl = this.view.IsControlOnLeft(sender);
            string fileName = onLeftTabControl ? view.StartPage1.List.SelectedValue : view.StartPage2.List.SelectedValue;
            if (fileName != null)
            {
                this.OpenApsimXFileInTab(fileName, onLeftTabControl);
                Utility.Configuration.Settings.PreviousFolder = Path.GetDirectoryName(fileName);
            }
        }

        /// <summary>
        /// Current tab is closing - remove presenter from our presenters list
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnTabClosing(object sender, TabClosingEventArgs e)
        {
            if (e.LeftTabControl)
            {
                e.AllowClose = this.presenters1[e.Index - 1].SaveIfChanged();
                if (e.AllowClose)
                {
                    this.presenters1[e.Index - 1].Detach();
                    this.presenters1.RemoveAt(e.Index - 1);
                }
            }
            else
            {
                e.AllowClose = this.presenters2[e.Index - 1].SaveIfChanged();
                if (e.AllowClose)
                {
                    this.presenters2[e.Index - 1].Detach();
                    this.presenters2.RemoveAt(e.Index - 1);
                }
            }
        }

        /// <summary>Event handler invoked when user clicks on 'Standard toolbox'</summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        public void OnStandardToolboxClick(object sender, EventArgs e)
        {
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("ApsimNG.Resources.Toolboxes.StandardToolbox.apsimx");
            StreamReader streamReader = new StreamReader(s);
            bool onLeftTabControl = true;
            if (sender != null)
                onLeftTabControl = this.view.IsControlOnLeft(sender);
            this.OpenApsimXFromMemoryInTab("Standard toolbox", streamReader.ReadToEnd(), onLeftTabControl);
        }

        /// <summary>
        /// Event handler invoked when user clicks on 'Management toolbox'
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnManagementToolboxClick(object sender, EventArgs e)
        {
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("ApsimNG.Resources.Toolboxes.ManagementToolbox.apsimx");
            StreamReader streamReader = new StreamReader(s);
            bool onLeftTabControl = this.view.IsControlOnLeft(sender);
            this.OpenApsimXFromMemoryInTab("Management toolbox", streamReader.ReadToEnd(), onLeftTabControl);
        }

        /// <summary>
        /// Event handler invoked when user clicks on 'Training toolbox'
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnTrainingToolboxClick(object sender, EventArgs e)
        {
            try
            {
                Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("ApsimNG.Resources.Toolboxes.TrainingToolbox.apsimx");
                StreamReader streamReader = new StreamReader(s);
                bool onLeftTabControl = this.view.IsControlOnLeft(sender);
                this.OpenApsimXFromMemoryInTab("Training toolbox", streamReader.ReadToEnd(), onLeftTabControl);
            }
            catch (Exception err)
            {
                string message = err.Message;
                if (err.InnerException != null)
                    message += "\r\n" + err.InnerException.Message;

                view.ShowMessage(message, Simulation.ErrorLevel.Error);
            }
        }

        /// <summary>
        /// Event handler invoked when user clicks on 'Import'
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnImport(object sender, EventArgs e)
        {
            string fileName = view.AskUserForOpenFileName("*.apsim|*.apsim");

            APSIMImporter importer = new APSIMImporter();
            try
            {
                view.ShowWaitCursor(true);
                try
                {
                    importer.ProcessFile(fileName);

                    string newFileName = Path.ChangeExtension(fileName, ".apsimx");
                    bool onLeftTabControl = this.view.IsControlOnLeft(sender);
                    this.OpenApsimXFileInTab(newFileName, onLeftTabControl);
                }
                finally
                {
                    view.ShowWaitCursor(false);
                }
            }
            catch (Exception exp)
            {
                throw new Exception("Failed import: " + exp.Message);
            }
        }

        /// <summary>
        /// Open a file open dialog with the initial directory in an Examples directory.
        /// Use one that is at the same level as this app directory.
        /// Any files opened here will need to be saved before running.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnExample(object sender, EventArgs e)
        {
            string initialPath;

            if ((this.lastExamplesPath != null) && (this.lastExamplesPath.Length > 0) && Directory.Exists(this.lastExamplesPath))
            {
                initialPath = this.lastExamplesPath; // use the last used path in this session
            }
            else
            {
                // use an examples directory relative to this assembly
                initialPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                initialPath = Path.GetFullPath(Path.Combine(initialPath, "..", "Examples"));
            }
            string fileName = view.AskUserForOpenFileName("*.apsimx|*.apsimx", initialPath);

            if (fileName != null)
            {
                this.lastExamplesPath = Path.GetDirectoryName(fileName);

                // ensure that they are saved in another file before running by opening them in memory
                StreamReader reader = new StreamReader(fileName);
                bool onLeftTabControl = this.view.IsControlOnLeft(sender);
                string label = Path.GetFileNameWithoutExtension(fileName) + " (example)";
                this.OpenApsimXFromMemoryInTab(label, reader.ReadToEnd(), onLeftTabControl);
                reader.Close();
            }
        }

        /// <summary>
        /// Upgrade Apsim Next Generation
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnUpgrade(object sender, EventArgs e)
        {
            // Get the version of the current assembly.
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version.Revision == 0)
                view.ShowMessage("You are on a custom build. You cannot upgrade.", Simulation.ErrorLevel.Error);
            else
            {
                if (AllowClose())
                {
                    Forms.UpgradeForm form = new Forms.UpgradeForm(view);
                    form.Show();
                }
            }
        }

        /// <summary>Application is closing - allow this to happen?</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Close arguments</param>
        private void OnClosing(object sender, AllowCloseArgs e)
        {
            e.AllowClose = AllowClose();
            if (e.AllowClose)
            {
                Utility.Configuration.Settings.MainFormLocation = view.WindowLocation;
                Utility.Configuration.Settings.MainFormSize = view.WindowSize;
                Utility.Configuration.Settings.MainFormMaximized = view.WindowMaximised;
            }
        }

        /// <summary>
        /// Closes the tab containing a specified object
        /// </summary>
        /// <param name="o">The object (normally a Gtk Widget) being sought</param>
        public void CloseTabContaining(object o)
        {
            view.CloseTabContaining(o);
        }
    }
}
