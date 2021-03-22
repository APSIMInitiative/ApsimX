
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.NetworkInformation;
    using System.Reflection;
    using System.Text;
    using APSIM.Shared.Utilities;
    using global::UserInterface.Interfaces;
    using Models.CLEM;
    using Models.CLEM.Interfaces;
    using Models.Core;
    using Views;

    /// <summary>
    /// Presenter to provide HTML description summary for CLEM models
    /// </summary>
    public class CLEMSummaryPresenter : IPresenter, IRefreshPresenter
    {
        /// <summary>
        /// The model
        /// </summary>
        private Model model;

        /// <summary>
        /// The view to use
        /// </summary>
        private IMarkdownView genericView;

        private ExplorerPresenter explorer;
        private string htmlFilePath = "";
        private string targetFilePath = "";

        /// <summary>
        /// Attach the view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The explorer</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as Model;
            this.genericView = view as IMarkdownView; 
            explorer = explorerPresenter;

            // save summary to disk when component is first pressed regardless of user selecting summary tab as now goes to html in browser

            htmlFilePath = "CurrentDescriptiveSummary.html";
            targetFilePath = "CurrentDescriptiveSummary.html";

            if (typeof(ISpecificOutputFilename).IsAssignableFrom(model.GetType()))
            {
                targetFilePath = (model as ISpecificOutputFilename).HtmlOutputFilename;
            }

            htmlFilePath = Path.Combine(Path.GetDirectoryName(explorer.ApsimXFile.FileName), htmlFilePath);
            targetFilePath = Path.Combine(Path.GetDirectoryName(explorer.ApsimXFile.FileName), targetFilePath);
            System.IO.File.WriteAllText(htmlFilePath, CLEMModel.CreateDescriptiveSummaryHTML(this.model, Utility.Configuration.Settings.DarkTheme));
        }

        public void Refresh()
        {
            this.genericView.Text = CreateMarkdown(this.model);

            // save summary to disk
            System.IO.File.WriteAllText(Path.Combine(Path.GetDirectoryName(explorer.ApsimXFile.FileName), "CurrentDescriptiveSummary.html"), CLEMModel.CreateDescriptiveSummaryHTML(this.model, Utility.Configuration.Settings.DarkTheme));
        }

        public string CreateMarkdown(Model modelToSummarise)
        {
            // get help uri
            string helpURL = "";
            try
            {
                HelpUriAttribute helpAtt = ReflectionUtilities.GetAttribute(model.GetType(), typeof(HelpUriAttribute), false) as HelpUriAttribute;
                string modelHelpURL = "";
                if (helpAtt != null)
                {
                    modelHelpURL = helpAtt.ToString();
                }
                // does offline help exist
                var directory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string offlinePath = Path.Combine(directory, "CLEM/Help");
                if (File.Exists(Path.Combine(offlinePath, "Default.htm")))
                {
                    helpURL = offlinePath.Replace(@"\", "/") + "/" + modelHelpURL.TrimStart('/');
                }
                // is this application online for online help
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    // set to web address
                    // not currently available during development until web help is launched
                    helpURL = "https://www.apsim.info/clem/" + modelHelpURL.TrimStart('/');
                }
                if (helpURL == "")
                {
                    helpURL = "https://www.apsim.info";
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            string markdownString = "";
            if (File.Exists(targetFilePath))
            {
                markdownString = $"[View descriptive summary of current settings in browser](<{targetFilePath.Replace("\\", "/")}> \"descriptive summary\")  {Environment.NewLine}  {Environment.NewLine}";
            }
            markdownString += $"View reference details for this component [{modelToSummarise.GetType().Name}](<{helpURL}> \"{modelToSummarise.GetType().Name} help\")  {Environment.NewLine}";
            return markdownString;
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
        }
    }
}

