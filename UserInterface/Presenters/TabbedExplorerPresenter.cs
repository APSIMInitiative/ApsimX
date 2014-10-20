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
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;
    using System.Xml;
    using Importer;
    using Models.Core;
    using Views;
    
    /// <summary>
    /// This presenter class provides the functionality behind a TabbedExplorerView 
    /// which is a tab control where each tabs represent an .apsimx file. Each tab
    /// then has an ExplorerPresenter and ExplorerView created when the tab is
    /// created.
    /// </summary>
    public class TabbedExplorerPresenter
    {
        /// <summary>
        /// A private reference to the view this presenter will talk to.
        /// </summary>
        private ITabbedExplorerView view;

        /// <summary>
        /// The path last used to open the examples
        /// </summary>
        private string lastExamplesPath;

        /// <summary>
        /// Gets or sets the reference to the main form config
        /// </summary>
        public Utility.Configuration Config { get; set; }

        /// <summary>
        /// Gets a list of ExplorerPresenters - one for each open tab.
        /// </summary>
        public List<ExplorerPresenter> Presenters { get; private set; }

        /// <summary>
        /// Attach this presenter with a view.
        /// </summary>
        /// <param name="view">The view to attach</param>
        public void Attach(object view)
        {
            this.view = view as ITabbedExplorerView;
            this.view.PopulateStartPage += this.OnPopulateStartPage;
            this.view.MruFileClick += this.OnMruApsimOpenFile;
            this.view.ReloadMruView += this.OnReloadMruView;
            this.view.TabClosing += this.OnTabClosing;
            this.Presenters = new List<ExplorerPresenter>();
        }

        /// <summary>
        /// Detach this presenter from the view.
        /// </summary>
        /// <param name="view">The view used for this object</param>
        public void Detach(object view)
        {
            this.view.PopulateStartPage -= this.OnPopulateStartPage;
            this.view.MruFileClick -= this.OnMruApsimOpenFile;
            this.view.ReloadMruView -= this.OnReloadMruView;
            this.view.TabClosing -= this.OnTabClosing;
        }

        /// <summary>
        /// Close the application.
        /// </summary>
        /// <param name="askToSave">Flag to turn on the request to save</param>
        public void Close(bool askToSave = true)
        {
            UserControl view = this.view as UserControl;
            Form mainForm = view.ParentForm;
            if (!askToSave)
                mainForm.DialogResult = DialogResult.Cancel;
            mainForm.Close();
        }

        /// <summary>
        /// Allow the form to close?
        /// </summary>
        /// <returns>True if can be closed</returns>
        public bool AllowClose()
        {
            bool ok = true;

            foreach (ExplorerPresenter presenter in this.Presenters)
            {
                ok = presenter.SaveIfChanged() && ok;
            }

            return ok;
        }
        
        /// <summary>
        /// Open an .apsimx file into the current tab.
        /// </summary>
        /// <param name="fileName">The file to open</param>
        public void OpenApsimXFileInTab(string fileName)
        {
            if (fileName != null)
            {
                ExplorerView explorerView = new ExplorerView();
                ExplorerPresenter presenter = new ExplorerPresenter();
                this.Presenters.Add(presenter);
                try
                {
                    Cursor.Current = Cursors.WaitCursor;

                    presenter.Config = this.Config;  // give it access to the configuration settings for MRU's
                    Simulations simulations = Simulations.Read(fileName);
                    presenter.Attach(simulations, explorerView, null);
                    this.view.AddTab(fileName, Properties.Resources.apsim_logo32, explorerView, true);

                    // restore the simulation tree width on the form
                    if (simulations.ExplorerWidth == 0)
                        presenter.TreeWidth = 250;
                    else
                        presenter.TreeWidth = Math.Min(simulations.ExplorerWidth, this.view.TabWidth - 20); // ?
                    this.Config.Settings.AddMruFile(fileName);
                    this.Config.Save();
                    List<string> validMrus = new List<string>();                           // make sure recently used files still exist before displaying them
                    foreach (string s in this.Config.Settings.MruList)
                        if (File.Exists(s))
                            validMrus.Add(s);
                    this.Config.Settings.MruList = validMrus;
                    this.view.FillMruList(validMrus);

                    Cursor.Current = Cursors.Default;
                }
                catch (Exception err)
                {
                    this.view.ShowError(err.Message);
                }
            }
        }

        /// <summary>
        /// Open an .apsimx file into the current tab.
        /// </summary>
        /// <param name="name">Name of the simulation</param>
        /// <param name="contents">The xml content</param>
        public void OpenApsimXFromMemoryInTab(string name, string contents)
        {
            ExplorerView explorerView = new ExplorerView();
            ExplorerPresenter presenter = new ExplorerPresenter();
            this.Presenters.Add(presenter);
            presenter.Config = this.Config;  // give it access to the configuration settings for MRU's
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(contents);
            Simulations simulations = Simulations.Read(doc.DocumentElement);
            presenter.Attach(simulations, explorerView, null);

            this.view.AddTab(name, Properties.Resources.apsim_logo32, explorerView, true);
        }

        /// <summary>
        /// When the view wants to populate it's start page, it will invoke this
        /// event handler.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnPopulateStartPage(object sender, PopulateStartPageArgs e)
        {
            e.Descriptions.Add(new PopulateStartPageArgs.Description()
            {
                Name = "Open APSIM File",
                ResourceNameForImage = "apsim_logo32",
                OnClick = this.OnOpenApsimXFile
            });

            e.Descriptions.Add(new PopulateStartPageArgs.Description()
            {
                Name = "Standard toolbox",
                ResourceNameForImage = "StandardToolboxIcon",
                OnClick = this.OnStandardToolboxClick
            });

            e.Descriptions.Add(new PopulateStartPageArgs.Description()
            {
                Name = "Import old .apsim file",
                ResourceNameForImage = "apsim7_logo32",
                OnClick = this.OnImport
            });

            e.Descriptions.Add(new PopulateStartPageArgs.Description()
            {
                Name = "Open an example",
                ResourceNameForImage = "gfolder_32",
                OnClick = this.OnExample
            });

            this.Config.Settings.CleanMruList();                     // cleanup the list when this tab is first shown
            this.view.FillMruList(this.Config.Settings.MruList);
        }

        /// <summary>
        /// Handler to reload the most recently used files list
        /// </summary>
        /// <param name="sender">Sender Object</param>
        /// <param name="e">Event arguments</param>
        private void OnReloadMruView(object sender, EventArgs e)
        {
            this.view.FillMruList(this.Config.Settings.MruList);
        }

        /// <summary>
        /// Event handler invoked when user clicks on 'Open ApsimX file'
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event parameters</param>
        private void OnOpenApsimXFile(object sender, EventArgs e)
        {
            string fileName = this.view.AskUserForFileName(string.Empty, "*.apsimx|*.apsimx");
            this.OpenApsimXFileInTab(fileName);
        }

        /// <summary>
        /// Open a recently used file
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnMruApsimOpenFile(object sender, EventArgs e)
        {
            string fileName = this.view.SelectedMruFileName();
            this.OpenApsimXFileInTab(fileName);
        }

        /// <summary>
        /// Current tab is closing - remove presenter from our presenters list
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnTabClosing(object sender, EventArgs e)
        {
            this.Presenters[this.view.CurrentTabIndex].SaveIfChanged();
            this.Presenters.RemoveAt(this.view.CurrentTabIndex);
        }

        /// <summary>
        /// Event handler invoked when user clicks on 'Standard toolbox'
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        public void OnStandardToolboxClick(object sender, EventArgs e)
        {
            byte[] b = Properties.Resources.ResourceManager.GetObject("StandardToolBox") as byte[];
            StreamReader streamReader = new StreamReader(new MemoryStream(b));
            this.OpenApsimXFromMemoryInTab("Standard toolbox", streamReader.ReadToEnd());
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
                Cursor.Current = Cursors.WaitCursor;
                importer.ProcessFile(fileName);

                string newFileName = Path.ChangeExtension(fileName, ".apsimx");
                this.OpenApsimXFileInTab(newFileName);
                Cursor.Current = Cursors.Default;
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
                initialPath = Path.GetFullPath(Path.Combine(initialPath, @"../Examples"));
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
    }
}
