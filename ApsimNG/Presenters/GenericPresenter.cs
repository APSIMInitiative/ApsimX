using Models.Core;
using UserInterface.Views;
using APSIM.Documentation;
using APSIM.Documentation.Models;

namespace UserInterface.Presenters
{
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

            this.genericView.Text = WebDocs.ConvertToMarkdown(AutoDocumentation.Document(this.model), "");
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
        }
    }
}
