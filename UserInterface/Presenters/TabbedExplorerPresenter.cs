using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UserInterface.Views;
using System.IO;
using System.Xml;
using Models.Core;

namespace UserInterface.Presenters
{
    class TabbedExplorerPresenter
    {
        private ITabbedExplorerView View;
        private List<ExplorerPresenter> Presenters = new List<ExplorerPresenter>();

        public void Attach(object View)
        {
            this.View = View as ITabbedExplorerView;
            this.View.PopulateStartPage += OnPopulateStartPage;
        }

        void OnPopulateStartPage(object sender, PopulateStartPageArgs e)
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
        }

        private void OnOpenApsimXFile(object Sender, EventArgs e)
        {
            string FileName = View.AskUserForFileName();
            OpenApsimXFileInTab(FileName);
        }
        
        private void OnStandardToolboxClick(object Sender, EventArgs e)
        {
            byte[] b = Properties.Resources.ResourceManager.GetObject("StandardToolBox") as byte[];
            StreamReader SR = new StreamReader(new MemoryStream(b));
            OpenApsimXFromMemoryInTab("Standard toolbox", SR.ReadToEnd());
        }

        /// <summary>
        /// Allow the for to close?
        /// </summary>
        public bool AllowClose()
        {
            bool ok = true;
            foreach (ExplorerPresenter presenter in Presenters)
                ok = presenter.Save() && ok;
            return ok;
        }
        
        /// <summary>
        /// Open an .apsimx file into the current tab.
        /// </summary>
        private void OpenApsimXFileInTab(string FileName)
        {
            if (FileName != null)
            {
                ExplorerView ExplorerView = new ExplorerView();
                ExplorerPresenter Presenter = new ExplorerPresenter();
                Presenters.Add(Presenter);

                try
                {
                    Simulations simulations = Simulations.Read(FileName);
                    Presenter.Attach(simulations, ExplorerView, null);
                    View.AddTab(FileName, Properties.Resources.apsim_logo32, ExplorerView, true);
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
        private void OpenApsimXFromMemoryInTab(string Name, string Contents)
        {
            ExplorerView ExplorerView = new ExplorerView();
            ExplorerPresenter Presenter = new ExplorerPresenter();

            XmlDocument Doc = new XmlDocument();
            Doc.LoadXml(Contents);
            Presenter.Attach(Utility.Xml.Deserialise(Doc.DocumentElement), ExplorerView, null);

            View.AddTab(Name, Properties.Resources.apsim_logo32, ExplorerView, true);
        }

    }
}
