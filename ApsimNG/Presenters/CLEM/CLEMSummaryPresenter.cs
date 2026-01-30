using System;
using System.IO;
using System.Net.NetworkInformation;
using APSIM.Shared.Utilities;
using UserInterface.Interfaces;
using Models.CLEM;
using Models.CLEM.Interfaces;
using Models.Core;
using UserInterface.Views;
using Models.CLEM.DescriptiveSummary;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Presenter to provide HTML description summary for CLEM models
    /// </summary>
    public class CLEMSummaryPresenter : IPresenter, IRefreshPresenter
    {
        private Model model;
        private IMarkdownView genericView;
        private ExplorerPresenter explorer;
        private string htmlFilePath = "";
        private string targetFilePath = "";
        private bool firstentry = true;
        readonly DescriptiveSummaryGenerator summaryGenerator = new DescriptiveSummaryGenerator(DescriptiveSummaryFormat.HTML, false);

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
            
            if (model is not ZoneCLEM zc)
            {
                zc = (model as IModel).Node.Find<ZoneCLEM>();
            }

            summaryGenerator.OutputFormat = zc.DescriptiveSummaryFormatStyle;
            summaryGenerator.IsDarkMode = Utility.Configuration.Settings.DarkTheme;

            string extension = DescriptiveSummaryGenerator.FileExtensionToUse(zc.DescriptiveSummaryFormatStyle);

            htmlFilePath = $"CurrentDescriptiveSummary{extension}";
            targetFilePath = $"CurrentDescriptiveSummary{extension}";

            if (typeof(ISpecificOutputFilename).IsAssignableFrom(model.GetType()))
                targetFilePath = (model as ISpecificOutputFilename).HtmlOutputFilename;

            htmlFilePath = Path.Combine(Path.GetDirectoryName(explorer.ApsimXFile.FileName), htmlFilePath);
            targetFilePath = Path.Combine(Path.GetDirectoryName(explorer.ApsimXFile.FileName), targetFilePath);

            summaryGenerator.GenerateSummaryForComponentAndChildren(model as IModel, htmlFilePath);
            //summaryGenerator.GenerateSummaryForComponentAndChildren(model as IModel, Path.Combine(Path.GetDirectoryName(explorer.ApsimXFile.FileName), "CurrentDescriptiveSummary.html"));
            //File.WriteAllText(htmlFilePath, CLEMModel.CreateDescriptiveSummaryHTML(this.model, this.model.Node, Utility.Configuration.Settings.DarkTheme, markdown2Html: Utility.MarkdownConverter.ToHtml ));
        }

        public void Refresh()
        {
            this.genericView.Text = CreateMarkdown(this.model);
            //save summary to disk
            //File.WriteAllText(Path.Combine(Path.GetDirectoryName(explorer.ApsimXFile.FileName), "CurrentDescriptiveSummary.html"), CLEMModel.CreateDescriptiveSummaryHTML(this.model, this.model.Node, Utility.Configuration.Settings.DarkTheme, markdown2Html: Utility.MarkdownConverter.ToHtml));
            if (!firstentry)
            {
                summaryGenerator.GenerateSummaryForComponentAndChildren(model as IModel, htmlFilePath);
                firstentry = false;
            }
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
                    modelHelpURL = helpAtt.ToString();

                // does offline help exist
                var directory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string offlinePath = Path.Combine(directory, "CLEM/Help");
                if (File.Exists(Path.Combine(offlinePath, "Default.htm")))
                    helpURL = offlinePath.Replace(@"\", "/") + "/" + modelHelpURL.TrimStart('/');

                // is this application online for online help
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    // set to web address
                    // not currently available during development until web help is launched
                    helpURL = "https://www.apsim.info/clem/" + modelHelpURL.TrimStart('/');
                }
                if (helpURL == "")
                    helpURL = "https://www.apsim.info";
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            string markdownString = "";
            if (File.Exists(targetFilePath))
                markdownString = $"[View descriptive summary of current settings in browser](<{targetFilePath.Replace("\\", "/")}> \"descriptive summary\")  {Environment.NewLine}  {Environment.NewLine}";
            markdownString += $"View reference details for this component [{modelToSummarise.GetType().Name}](<{helpURL}> \"{modelToSummarise.GetType().Name} help\")  {Environment.NewLine}";
            return markdownString;
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
            (genericView as MarkdownView).Dispose();
        }
    }
}

