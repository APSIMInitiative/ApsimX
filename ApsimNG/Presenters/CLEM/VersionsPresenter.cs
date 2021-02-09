using APSIM.Shared.Utilities;
using ApsimNG.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Interfaces;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    public class VersionsPresenter : IPresenter, IRefreshPresenter
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
        /// Attach the view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The explorer</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as Model;
            this.genericView = view as IMarkdownView;
        }

        public void Refresh()
        {
            this.genericView.Text = CreateMarkdown();
        }

        private string CreateMarkdown()
        {
            string markdownString = "";

            foreach (VersionAttribute item in ReflectionUtilities.GetAttributes(model.GetType(), typeof(VersionAttribute), false))
            {
                markdownString += $"### v {item.ToString()}";
                markdownString += $"  {Environment.NewLine} {(item.Comments().Length == 0 ? ((item.ToString() == "1.0.1") ? "Initial release of this component" : "No details provided") : item.Comments().Replace("\r\n", "  \r\n  \r\n "))}  {Environment.NewLine}  {Environment.NewLine}";
            }
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
