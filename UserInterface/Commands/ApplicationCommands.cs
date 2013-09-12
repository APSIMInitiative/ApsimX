using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UserInterface.Presenters;
using UserInterface.Views;
using Model.Core;
using System.Xml;

namespace UserInterface.Commands
{
    public class ApplicationCommands
    {
        private MainForm MainForm;

        public ApplicationCommands(MainForm MainForm) { this.MainForm = MainForm; }

        /// <summary>
        /// Add a special start tab to the application.
        /// </summary>
        public void AddStartTab()
        {
            MainForm.AddTab("", null, new StartPageView(this), false);
        }

        /// <summary>
        /// Change the text of the tab.
        /// </summary>
        void ChangeCurrentTabText(string NewTabName)
        {
            MainForm.CurrentTabText = NewTabName;
        }
    

        /// <summary>
        /// Open an .apsimx file into the current tab.
        /// </summary>
        public void OpenApsimXFileInTab(string FileName)
        {
            ExplorerView View = new ExplorerView();
            ExplorerPresenter Presenter = new ExplorerPresenter();
            Simulations Simulations = Utility.Xml.Deserialise(FileName) as Simulations;
            Simulations.FileName = FileName;

            Presenter.Attach(Simulations, View, null);

            MainForm.ReplaceCurrentTab(FileName, Properties.Resources.apsim_logo32, View);

        }

        /// <summary>
        /// Open an .apsimx file into the current tab.
        /// </summary>
        public void OpenApsimXFromMemoryInTab(string Name, string Contents)
        {
            ExplorerView View = new ExplorerView();
            ExplorerPresenter Presenter = new ExplorerPresenter();

            XmlDocument Doc = new XmlDocument();
            Doc.LoadXml(Contents);
            Presenter.Attach(Utility.Xml.Deserialise(Doc.DocumentElement), View, null);

            MainForm.ReplaceCurrentTab(Name, Properties.Resources.apsim_logo32, View);
        }
    }
}
