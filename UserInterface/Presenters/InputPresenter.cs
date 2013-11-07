using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Attaches an Input model to an Input View.
    /// </summary>
    public class InputPresenter : IPresenter
    {
        private Input Input;
        private IInputView View;
        private CommandHistory CommandHistory;

        /// <summary>
        /// Attaches an Input model to an Input View.
        /// </summary>
        public void Attach(object model, object view, CommandHistory commandHistory)
        {
            Input = model as Input;
            View = view as IInputView;
            CommandHistory = commandHistory;

            View.FileName = Input.FileName;
            View.GridView.DataSource = Input.GetTable();

            View.BrowseButtonClicked += OnBrowseButtonClicked;
            CommandHistory.ModelChanged += OnModelChanged;
        }


        /// <summary>
        /// Detaches an Input model from an Input View.
        /// </summary>
        public void Detach()
        {
            View.BrowseButtonClicked -= OnBrowseButtonClicked;
            CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>
        /// Browse button was clicked by user.
        /// </summary>
        private void OnBrowseButtonClicked(object sender, OpenDialogArgs e)
        {
            CommandHistory.Add(new Commands.ChangePropertyCommand(Input, "FileName", e.FileName));
        }

        /// <summary>
        /// The model has changed - update the view.
        /// </summary>
        void OnModelChanged(object changedModel)
        {
            View.FileName = Input.FileName;
            View.GridView.DataSource = Input.GetTable();
        }


    }
}
