using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UserInterface.Views;
using System.IO;
using System.Xml;
using Models.Core;
using Importer;
using System.Windows.Forms;

namespace UserInterface.Presenters
{
    /// <summary>
    /// This presenter class provides the funcionality behind a TabbedExplorerView 
    /// which is a tab control where each tabs represent an .apsimx file. Each tab
    /// then has an ExplorerPresenter and ExplorerView created when the tab is
    /// created.
    /// </summary>
    public class TabbedExplorerPresenter
    {
        /// <summary>
        /// A private reference to the view this presenter will talk to.
        /// </summary>
        private ITabbedExplorerView View;



        /// <summary>
        /// A list of ExplorerPresenters - one for each open tab.
        /// </summary>
        public List<ExplorerPresenter> Presenters { get; private set; }

        /// <summary>
        /// Attach this presenter with a view.
        /// </summary>
        public void Attach(object view)
        {
            this.View = view as ITabbedExplorerView;
            this.View.PopulateStartPage += OnPopulateStartPage;
            Presenters = new List<ExplorerPresenter>();
        }

        /// <summary>
        /// Close the application.
        /// </summary>
        public void Close()
        {
            UserControl view = View as UserControl;
            Form mainForm = view.ParentForm;
            mainForm.Close();
        }

        /// <summary>
        /// Allow the for to close?
        /// </summary>
        public bool AllowClose()
        {
            bool ok = true;

            foreach (ExplorerPresenter presenter in Presenters)
            {
                ok = presenter.SaveIfChanged() && ok;
            }

            return ok;
        }
        
        /// <summary>
        /// Open an .apsimx file into the current tab.
        /// </summary>
        public void OpenApsimXFileInTab(string fileName)
        {
            if (fileName != null)
            {
                ExplorerView ExplorerView = new ExplorerView();
                ExplorerPresenter Presenter = new ExplorerPresenter();
                Presenters.Add(Presenter);
                try
                {
                    Simulations simulations = Simulations.Read(fileName);
                    Presenter.Attach(simulations, ExplorerView, null);
                    View.AddTab(fileName, Properties.Resources.apsim_logo32, ExplorerView, true);
                    // restore the simulation tree width on the form
                    if (simulations.ExplorerWidth == 0)
                        Presenter.TreeWidth = 250;
                    else
                        Presenter.TreeWidth = Math.Min(simulations.ExplorerWidth, View.TabWidth - 20); // ?
                }
                catch (Exception err)
                {
                    this.View.ShowError(err.Message);
                }
            }
        }

        /// <summary>
        /// Open an .apsimx file into the current tab.
        /// </summary>
        public void OpenApsimXFromMemoryInTab(string name, string contents)
        {
            ExplorerView ExplorerView = new ExplorerView();
            ExplorerPresenter Presenter = new ExplorerPresenter();
            Presenters.Add(Presenter);

            XmlDocument Doc = new XmlDocument();
            Doc.LoadXml(contents);
            Simulations simulations = Simulations.Read(Doc.DocumentElement);
            Presenter.Attach(simulations, ExplorerView, null);

            View.AddTab(name, Properties.Resources.apsim_logo32, ExplorerView, true);
        }

        /// <summary>
        /// When the view wants to populate it's start page, it will invoke this
        /// event handler.
        /// </summary>
        private void OnPopulateStartPage(object sender, PopulateStartPageArgs e)
        {
            e.Descriptions.Add(new PopulateStartPageArgs.Description()
            {
                Name = "Open ApsimX File",
                ResourceNameForImage = "apsim_logo32",
                OnClick = OnOpenApsimXFile
            });

            e.Descriptions.Add(new PopulateStartPageArgs.Description()
            {
                Name = "Standard toolbox",
                ResourceNameForImage = "StandardToolboxIcon",
                OnClick = OnStandardToolboxClick
            });

            e.Descriptions.Add(new PopulateStartPageArgs.Description()
            {
                Name = "Import old .apsim file",
                ResourceNameForImage = "import2.png",
                OnClick = OnImport
            });

        }

        /// <summary>
        /// Event handler invoked when user clicks on 'Open ApsimX file'
        /// </summary>
        private void OnOpenApsimXFile(object sender, EventArgs e)
        {
            string FileName = View.AskUserForFileName("*.apsimx|*.apsimx");
            OpenApsimXFileInTab(FileName);
        }

        /// <summary>
        /// Event handler invoked when user clicks on 'Standard toolbox'
        /// </summary>
        public void OnStandardToolboxClick(object sender, EventArgs e)
        {
            byte[] b = Properties.Resources.ResourceManager.GetObject("StandardToolBox") as byte[];
            StreamReader SR = new StreamReader(new MemoryStream(b));
            OpenApsimXFromMemoryInTab("Standard toolbox", SR.ReadToEnd());
        }

        /// <summary>
        /// Event handler invoked when user clicks on 'Import'
        /// </summary>
        private void OnImport(object sender, EventArgs e)
        {
            string FileName = View.AskUserForFileName("*.apsim|*.apsim");

            APSIMImporter importer = new APSIMImporter();
            try
            {
                importer.ProcessFile(FileName);

                string newFileName = Path.ChangeExtension(FileName, ".apsimx");
                OpenApsimXFileInTab(newFileName);
            }
            catch (Exception exp)
            {
                throw new Exception("Failed import: " + exp.Message);
            }
        }

    }
}
