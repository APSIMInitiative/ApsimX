using System;
using System.Collections.Generic;
using System.Text;
using UserInterface.Views;
using Models;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Presents the text from a memo component.
    /// </summary>
    public class MemoPresenter : IPresenter
    {
        private Memo MemoModel;
        private MemoView MemoViewer;

        private ExplorerPresenter ExplorerPresenter;

        public void Attach(object Model, object View, ExplorerPresenter explorerPresenter)
        {
            MemoModel = Model as Memo;
            MemoViewer = View as MemoView;
            ExplorerPresenter = explorerPresenter;

            MemoViewer.MemoText = MemoModel.MemoText;

            MemoViewer.MemoUpdate += Update;
        }

        public void Detach()
        {
            MemoViewer.MemoUpdate -= Update;
        }

        /// <summary>
        /// Handles the event from the view to update the memo component text.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
    }
}
