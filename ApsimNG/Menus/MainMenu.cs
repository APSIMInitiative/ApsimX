namespace UserInterface.Presenters
{
    using System;
    using System.Diagnostics;
    using APSIM.Shared.Utilities;
    using Models.Core;

    /// <summary>
    /// This class contains methods for all main menu items that the ExplorerView exposes to the user.
    /// </summary>
    public class MainMenu
    {
        /// <summary>
        /// Reference to the main presenter.
        /// </summary>
        private MainPresenter presenter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainMenu" /> class.
        /// </summary>
        /// <param name="mainPresenter">The presenter to work with</param>
        public MainMenu(MainPresenter mainPresenter)
        {
            presenter = mainPresenter;
        }

        /// <summary>
        /// User has clicked on Save
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu("Save", "<Ctrl>s")]
        public void OnSaveClick(object sender, EventArgs e)
        {
            try
            {
                ExplorerPresenter explorer = presenter.GetCurrentExplorerPresenter();
                if (explorer != null)
                    explorer.Save();
            }
            catch (Exception err)
            {
                presenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has clicked on SaveAs
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu("Save As", "<Ctrl><Shift>s")]
        public void OnSaveAsClick(object sender, EventArgs e)
        {
            try
            {
                ExplorerPresenter explorer = presenter.GetCurrentExplorerPresenter();
                if (explorer != null)
                    explorer.SaveAs();
            }
            catch (Exception err)
            {
                presenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has clicked on Undo
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu("Undo", "<Ctrl>z")]
        public void OnUndoClick(object sender, EventArgs e)
        {
            try
            {
                ExplorerPresenter explorer = presenter.GetCurrentExplorerPresenter();
                if (explorer != null)
                    explorer.CommandHistory.Undo();
            }
            catch (Exception err)
            {
                presenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has clicked on Redo
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu("Redo", "<Ctrl>y")]
        public void OnRedoClick(object sender, EventArgs e)
        {
            try
            {
                ExplorerPresenter explorer = presenter.GetCurrentExplorerPresenter();
                if (explorer != null)
                    explorer.CommandHistory.Redo();
            }
            catch (Exception err)
            {
                presenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has clicked on Redo
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu("Split Screen", "<Ctrl>t")]
        public void ToggleSecondExplorerViewVisible(object sender, EventArgs e)
        {
            try
            {
                presenter.ToggleSecondExplorerViewVisible();
            }
            catch (Exception err)
            {
                presenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has clicked on clear status panel.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [MainMenu("Clear Status", "<Ctrl>g")]
        public void ClearStatusPanel(object sender, EventArgs args)
        {
            try
            {
                presenter.ClearStatusPanel();
            }
            catch (Exception err)
            {
                presenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has clicked on Help
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu("Help", "F1")]
        public void OnHelp(object sender, EventArgs e)
        {
            try
            {
                ProcessUtilities.ProcessStart("https://apsimnextgeneration.netlify.com/");
            }
            catch (Exception err)
            {
                presenter.ShowError(err);
            }
        }
    }
}