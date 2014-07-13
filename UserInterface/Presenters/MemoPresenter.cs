using System;
using System.Collections.Generic;
using System.Text;
using UserInterface.Views;
using Models;
using System.Drawing;
using System.IO;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Presents the text from a memo component.
    /// </summary>
    public class MemoPresenter : IPresenter, IExportable
    {
        private Memo MemoModel;
        private HTMLView MemoViewer;

        private ExplorerPresenter ExplorerPresenter;

        /// <summary>
        /// Attach the 'Model' and the 'View' to this presenter.
        /// </summary>
        public void Attach(object Model, object View, ExplorerPresenter explorerPresenter)
        {
            MemoModel = Model as Memo;
            MemoViewer = View as HTMLView;
            ExplorerPresenter = explorerPresenter;

            MemoViewer.MemoText = MemoModel.MemoText;

            MemoViewer.MemoUpdate += Update;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            MemoViewer.MemoUpdate -= Update;
        }

        /// <summary>
        /// Handles the event from the view to update the memo component text.
        /// </summary>
        void Update(object sender, EventArgs e)
        {
            ExplorerPresenter.CommandHistory.Add(new Commands.ChangePropertyCommand(MemoModel, "MemoText", ((EditorArgs)e).TextString));
        }

        /// <summary>
        /// The model has changed so update our view.
        /// </summary>
        void CommandHistory_ModelChanged(object changedModel)
        {
            if (changedModel == MemoModel)
                MemoViewer.MemoText = ((Memo)changedModel).MemoText;
        }

        /// <summary>
        /// Export the contents of this memo to the specified file.
        /// </summary>
        public string ConvertToHtml(string folder)
        {
            return MemoViewer.MemoText;
        }
    }
}
