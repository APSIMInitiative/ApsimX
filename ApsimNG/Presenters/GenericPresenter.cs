namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using Models.Core;
    using Views;
    using Interfaces;
    using Markdig;
    using Markdig.Renderers;
    using Markdig.Syntax;
    using Markdig.Parsers;
    using Utility;

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
        private IMarkdownView genericView;

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
            this.genericView = view as IMarkdownView;
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
                if (tag is AutoDocumentation.Heading heading)
                {
                    contents.AppendLine();
                    contents.Append($"### {heading.text}");
                }
                else if (tag is AutoDocumentation.Paragraph paragraph)
                {
                    contents.AppendLine();
                    contents.Append(paragraph.text);
                }
            }

            this.genericView.Text = contents.ToString();
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
        }
    }
}
