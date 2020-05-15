namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Views;

    /// <summary>
    /// Presenter of unspecified type
    /// </summary>
    public class GenericPresenter : IPresenter
    {
        /// <summary>
        /// The model
        /// </summary>
        private Model model;

        /// <summary>
        /// The view to use
        /// </summary>
        private IHTMLView genericView;

        /// <summary>
        /// The explorer
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attach the view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The explorer</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as Model;
            this.genericView = view as IHTMLView;
            this.explorerPresenter = explorerPresenter;

            // Just how much documentation do we want to generate?
            // For now, let's just use the component name and a basic description.

            // It's slightly simpler to generate Markdown for this, but it
            // would be pretty easy to build this directly as HTML
            List<AutoDocumentation.ITag> tags = new List<AutoDocumentation.ITag>();
            AutoDocumentation.DocumentModel(this.model, tags, 1, 0, false, force: true);

            StringBuilder contents = new StringBuilder();
            foreach (AutoDocumentation.ITag tag in tags)
            {
                if (tag is AutoDocumentation.Heading)
                {
                    contents.Append("\r\n### ");
                    contents.Append((tag as AutoDocumentation.Heading).text);
                }
                else if (tag is AutoDocumentation.Paragraph)
                {
                    contents.Append("\r\n");
                    contents.Append((tag as AutoDocumentation.Paragraph).text);
                    FindImagesInParagraph(tag as AutoDocumentation.Paragraph);
                }
            }

            MarkdownDeep.Markdown markDown = new MarkdownDeep.Markdown();
            markDown.ExtraMode = true;
            markDown.DocumentLocation = Path.GetTempPath();
            markDown.UrlBaseLocation = markDown.DocumentLocation;

            string html = markDown.Transform(contents.ToString());
            this.genericView.SetContents(html, false, false);
        }

        /// <summary>
        /// For each image markdown tag in a paragraph, locate image from resource and save to temp folder.
        /// </summary>
        /// <param name="paragraph">The paragraph to scan.</param>
        private void FindImagesInParagraph(AutoDocumentation.Paragraph paragraph)
        {
            var regEx = new Regex(@"!\[(.+)\]\((.+)\)");
            foreach (Match match in regEx.Matches(paragraph.text))
            {
                var fileName = match.Groups[2].ToString();
                var tempFileName = Path.Combine(Path.GetTempPath(), fileName);
                if (File.Exists(tempFileName))
                {
                    var timeSinceLastAccess = DateTime.Now - File.GetLastAccessTime(tempFileName);
                    if (timeSinceLastAccess.Hours > 1)
                    {
                        using (FileStream file = new FileStream(tempFileName, FileMode.Create, FileAccess.Write))
                        {
                            var imageStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"ApsimNG.Resources.{fileName}");
                            imageStream?.CopyTo(file);
                            file.Close();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
        }
    }
}
