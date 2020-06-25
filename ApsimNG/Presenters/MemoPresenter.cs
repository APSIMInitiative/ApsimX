namespace UserInterface.Presenters
{
    using System.IO;
    using Models;
    using Views;
    using System;

    /// <summary>
    /// Presents the text from a memo component.
    /// </summary>
    public class MemoPresenter : IPresenter
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
            this.memoViewer.SetContents(this.memoModel.Text, true);
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            try
            {
                string markdown = this.memoViewer.GetMarkdown();
                if (markdown != this.memoModel.Text)
                {
                    this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.memoModel, "Text", markdown));
                }
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// The model has changed so update our view.
        /// </summary>
        /// <param name="changedModel">The model object that has changed</param>
        public void OnModelChanged(object changedModel)
        {
            if (changedModel == this.memoModel)
            {
                this.memoViewer.SetContents(((Memo)changedModel).Text, true);
            }
        }

    }
}
