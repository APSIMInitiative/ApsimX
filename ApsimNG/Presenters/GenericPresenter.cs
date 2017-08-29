using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.Core;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    class GenericPresenter : IPresenter
    {
        private Model model;
        private IHTMLView genericView;
        private ExplorerPresenter ExplorerPresenter;

        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as Model;
            this.genericView = view as IHTMLView;
            this.ExplorerPresenter = explorerPresenter;


            // Just how much documentation do we want to generate?
            // For now, let's just use the component name and a basic description.

            // It's slightly simpler to generate Markdown for this, but it
            // would be pretty easy to build this directly as HTML
            List<AutoDocumentation.ITag> tags = new List<AutoDocumentation.ITag>();
            AutoDocumentation.DocumentModel(this.model, tags, 1, 0);

            StringBuilder contents = new StringBuilder("## " + this.model.Name + "\r\n");
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
            genericView.SetContents(html, false, false);
        }

        public void Detach()
        {
        }
    }
}
