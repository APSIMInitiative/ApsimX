namespace UserInterface.Presenters
{
    using System;
    using System.Diagnostics;
    using Models.Core;

    /// <summary>
    /// This class contains methods for all main menu items that the ExplorerView exposes to the user.
    /// </summary>
    public class MainMenu
    {
        /// <summary>
        /// Reference to the ExplorerPresenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainMenu" /> class.
        /// </summary>
        /// <param name="explorerPresenter">The explorer presenter to work with</param>
        public MainMenu(ExplorerPresenter explorerPresenter)
        {
            this.explorerPresenter = explorerPresenter;
        }

        /// <summary>
        /// User has clicked on Save
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu(MenuName = "Save")]
        public void OnSaveClick(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.Save();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has clicked on SaveAs
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu(MenuName = "Save As")]
        public void OnSaveAsClick(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.SaveAs();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has clicked on Undo
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu(MenuName = "Undo")]
        public void OnUndoClick(object sender, EventArgs e)
        {
            try
            {
                this.explorerPresenter.CommandHistory.Undo();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has clicked on Redo
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu(MenuName = "Redo")]
        public void OnRedoClick(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.CommandHistory.Redo();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has clicked on Redo
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu(MenuName = "Split screen")]
        public void ToggleSecondExplorerViewVisible(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.MainPresenter.ToggleSecondExplorerViewVisible();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has clicked on clear status panel.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [MainMenu(MenuName = "Clear Status")]
        public void ClearStatusPanel(object sender, EventArgs args)
        {
            try
            {
                explorerPresenter.MainPresenter.ClearStatusPanel();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has clicked on Help
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        [MainMenu(MenuName = "Help")]
        public void OnHelp(object sender, EventArgs e)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "https://apsimnextgeneration.netlify.com/";
                process.Start();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }
    }
}