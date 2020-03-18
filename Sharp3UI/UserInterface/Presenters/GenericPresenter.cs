namespace UserInterface.Presenters
{
    using System.Collections.Generic;
    using System.Text;
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
                }
            }

            MarkdownDeep.Markdown markDown = new MarkdownDeep.Markdown();
            markDown.ExtraMode = true;

            string html = markDown.Transform(contents.ToString());
            this.genericView.SetContents(html, false, false);
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
        }
    }
}
