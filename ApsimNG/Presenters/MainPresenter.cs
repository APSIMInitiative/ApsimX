using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using APSIM.Shared.Utilities;
using UserInterface.Interfaces;
using Models.Core;
using UserInterface.Views;
using System.Linq;
using UserInterface.EventArguments;
using Utility;
using Models.Core.ApsimFile;
using Models.Core.Apsim710File;

namespace UserInterface.Presenters
{

    /// <summary>
    /// This presenter class provides the functionality behind a TabbedExplorerView
    /// which is a tab control where each tabs represent an .apsimx file. Each tab
    /// then has an ExplorerPresenter and ExplorerView created when the tab is
    /// created.
    /// </summary>
    public class MainPresenter
    {
        private class MockModel : Model { }

        /// <summary>A list of presenters for tabs on the left.</summary>
        public List<IPresenter> Presenters1 { get; set; } = new List<IPresenter>();

        /// <summary>A private reference to the view this presenter will talk to.</summary>
        private IMainView view;

        /// <summary>A list of presenters for tabs on the right.</summary>
        private List<IPresenter> presenters2 = new List<IPresenter>();

        /// <summary>
        /// View used to show help information.
        /// </summary>
        private HelpView helpView = null;

        /// <summary>
        /// The most recent exception that has been thrown.
        /// </summary>
        public List<string> LastError { get; private set; } = new List<string>();

        /// <summary>Attach this presenter with a view. Can throw if there are errors during startup.</summary>
        /// <param name="view">The view to attach</param>
        /// <param name="commandLineArguments">Optional command line arguments - can be null</param>
        public void Attach(object view, string[] commandLineArguments)
        {
            this.view = view as IMainView;
            this.view.OnError += OnError;
            // Set the main window location and size.
            this.view.WindowLocation = Utility.Configuration.Settings.MainFormLocation;
            this.view.WindowSize = Utility.Configuration.Settings.MainFormSize;
            // Maximize settings do not save correctly on OS X
            if (!ProcessUtilities.CurrentOS.IsMac)
                this.view.WindowMaximised = Utility.Configuration.Settings.MainFormMaximized;

            // Set the main window caption with version information.
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version.Major == 0)
            {
                this.view.WindowCaption = "APSIM (Custom Build)";
            }
            else
            {
                this.view.WindowCaption = "APSIM " + version;
            }

            // Cleanup the recent file list
            Utility.Configuration.Settings.CleanMruList();

            // Populate the 2 start pages.
            this.PopulateStartPage(this.view.StartPage1);
            this.PopulateStartPage(this.view.StartPage2);

            // Trap some events.
            this.view.AllowClose += this.OnClosing;
            this.view.StartPage1.List.DoubleClicked += this.OnFileDoubleClicked;
            this.view.StartPage2.List.DoubleClicked += this.OnFileDoubleClicked;
            this.view.TabClosing += this.OnTabClosing;
            this.view.ShowDetailedError += this.ShowDetailedErrorMessage;
            this.view.Show();

            int height = this.view.PanelHeight;
            double savedHeight = Utility.Configuration.Settings.StatusPanelHeight / 100.0;
            if (savedHeight > 0.9 || savedHeight < 0.1)
                this.view.StatusPanelPosition = (int)Math.Round(height * 0.7);
            else
                this.view.StatusPanelPosition = (int)Math.Round(height * savedHeight);

            double width = this.view.WindowSize.Width;
            int savedWidth = Utility.Configuration.Settings.SplitScreenPosition;
            if (savedWidth > 0.9 || savedWidth < 0.1)
                this.view.SplitScreenPosition = (int)Math.Round(width * 0.5);
            else
                this.view.SplitScreenPosition = (int)Math.Round(width * savedWidth);

            // Process command line.
            this.ProcessCommandLineArguments(commandLineArguments);
        }

        /// <summary>
        /// Detach this presenter from the view.
        /// </summary>
        /// <param name="view">The view used for this object</param>
        public void Detach(object view)
        {
            this.view.AllowClose -= this.OnClosing;
            this.view.StartPage1.List.DoubleClicked -= this.OnFileDoubleClicked;
            this.view.StartPage2.List.DoubleClicked -= this.OnFileDoubleClicked;
            this.view.TabClosing -= this.OnTabClosing;
            this.view.OnError -= OnError;
            this.view.ShowDetailedError -= ShowDetailedErrorMessage;
        }

        /// <summary>
        /// Allow the form to close?
        /// </summary>
        /// <returns>True if can be closed</returns>
        public bool AllowClose()
        {
            bool ok = true;

            for (int i = 0; i < Presenters1.Count; i++)
            {
                if (Presenters1[i] is ExplorerPresenter presenter)
                {
                    if (presenter.SaveIfChanged())
                    {
                        CloseTab(i, true);
                        i--;
                    }
                    else
                        ok = false;
                }
                else
                {
                    CloseTab(i, true);
                    i--;
                }
            }

            for (int i = 0; i < presenters2.Count; i++)
            {
                if (presenters2[i] is ExplorerPresenter presenter)
                {
                    if (presenter.SaveIfChanged())
                    {
                        CloseTab(i, false);
                        i--;
                    }
                    else
                        ok = false;
                }
                else
                {
                    CloseTab(i, false);
                    i--;
                }
            }

            return ok;
        }

        /// <summary>
        /// Toggle split screen view.
        /// </summary>
        public void ToggleSecondExplorerViewVisible()
        {
            this.view.SplitWindowOn = !this.view.SplitWindowOn;
        }

        /// <summary>
        /// Execute the specified script, returning any error messages or NULL if all OK.
        /// </summary>
        /// <param name="code">The script code</param>
        /// <returns>Any exception message or null</returns>
        public void ProcessStartupScript(string code)
        {
            // Allow UI scripts to reference gtk/gdk/etc
            List<string> assemblies = new List<string>()
            {
                typeof(Gtk.Widget).Assembly.Location,
                typeof(Gdk.Color).Assembly.Location,
                typeof(Cairo.Context).Assembly.Location,
                typeof(Pango.Context).Assembly.Location,
                typeof(GLib.Log).Assembly.Location,
            };

            var compiler = new ScriptCompiler();

            var results = compiler.Compile(code, new MockModel());

            if (results.ErrorMessages != null)
                throw new Exception($"Script compile errors: {results.ErrorMessages}");

            // Create a new script model.
            object script = results.Instance;

            // Look for a method called Execute
            MethodInfo executeMethod = script.GetType().GetMethod("Execute");
            if (executeMethod == null)
                throw new Exception("Cannot find a method Script.Execute");

            // Call Execute on our newly created script instance.
            object[] arguments = new object[] { this };
            executeMethod.Invoke(script, arguments);
        }

        /// <summary>
        /// Clears the status panel.
        /// </summary>
        public void ClearStatusPanel()
        {
            LastError.Clear();
            view.ClearStatusPanel();
        }

        /// <summary>
        /// Add a status message. A message of null will clear the status message.
        /// For error messages, use <see cref="ShowError(Exception, bool)"/>.
        /// </summary>
        /// <param name="message">The message test</param>
        /// <param name="messageType">The error level value</param>
        /// <param name="overwrite">Overwrite existing messages?</param>
        public void ShowMessage(string message, Simulation.MessageType messageType, bool overwrite = true)
        {
            MessageType errorType = MessageType.Information;

            if (messageType == Simulation.MessageType.Information)
                errorType = MessageType.Information;
            else if (messageType == Simulation.MessageType.Warning)
                errorType = MessageType.Warning;

            this.view.ShowMessage(message, errorType, overwrite);
        }

        public void HideProgressBar()
        {
            view.HideProgressBar();
        }

        /// <summary>
        /// Displays several messages, with a separator between them.
        /// For error messages, use <see cref="ShowError(List{Exception}, bool)"/>.
        /// </summary>
        /// <param name="messages">Messages to be displayed.</param>
        /// <param name="messageType"></param>
        public void ShowMessage(List<string> messages, Simulation.MessageType messageType)
        {
            MessageType errorType = MessageType.Information;

            if (messageType == Simulation.MessageType.Information)
                errorType = MessageType.Information;
            else if (messageType == Simulation.MessageType.Warning)
                errorType = MessageType.Warning;

            foreach (string msg in messages)
            {
                view.ShowMessage(msg, errorType, false, true, false);
            }
        }

        /// <summary>
        /// Displays a simple error message in the status bar, without a 'more information' button.
        /// If you've just caught an exception, <see cref="ShowError(Exception, bool)"/> is probably a better method to use.
        /// </summary>
        /// <param name="error"></param>
        public void ShowError(string error)
        {
            LastError.Clear();
            view.ShowMessage(error, MessageType.Error, withButton : false);
        }

        /// <summary>
        /// Displays an error message in the status bar, along with a button which brings up more info.
        /// </summary>
        /// <param name="error">The error to be displayed.</param>
        /// <param name="overwrite">Overwrite any existing error messages?</param>
        public void ShowError(Exception error, bool overwrite = true)
        {
            if (overwrite)
                LastError.Clear();
            if (error != null)
            {
                if (view == null)
                {
                    // This can happen when CreateDocumentation.exe is the main program.
                    Console.WriteLine(error.ToString());
                }
                else
                {
                    LastError.Add(error.ToString());
                    view.ShowMessage(GetExceptionMessage(error), MessageType.Error, overwrite: overwrite, addSeparator: !overwrite);
                }
            }
        }

        /// <summary>
        /// Displays several error messages in the status bar. Each error will have an associated
        /// </summary>
        /// <param name="errors"></param>
        /// <param name="overwrite"></param>
        public void ShowError(List<Exception> errors, bool overwrite = true)
        {
            // if the list contains at least 1 null exception, display none of them
            if (errors != null && !errors.Contains(null))
            {
                LastError = errors.Select(err => err.ToString()).ToList();
                for (int i = 0; i < errors.Count; i++)
                {
                    if (errors[i] is AggregateException aggregate)
                        ShowError(aggregate.InnerExceptions.ToList(), overwrite && i == 0);
                    else
                        // only overwrite other messages the first time through the loop
                        view.ShowMessage(GetExceptionMessage(errors[i]), MessageType.Error, overwrite && i == 0, true);
                }
            }
            else
            {
                LastError.Clear();
                ShowError(new NullReferenceException("Attempted to display a null error"));
            }
        }

        /// <summary>
        /// Get an exception's message along with the messages of any inner
        /// exceptions in a format suitable for display in the GUI.
        /// </summary>
        /// <param name="exception">The exception.</param>
        private string GetExceptionMessage(Exception exception)
        {
            if (exception.InnerException == null)
                return exception.Message;

            string innerMessage = GetExceptionMessage(exception.InnerException);

            if (string.IsNullOrEmpty(exception.Message))
                return innerMessage;

            // AggregateExceptions will include all inner exceptions' messages
            // in their message. Therefore we don't need to fetch the inner
            // exceptions' messages in such cases.
            if (string.IsNullOrEmpty(innerMessage) || exception is AggregateException)
                return exception.Message;

            return $"{exception.Message} --> {innerMessage}";
        }

        /// <summary>
        /// Shows a window which contains detailed error information (such as stack trace).
        /// </summary>
        /// <param name="sender">Sender object. Must be an <see cref="ApsimNG.Classes.CustomButton"/></param>
        /// <param name="e">Event Arguments.</param>
        private void ShowDetailedErrorMessage(object sender, EventArgs e)
        {
            if (sender is ApsimNG.Classes.CustomButton)
            {
                ErrorView err = new ErrorView(LastError[(sender as ApsimNG.Classes.CustomButton).ID], view as ViewBase);
                err.Show();
            }
        }

        /// <summary>
        /// Show a message in a dialog box
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="title">The dialog title</param>
        /// <param name="msgType">The type of dialog message</param>
        /// <param name="buttonType">Button type</param>
        /// <returns>The message dialog return value</returns>
        public int ShowMsgDialog(string message, string title, Gtk.MessageType msgType, Gtk.ButtonsType buttonType)
        {
            return this.view.ShowMsgDialog(message, title, msgType, buttonType);
        }

        /// <summary>
        /// Show progress bar with the specified percent.
        /// </summary>
        /// <param name="progress">The progress (0 - 1)</param>
        /// <param name="showStopButton">Should a stop button be displayed as well?</param>
        public void ShowProgress(double progress, bool showStopButton = true)
        {
            this.view.ShowProgress(progress, showStopButton);
        }

        /// <summary>
        /// Add a status message. A message of null will clear the status message.
        /// For error messages, use <see cref="ShowError(Exception, bool)"/>.
        /// </summary>
        /// <param name="message">The message test</param>
        /// <param name="messageType">The error level value</param>
        public void ShowProgressMessage(string message)
        {
            view.ShowProgressMessage(message);
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
        /// Remove a handler for the "stop" button of the view.
        /// </summary>
        /// <param name="handler">The handler to be removed</param>
        public void RemoveStopHandler(EventHandler<EventArgs> handler)
        {
            this.view.StopSimulation -= handler;
        }

        /// <summary>
        /// Show the wait cursor.
        /// </summary>
        /// <param name="wait">If true will show the wait cursor otherwise the normal cursor.</param>
        public void ShowWaitCursor(bool wait)
        {
            this.view.ShowWaitCursor(wait);
        }

        /// <summary>
        /// Change the text of a tab.
        /// </summary>
        /// <param name="ownerView">The owning view.</param>
        /// <param name="newTabName">New text of the tab.</param>
        /// <param name="tooltip">The tooltip text.</param>
        public void ChangeTabText(object ownerView, string newTabName, string tooltip)
        {
            this.view.ChangeTabText(ownerView, newTabName, tooltip);
        }

        /// <summary>
        /// Close the application.
        /// </summary>
        /// <param name="askToSave">Prompt to save</param>
        public void Close(bool askToSave)
        {
            this.view.Close(askToSave);
        }

        /// <summary>
        /// Ask the user a question.
        /// </summary>
        /// <param name="message">The message to show the user.</param>
        /// <returns>A response value.</returns>
        public QuestionResponseEnum AskQuestion(string message)
        {
            return this.view.AskQuestion(message);
        }

        /// <summary>
        /// Ask user for a filename to open.
        /// </summary>
        /// <param name="fileSpec">The file specification to use to filter the files.</param>
        /// <param name="initialDirectory">Optional Initial starting directory.</param>
        /// <returns>A filename.</returns>
        public string AskUserForOpenFileName(string fileSpec, string initialDirectory = "")
        {
            IFileDialog fileChooser = new FileDialog()
            {
                Action = FileDialog.FileActionType.Open,
                FileType = fileSpec,
                InitialDirectory = initialDirectory,
            };
            return fileChooser.GetFile();
        }

        /// <summary>
        /// A helper function that asks user for a SaveAs name and returns their new choice.
        /// </summary>
        /// <param name="fileSpec">The file specification to filter the files.</param>
        /// <param name="oldFilename">The current file name.</param>
        /// <returns>Returns the new file name or null if action cancelled by user.</returns>
        public string AskUserForSaveFileName(string fileSpec, string oldFilename)
        {
            IFileDialog fileChooser = new FileDialog()
            {
                Action = FileDialog.FileActionType.Save,
                FileType = fileSpec,
            };
            return fileChooser.GetFile();
        }

        /// <summary>Open an .apsimx file into the current tab.</summary>
        /// <param name="fileName">The file to open.</param>
        /// <param name="onLeftTabControl">If true a tab will be added to the left hand tab control.</param>
        /// <returns>The presenter.</returns>
        public ExplorerPresenter OpenApsimXFileInTab(string fileName, bool onLeftTabControl)
        {
            ExplorerPresenter presenter = null;
            if (fileName != null)
            {
                presenter = this.PresenterForFile(fileName, onLeftTabControl);
                if (presenter != null)
                {
                    this.view.SelectTabContaining(presenter.GetView().MainWidget);
                    return presenter;
                }

                this.view.ShowWaitCursor(true);
                try
                {
                    var converter = FileFormat.ReadFromFile<Simulations>(fileName, e => ShowError(e), true);
                    Simulations simulations = converter.NewModel as Simulations;
                    presenter = (ExplorerPresenter)this.CreateNewTab(fileName, simulations, onLeftTabControl, "UserInterface.Views.ExplorerView", "UserInterface.Presenters.ExplorerPresenter");

                    // Clear simulation messages.
                    this.ShowMessage("", Simulation.MessageType.Information, true);

                    if (converter.DidConvert)
                        this.ShowMessage($"Simulation has been converted to the latest version: {simulations.Version}",Simulation.MessageType.Information,true);

                    // Add to MRU list and update display
                    Configuration.Settings.AddMruFile(new ApsimFileMetadata(fileName));
                    Configuration.Settings.Save();
                    this.UpdateMRUDisplay();
                }
                catch (Exception err)
                {
                    ShowError(err);
                }

                this.view.ShowWaitCursor(false);
            }

            return presenter;
        }

        /// <summary>
        /// Get the currently active explorer presenter instance.
        /// Return null if none are active.
        /// </summary>
        public ExplorerPresenter GetCurrentExplorerPresenter()
        {
            (int index, bool onLeft) = view.GetCurrentTab();

            // The view has an extra tab ("home") which is not included in
            // the presenters list. Our index needs to account for this
            // offset.
            index--;

            List<IPresenter> presenters = onLeft ? Presenters1 : presenters2;
            if (index < 0 || index >= presenters.Count)
                return null;

            return presenters[index] as ExplorerPresenter;
        }

        /// <summary>
        /// Updates display of the list of most-recently-used files.
        /// </summary>
        public void UpdateMRUDisplay()
        {
            this.view.StartPage1.List.Values = Configuration.Settings.MruList.Select(f => f.FileName).ToArray();
            this.view.StartPage2.List.Values = Configuration.Settings.MruList.Select(f => f.FileName).ToArray();
        }


        /// <summary>
        /// Gets the inner-most exception of an exception.
        /// </summary>
        /// <param name="error">An exception.</param>
        private Exception GetInnerException(Exception error)
        {
            Exception inner = error;
            while (inner.InnerException != null)
                inner = inner.InnerException;
            return inner;
        }

        /// <summary>
        /// Populate the view for the first time. Will throw if there are errors on startup.
        /// </summary>
        /// <param name="startPage">The start page to populate.</param>
        private void PopulateStartPage(IListButtonView startPage)
        {
            // Add the buttons into the main window.
            startPage.AddButton(
                                "Open APSIM File",
                                          new Gtk.Image(null, "ApsimNG.Resources.Toolboxes.OpenFile.svg"),
                                          this.OnOpenApsimXFile);

            startPage.AddButton(
                                "Open an example",
                                          new Gtk.Image(null, "ApsimNG.Resources.Toolboxes.OpenExample.svg"),
                                          this.OnExample);


            startPage.AddButton(
                                "Management toolbox",
                                          new Gtk.Image(null, "ApsimNG.Resources.Toolboxes.Toolbox.svg"),
                                          this.OnManagementToolboxClick);

            startPage.AddButton(
                                "Training toolbox",
                                          new Gtk.Image(null, "ApsimNG.Resources.Toolboxes.Toolbox.svg"),
                                          this.OnTrainingToolboxClick);

            startPage.AddButton(
                                "Import old .apsim file",
                                          new Gtk.Image(null, "ApsimNG.Resources.Toolboxes.Import.svg"),
                                          this.OnImport);

            startPage.AddButton(
                                "Upgrade",
                                        new Gtk.Image(null, "ApsimNG.Resources.MenuImages.Upgrade.svg"),
                                        this.OnUpgrade);

            startPage.AddButton(
                                "View Cloud Jobs",
                                        new Gtk.Image(null, "ApsimNG.Resources.Cloud.svg"),
                                        this.OnViewCloudJobs);

#if DEBUG
            startPage.AddButton(
                                "Upgrade Resource Files",
                                new Gtk.Image(null, "ApsimNG.Resources.MenuImages.Upgrade.svg"),
                                this.OnShowConverter);
#endif

            startPage.AddButton(
                                "Settings",
                                new Gtk.Image(null, "ApsimNG.Resources.MenuImages.Settings.svg"),
                                OnShowSettingsDialog);
            startPage.AddButton(
                            "Help",
                            new Gtk.Image(null, "ApsimNG.Resources.MenuImages.Help.svg"),
                            this.OnHelp);
            // Populate the view's listview.
            startPage.List.Values = Configuration.Settings.MruList.Select(f => f.FileName).ToArray();

            this.PopulatePopup(startPage);
        }

        private void OnShowSettingsDialog(object sender, EventArgs e)
        {
            try
            {
                SettingsDialog dialog = new SettingsDialog(((ViewBase)view).MainWidget as Gtk.Window);
                dialog.ShowAll();
                dialog.Run();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Defines the list of items to be included in a popup menu for the
        /// most-recently-used file display.
        /// </summary>
        /// <param name="startPage">The page to which the menu will be added.</param>
        private void PopulatePopup(IListButtonView startPage)
        {
            List<MenuDescriptionArgs> descriptions = new List<MenuDescriptionArgs>();
            MenuDescriptionArgs descOpen = new MenuDescriptionArgs();
            descOpen.Name = "Open";
            descOpen.Enabled = true;
            descOpen.OnClick = this.OnOpen;
            descriptions.Add(descOpen);

            MenuDescriptionArgs descRemove = new MenuDescriptionArgs();
            descRemove.Name = "Remove from recent file list";
            descRemove.Enabled = true;
            descRemove.OnClick = this.OnRemove;
            descriptions.Add(descRemove);

            MenuDescriptionArgs descClear = new MenuDescriptionArgs();
            descClear.Name = "Clear recent file list";
            descClear.Enabled = true;
            descClear.OnClick = this.OnClear;
            descriptions.Add(descClear);

            MenuDescriptionArgs descRename = new MenuDescriptionArgs();
            descRename.Name = "Rename";
            descRename.Enabled = true;
            descRename.OnClick = this.OnRename;
            descriptions.Add(descRename);

            MenuDescriptionArgs descCopy = new MenuDescriptionArgs();
            descCopy.Name = "Copy";
            descCopy.Enabled = true;
            descCopy.OnClick = this.OnCopy;
            descriptions.Add(descCopy);

            MenuDescriptionArgs descDelete = new MenuDescriptionArgs();
            descDelete.Name = "Delete";
            descDelete.Enabled = true;
            descDelete.OnClick = this.OnDelete;
            descriptions.Add(descDelete);

            startPage.List.PopulateContextMenu(descriptions);
        }

        /// <summary>
        /// Handles the Open menu item of the MRU context menu.
        /// </summary>
        /// <param name="obj">The object issuing the event.</param>
        /// <param name="args">Event parameters.</param>
        private void OnOpen(object obj, EventArgs args)
        {
            try
            {
                string fileName = this.view.GetMenuItemFileName(obj);
                if (fileName != null)
                {
                    this.OpenApsimXFileInTab(fileName, this.view.IsControlOnLeft(obj));
                    Utility.Configuration.Settings.PreviousFolder = Path.GetDirectoryName(fileName);
                    Configuration.Settings.Save();
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Handles the Remove menu item of the MRU context menu.
        /// </summary>
        /// <param name="obj">The object issuing the event.</param>
        /// <param name="args">Event parameters.</param>
        private void OnRemove(object obj, EventArgs args)
        {
            try
            {
                string fileName = this.view.GetMenuItemFileName(obj);
                if (!string.IsNullOrEmpty(fileName))
                {
                    Utility.Configuration.Settings.DelMruFile(fileName);
                    Configuration.Settings.Save();
                    this.UpdateMRUDisplay();
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Handles the Clear menu item of the MRU context menu.
        /// </summary>
        /// <param name="obj">The object issuing the event.</param>
        /// <param name="args">Event parameters.</param>
        private void OnClear(object obj, EventArgs args)
        {
            try
            {
                if (this.AskQuestion("Are you sure you want to completely clear the list of recently used files?") == QuestionResponseEnum.Yes)
                {
                    string[] mruFiles = Configuration.Settings.MruList.Select(f => f.FileName).ToArray();
                    foreach (string fileName in mruFiles)
                    {
                        Utility.Configuration.Settings.DelMruFile(fileName);
                    }
                    Configuration.Settings.Save();

                    this.UpdateMRUDisplay();
                }

            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Handles the Rename menu item of the MRU context menu.
        /// </summary>
        /// <param name="obj">The object issuing the event.</param>
        /// <param name="args">Event parameters.</param>
        private void OnRename(object obj, EventArgs args)
        {
            try
            {
                string fileName = this.view.GetMenuItemFileName(obj);
                if (!string.IsNullOrEmpty(fileName))
                {
                    string newName = this.AskUserForSaveFileName("ApsimX files|*.apsimx", fileName);
                    if (!string.IsNullOrEmpty(newName) && newName != fileName)
                    {
                        try
                        {
                            File.Move(fileName, newName);
                            Utility.Configuration.Settings.RenameMruFile(fileName, newName);
                            Configuration.Settings.Save();
                            this.UpdateMRUDisplay();
                        }
                        catch (Exception e)
                        {
                            ShowError(new Exception("Error renaming file!", e));
                        }
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Handles the Copy menu item of the MRU context menu.
        /// </summary>
        /// <param name="obj">The object issuing the event.</param>
        /// <param name="args">Event parameters.</param>
        private void OnCopy(object obj, EventArgs args)
        {
            try
            {
                string fileName = this.view.GetMenuItemFileName(obj);
                if (!string.IsNullOrEmpty(fileName))
                {
                    string newFileName = "Copy of " + Path.GetFileName(fileName);
                    string newFilePath = Path.Combine(Path.GetDirectoryName(fileName), newFileName);
                    string copyName = this.AskUserForSaveFileName("ApsimX files|*.apsimx", newFilePath);
                    if (!string.IsNullOrEmpty(copyName))
                    {
                        try
                        {
                            File.Copy(fileName, copyName);
                            Configuration.Settings.AddMruFile(new ApsimFileMetadata(copyName));
                            Configuration.Settings.Save();
                            this.UpdateMRUDisplay();
                        }
                        catch (Exception e)
                        {
                            ShowError(new Exception("Error creating copy of file!", e));
                        }
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Handles the Delete menu item of the MRU context menu.
        /// </summary>
        /// <param name="obj">The object issuing the event.</param>
        /// <param name="args">Event parameters.</param>
        private void OnDelete(object obj, EventArgs args)
        {
            try
            {
                string fileName = this.view.GetMenuItemFileName(obj);
                if (!string.IsNullOrEmpty(fileName))
                {
                    if (this.AskQuestion("Are you sure you want to completely delete the file " + StringUtilities.PangoString(fileName) + "?") == QuestionResponseEnum.Yes)
                    {
                        try
                        {
                            File.Delete(fileName);
                            Utility.Configuration.Settings.DelMruFile(fileName);
                            Configuration.Settings.Save();
                            this.UpdateMRUDisplay();
                        }
                        catch (Exception e)
                        {
                            ShowError(new Exception("Error deleting file!", e));
                        }
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Process the specified command line arguments. Will throw if there are errors during startup.
        /// </summary>
        /// <param name="commandLineArguments">Optional command line arguments - can be null.</param>
        private void ProcessCommandLineArguments(string[] commandLineArguments)
        {
            // Look for a script specified on the command line.
            if (commandLineArguments == null)
                return;

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
        /// or null if the file is not currently open.
        /// </summary>
        /// <param name="fileName">The file name being sought.</param>
        /// <param name="onLeftTabControl">If true, search the left screen, else search the right.</param>
        /// <returns>The explorer presenter.</returns>
        private ExplorerPresenter PresenterForFile(string fileName, bool onLeftTabControl)
        {
            List<ExplorerPresenter> presenters = onLeftTabControl ? this.Presenters1.OfType<ExplorerPresenter>().ToList() : this.presenters2.OfType<ExplorerPresenter>().ToList();
            foreach (ExplorerPresenter presenter in presenters)
            {
                if (presenter.ApsimXFile.FileName == fileName)
                {
                    return presenter;
                }
            }

            return null;
        }

        /// <summary>Open an .apsimx file into the current tab.</summary>
        /// <param name="name">Name of the simulation.</param>
        /// <param name="contents">The xml content.</param>
        /// <param name="onLeftTabControl">If true a tab will be added to the left hand tab control.</param>
        private void OpenApsimXFromMemoryInTab(string name, string contents, bool onLeftTabControl)
        {
            var simulations = FileFormat.ReadFromString<Simulations>(contents, e => throw e, true).NewModel as Simulations;
            this.CreateNewTab(name, simulations, onLeftTabControl, "UserInterface.Views.ExplorerView", "UserInterface.Presenters.ExplorerPresenter");
        }

        /// <summary>Create a new tab.</summary>
        /// <param name="name">Name of the simulation.</param>
        /// <param name="simulations">The simulations object to add to tab.</param>
        /// <param name="onLeftTabControl">If true a tab will be added to the left hand tab control.</param>
        /// <param name="viewName">Name of the view to create.</param>
        /// <param name="presenterName">Name of the presenter to create.</param>
        /// <returns>The explorer presenter.</returns>
        private IPresenter CreateNewTab(string name, Simulations simulations, bool onLeftTabControl, string viewName, string presenterName)
        {
            ViewBase newView;
            IPresenter newPresenter;
            try
            {
                if (viewName.Contains(".glade"))
                    newView = new ViewBase(view as ViewBase, viewName);
                else
                    newView = (ViewBase)Assembly.GetExecutingAssembly().CreateInstance(viewName, false, BindingFlags.Default, null, new object[] { this.view }, null, null);
                newPresenter = (IPresenter)Assembly.GetExecutingAssembly().CreateInstance(presenterName, false, BindingFlags.Default, null, new object[] { this }, null, null);
            }
            catch (InvalidCastException e)
            {
                // viewName or presenterName does not define a class derived from ViewBase or IPresenter, respectively.
                ShowError(e);
                return null;
            }

            //ExplorerView explorerView = new ExplorerView(null);
            //ExplorerPresenter presenter = new ExplorerPresenter(this);
            if (onLeftTabControl)
            {
                this.Presenters1.Add(newPresenter);
            }
            else
            {
                this.presenters2.Add(newPresenter);
            }

            newPresenter.Attach(simulations, newView, null);

            this.view.AddTab(name, null, newView.MainWidget, onLeftTabControl);

            // restore the simulation tree width on the form
            if (newPresenter.GetType() == typeof(ExplorerPresenter))
            {
                int width = this.view.WindowSize.Width;
                double savedWidth = Utility.Configuration.Settings.TreeSplitScreenPosition;
                if ((savedWidth > 0.9) || (savedWidth < 0.1))
                    ((ExplorerPresenter)newPresenter).TreeWidth = (int)Math.Round(width * 0.9);
                else
                    ((ExplorerPresenter)newPresenter).TreeWidth = (int)Math.Round(width * savedWidth);
            }
            return newPresenter;
        }

        /// <summary>
        /// Event handler invoked when user clicks on 'Open ApsimX file'.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event parameters.</param>
        private void OnOpenApsimXFile(object sender, EventArgs e)
        {
            try
            {
                string fileName = this.AskUserForOpenFileName("ApsimX files|*.apsimx");
                if (fileName != null)
                {
                    bool onLeftTabControl = this.view.IsControlOnLeft(sender);
                    OpenApsimXFileInTab(fileName, onLeftTabControl);
                    Configuration.Settings.PreviousFolder = Path.GetDirectoryName(fileName);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Open a recently used file.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnFileDoubleClicked(object sender, EventArgs e)
        {
            try
            {
                bool onLeftTabControl = this.view.IsControlOnLeft(sender);
                string fileName = onLeftTabControl ? this.view.StartPage1.List.SelectedValue : this.view.StartPage2.List.SelectedValue;
                if (fileName != null)
                {
                    OpenApsimXFileInTab(fileName, onLeftTabControl);
                    Configuration.Settings.PreviousFolder = Path.GetDirectoryName(fileName);
                    Configuration.Settings.Save();
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Current tab is closing - remove presenter from our presenters list.
        /// </summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="e">Event arguments.</param>
        private void OnTabClosing(object sender, TabClosingEventArgs e)
        {
            e.AllowClose = true;

            IPresenter presenter = e.LeftTabControl ? Presenters1[e.Index - 1] : presenters2[e.Index - 1];
            if (presenter is ExplorerPresenter explorerPresenter)
                e.AllowClose = explorerPresenter.SaveIfChanged();

            if (e.AllowClose)
                CloseTab(e.Index - 1, e.LeftTabControl);
        }

        /// <summary>
        /// Close a tab (does not prompt user to save).
        /// </summary>
        /// <param name="index">0-based index of the tab.</param>
        /// <param name="onLeft">Is the tab in the left tab control?</param>
        public void CloseTab(int index, bool onLeft)
        {
            int nPages = view.PageCount(onLeft);
            List<IPresenter> presenters = onLeft ? Presenters1 : presenters2;
            presenters[index].Detach();
            presenters.RemoveAt(index);

            // Need to add an offset to account for the home tab. E.g. presenter
            // 0 (ie .apsimx file 0) will be the second tab in the notebook (ie
            // index 1).

            // The tab should have been removed by the call to Detach
            // But check to be sure a tab has actually been closed and, if not, remove it now

            if (view.PageCount(onLeft) == nPages)
                view.RemoveTab(index + 1, onLeft);

            // We've just closed Simulations
            // This is a good time to force garbage collection
            GC.Collect(2, GCCollectionMode.Forced, true);
        }

        /// <summary>
        /// Event handler invoked when user clicks on 'Management toolbox'.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnManagementToolboxClick(object sender, EventArgs e)
        {
            try
            {
                Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("ApsimNG.Resources.Toolboxes.ManagementToolbox.apsimx");
                StreamReader streamReader = new StreamReader(s);
                bool onLeftTabControl = this.view.IsControlOnLeft(sender);
                this.OpenApsimXFromMemoryInTab("Management toolbox", streamReader.ReadToEnd(), onLeftTabControl);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Event handler invoked when user clicks on 'Training toolbox'.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
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
                ShowError(err);
            }
        }

        /// <summary>
        /// Event handler invoked when user clicks on 'Import'.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnImport(object sender, EventArgs e)
        {
            try
            {
                string fileName = this.AskUserForOpenFileName("APSIM Classic Files|*.apsim");
                this.view.ShowWaitCursor(true);
                this.Import(fileName);

                string newFileName = Path.ChangeExtension(fileName, ".apsimx");
                this.OpenApsimXFileInTab(newFileName, this.view.IsControlOnLeft(sender));
                Configuration.Settings.PreviousFolder = Path.GetDirectoryName(fileName);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
            finally
            {
                this.view.ShowWaitCursor(false);
            }
        }

        /// <summary>
        /// Runs the importer on a file, then opens it in a new tab.
        /// </summary>
        /// <param name="fileName">Path to the file to be imported.</param>
        /// <param name="leftTab">Should the file be opened in the left tabset?</param>
        public void Import(string fileName)
        {
            // When an old .apsim file is imported, exceptions can occur when converting the file but the 
            // file is still converted i.e. exception isn't fatal. Catch these exceptions and show to user
            // in the 

            List<Exception> importExceptions = new();
            try
            {
                var importer = new Importer();
                importer.ProcessFile(fileName, e => { importExceptions.Add(e); });
            }
            catch (Exception err)
            {
                throw new Exception("Error during Import: " + err.Message);
            }
            string importExceptionsAsString = importExceptions.Join(Environment.NewLine);
            if (!string.IsNullOrEmpty(importExceptionsAsString))
                ShowMessage(importExceptionsAsString, Simulation.MessageType.Warning);
        }

        /// <summary>
        /// Open a tab which shows a list of jobs submitted to the cloud.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event Arguments.</param>
        public void OnViewCloudJobs(object sender, EventArgs e)
        {
            try
            {
                bool onLeftTabControl = view.IsControlOnLeft(sender);
                // Clear the message window
                view.ShowMessage(" ", MessageType.Information);
                CreateNewTab("View Cloud Jobs", null, onLeftTabControl, "ApsimNG.Resources.Glade.CloudJobView.glade", "UserInterface.Presenters.CloudJobPresenter");
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Opens the ApsimX online documentation.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnHelp(object sender, EventArgs args)
        {
            try
            {
                if (helpView == null)
                    helpView = new HelpView(view as MainView);

                helpView.Visible = true;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Open a file open dialog with the initial directory in an Examples directory.
        /// Use one that is at the same level as this app directory.
        /// Any files opened here will need to be saved before running.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnExample(object sender, EventArgs e)
        {
            try
            {
                string initialPath = PathUtilities.GetAbsolutePath(Path.Combine("%root%", "Examples"), null);
                string fileName = AskUserForOpenFileName("ApsimX files|*.apsimx", initialPath);

                if (fileName != null)
                {
                    // ensure that they are saved in another file before running by opening them in memory
                    StreamReader reader = new StreamReader(fileName);
                    bool onLeftTabControl = this.view.IsControlOnLeft(sender);
                    string label = Path.GetFileNameWithoutExtension(fileName) + " (example)";
                    this.OpenApsimXFromMemoryInTab(label, reader.ReadToEnd(), onLeftTabControl);
                    reader.Close();
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user clicks the 'choose font' button.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnChooseFont(object sender, EventArgs args)
        {
            try
            {
                view.ShowFontChooser();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Shows the file converter view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnShowConverter(object sender, EventArgs args)
        {
            try
            {
                int version = Models.Core.ApsimFile.Converter.LatestVersion;
                ClearStatusPanel();
                string apsimx = PathUtilities.GetAbsolutePath("%root%", null);
                string resources = Path.Combine(apsimx, "Models", "Resources");
                if (!Directory.Exists(resources))
                    throw new Exception("Unable to locate resources directory");
                IEnumerable<string> files = Directory.EnumerateFiles(resources, "*.json", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    if (!File.Exists(file))
                        throw new FileNotFoundException(string.Format("Unable to upgrade {0}: file does not exist.", file));

                    // Run the converter.
                    string contents = File.ReadAllText(file);
                    var converter = Converter.DoConvert(contents, version, file);
                    if (converter.DidConvert)
                    {
                        File.WriteAllText(file, converter.Root.ToString());
                        view.ShowMessage(string.Format("Successfully upgraded {0} to version {1}.", file, version), MessageType.Information, false);
                    }
                }
                view.ShowMessage("Successfully upgraded all files.", MessageType.Information);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Upgrade Apsim Next Generation.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnUpgrade(object sender, EventArgs e)
        {
            try
            {
                // Get the version of the current assembly.
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version.Build == 0)
                {
                    ShowError("You are on a custom build. You cannot upgrade.");
                }

                if (AllowClose())
                {
                    UpgradeView form = new UpgradeView(view as ViewBase);
                    form.Show();
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Application is closing - allow this to happen?</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Close arguments</param>
        private void OnClosing(object sender, AllowCloseArgs e)
        {
            int treeWidth = 0;
            if (this.GetCurrentExplorerPresenter() != null)
                treeWidth = this.GetCurrentExplorerPresenter().TreeWidth;

            e.AllowClose = this.AllowClose();
            if (e.AllowClose)
            {
                Utility.Configuration.Settings.SplitScreenPosition = (int)MathF.Round((float)this.view.SplitScreenPosition / (float)this.view.WindowSize.Width);
                Utility.Configuration.Settings.MainFormLocation = this.view.WindowLocation;
                Utility.Configuration.Settings.MainFormSize = this.view.WindowSize;
                Utility.Configuration.Settings.MainFormMaximized = this.view.WindowMaximised;
                Utility.Configuration.Settings.StatusPanelHeight = (int)(((double)this.view.StatusPanelPosition / (double)this.view.PanelHeight) * 100);
                if (treeWidth > 0)
                    Utility.Configuration.Settings.TreeSplitScreenPosition = (int)MathF.Round(((float)treeWidth / (float)this.view.WindowSize.Width) * 100);
                Utility.Configuration.Settings.Save();
            }
        }

        /// <summary>
        /// Invoked when an error has been thrown in a view.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnError(object sender, ErrorArgs args)
        {
            ShowError(args.Error);
        }
    }
}
