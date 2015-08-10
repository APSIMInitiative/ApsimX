// -----------------------------------------------------------------------
// <copyright file="MemoPresenter.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Text;
    using Models;
    using Views;
    using APSIM.Shared.Utilities;
    using System.Xml;

    /// <summary>
    /// Presents the text from a memo component.
    /// </summary>
    public class MemoPresenter : IPresenter, IExportable
    {
        /// <summary>
        /// The memo object
        /// </summary>
        private Memo memoModel;

        /// <summary>
        /// The memo view
        /// </summary>
        private HTMLView memoViewer;

        /// <summary>
        /// The explorer presenter used
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attach the 'Model' and the 'View' to this presenter.
        /// </summary>
        /// <param name="model">The model to use</param>
        /// <param name="view">The view object</param>
        /// <param name="explorerPresenter">The explorer presenter used</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.memoModel = model as Memo;
            this.memoViewer = view as HTMLView;
            this.explorerPresenter = explorerPresenter;

            this.memoViewer.ImagePath = Path.GetDirectoryName(explorerPresenter.ApsimXFile.FileName);
            this.memoViewer.SetContents(this.memoModel.MemoText, true);
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            string markdown = this.memoViewer.GetMarkdown();
            if (markdown != memoModel.MemoText)
                this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.memoModel, "MemoText", markdown));
        }

        /// <summary>
        /// The model has changed so update our view.
        /// </summary>
        /// <param name="changedModel">The model object that has changed</param>
        public void OnModelChanged(object changedModel)
        {
            if (changedModel == this.memoModel)
                this.memoViewer.SetContents(((Memo)changedModel).MemoText, true);
        }

        /// <summary>
        /// Export the contents of this memo to the specified file.
        /// </summary>
        /// <param name="folder">The name of the folder</param>
        /// <returns>The text from the memo</returns>
        public string ConvertToHtml(string folder)
        {
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(this.memoModel.MemoText);
            XmlNode bodyNode = XmlUtilities.Find(doc.DocumentElement, "body");
            return bodyNode.InnerXml;
        }
    }
}
