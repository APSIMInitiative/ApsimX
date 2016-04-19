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
    using Models.Core;
    using Views;
    using System.Reflection;
    using System.Drawing;
    using Models;
    using Forms;

    /// <summary>
    /// This presenter class provides the functionality behind a TabbedExplorerView 
    /// which is a tab control where each tabs represent an .apsimx file. Each tab
    /// then has an ExplorerPresenter and ExplorerView created when the tab is
    /// created.
    /// </summary>
    public class TabbedExplorerPresenter
    {
        /// <summary>A private reference to the view this presenter will talk to.</summary>
        private ITabbedExplorerView view;

        /// <summary>The path last used to open the examples</summary>
        private string lastExamplesPath;

        /// <summary>A list of all tabs.</summary>
        private List<ExplorerPresenter> presenters;

        /// <summary>Attach this presenter with a view.</summary>
        /// <param name="view">The view to attach</param>
        public void Attach(object view)
        {
            this.view = view as ITabbedExplorerView;
            this.presenters = new List<ExplorerPresenter>();
            PopulateView();
        }

        /// <summary>Detach this presenter from the view.</summary>
        /// <param name="view">The view used for this object</param>
        public void Detach(object view)
        {
        }

        /// <summary>Allow the form to close?</summary>
        /// <returns>True if can be closed</returns>
        public bool AllowClose()
        {
            bool ok = true;

            foreach (ExplorerPresenter presenter in this.presenters)
            {
                ok = presenter.SaveIfChanged() && ok;
            }

            return ok;
        }
        
        /// <summary>Open an .apsimx file into the current tab.</summary>
        /// <param name="fileName">The file to open</param>
        public void OpenApsimXFileInTab(string fileName)
        {
            if (fileName != null)
            {
                ExplorerView explorerView = new ExplorerView();
                ExplorerPresenter presenter = new ExplorerPresenter();
                this.presenters.Add(presenter);
                try
                {
                    view.SetWaitCursor(true);
                    try
                    {
                        Simulations simulations = Simulations.Read(fileName);
                        presenter.Attach(simulations, explorerView, null);
                        view.AddTab(fileName, Properties.Resources.apsim_logo32, explorerView, OnTabClosing);

                        // restore the simulation tree width on the form
                        if (simulations.ExplorerWidth == 0)
                            presenter.TreeWidth = 250;
                        else
                            presenter.TreeWidth = simulations.ExplorerWidth;

                        Utility.Configuration.Settings.AddMruFile(fileName);
                        List<string> validMrus = new List<string>();                           // make sure recently used files still exist before displaying them
                        foreach (string s in Utility.Configuration.Settings.MruList)
                            if (File.Exists(s))
                                validMrus.Add(s);
                        Utility.Configuration.Settings.MruList = validMrus;
                    }
                    finally
                    {
                        view.SetWaitCursor(false);
                    }
                }
                catch (Exception err)
                {
                    this.view.ShowError(err.Message);
                }
            }
        }

        /// <summary>When the view wants to populate it's start page, it will invoke this event handler.</summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void PopulateView()
        {
            view.ListAndButtons.AddButton("Open APSIM File",
                                          new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("UserInterface.Resources.Toolboxes.OpenFile.png")),
                                          OnOpenApsimXFile);

            view.ListAndButtons.AddButton("Open an example",
                                          new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("UserInterface.Resources.Toolboxes.OpenExample.png")),
                                          OnExample);

            view.ListAndButtons.AddButton("Open standard toolbox",
                                          new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("UserInterface.Resources.Toolboxes.Toolbox.png")),
                                          OnStandardToolboxClick);

            view.ListAndButtons.AddButton("Open management toolbox",
                                          new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("UserInterface.Resources.Toolboxes.Toolbox.png")),
                                          OnManagementToolboxClick);

            view.ListAndButtons.AddButton("Open training toolbox",
                                          new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("UserInterface.Resources.Toolboxes.Toolbox.png")),
                                          OnTrainingToolboxClick);

            view.ListAndButtons.AddButton("Import old .apsim file",
                                          new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("UserInterface.Resources.Toolboxes.Import.png")),
                                          OnImport);

            view.ListAndButtons.AddButton("Upgrade",
                              new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("UserInterface.Resources.MenuImages.Upgrade.png")),
                              OnUpgrade);

            // cleanup the list when this tab is first shown
            Utility.Configuration.Settings.CleanMruList();

            // Populate the view's listview.
            view.ListAndButtons.List.Values = Utility.Configuration.Settings.MruList.ToArray();

            view.ListAndButtons.List.DoubleClicked += OnFileDoubleClicked;
        }

        /// <summary>Open an .apsimx file into the current tab.</summary>
        /// <param name="name">Name of the simulation</param>
        /// <param name="contents">The xml content</param>
        private void OpenApsimXFromMemoryInTab(string name, string contents)
        {
            ExplorerView explorerView = new ExplorerView();
            ExplorerPresenter presenter = new ExplorerPresenter();
            presenters.Add(presenter);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(contents);
            Simulations simulations = Simulations.Read(doc.DocumentElement);
            presenter.Attach(simulations, explorerView, null);

            this.view.AddTab(name, Properties.Resources.apsim_logo32, explorerView, OnTabClosing);
        }

        /// <summary>
        /// Event handler invoked when user clicks on 'Open ApsimX file'
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event parameters</param>
        private void OnOpenApsimXFile(object sender, EventArgs e)
        {
            string fileName = this.view.AskUserForFileName(string.Empty, "*.apsimx|*.apsimx");
            if (fileName != null)
            {
                this.OpenApsimXFileInTab(fileName);
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
            string fileName = view.ListAndButtons.List.SelectedValue;
            if (fileName != null)
            {
                this.OpenApsimXFileInTab(fileName);
                Utility.Configuration.Settings.PreviousFolder = Path.GetDirectoryName(fileName);
            }
        }

        /// <summary>
        /// Current tab is closing - remove presenter from our presenters list
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnTabClosing(object sender, TabEventArgs e)
        {
            this.presenters[e.index-1].SaveIfChanged();
            this.presenters.RemoveAt(e.index - 1);
        }

        /// <summary>Event handler invoked when user clicks on 'Standard toolbox'</summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnStandardToolboxClick(object sender, EventArgs e)
        {
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("UserInterface.Resources.Toolboxes.StandardToolbox.apsimx");
            StreamReader streamReader = new StreamReader(s);
            this.OpenApsimXFromMemoryInTab("Standard toolbox", streamReader.ReadToEnd());
        }

        /// <summary>
        /// Event handler invoked when user clicks on 'Management toolbox'
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnManagementToolboxClick(object sender, EventArgs e)
        {
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("UserInterface.Resources.Toolboxes.ManagementToolbox.apsimx");
            StreamReader streamReader = new StreamReader(s);
            this.OpenApsimXFromMemoryInTab("Standard toolbox", streamReader.ReadToEnd());
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
                Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("UserInterface.Resources.Toolboxes.TrainingToolbox.apsimx");
                StreamReader streamReader = new StreamReader(s);
                this.OpenApsimXFromMemoryInTab("Training toolbox", streamReader.ReadToEnd());
            }
            catch (Exception err)
            {
                string message = err.Message;
                if (err.InnerException != null)
                    message += "\r\n" + err.InnerException.Message;

                this.view.ShowError(message);
            }
        }

        /// <summary>
        /// Event handler invoked when user clicks on 'Import'
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnImport(object sender, EventArgs e)
        {
            string fileName = this.view.AskUserForFileName(string.Empty, "*.apsim|*.apsim");

            APSIMImporter importer = new APSIMImporter();
            try
            {
                view.SetWaitCursor(true);
                try
                {
                    importer.ProcessFile(fileName);

                    string newFileName = Path.ChangeExtension(fileName, ".apsimx");
                    this.OpenApsimXFileInTab(newFileName);
                }
                finally
                {
                    view.SetWaitCursor(false);
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
            string fileName = this.view.AskUserForFileName(initialPath, "*.apsimx|*.apsimx");

            if (fileName != null)
            {
                this.lastExamplesPath = Path.GetDirectoryName(fileName);

                // ensure that they are saved in another file before running by opening them in memory
                StreamReader reader = new StreamReader(fileName);
                this.OpenApsimXFromMemoryInTab(string.Empty, reader.ReadToEnd());
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
                view.ShowError("You are on a custom build. You cannot upgrade.");
            else
            {
                if (AllowClose())
                {
                    UpgradeForm form = new UpgradeForm(view);
                    form.Show();
                }
            }

        }
    }
}
