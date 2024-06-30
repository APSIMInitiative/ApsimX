using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;
using global::UserInterface.Commands;
using global::UserInterface.Hotkeys;
using Models.Core;
using Models.Core.Run;
using Models;
using Utility;

namespace UserInterface.Presenters
{
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
        /// The current run command. When this is null, the "run" menu item is
        /// disabled.
        /// </summary>
        private RunCommand command;

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
        [MainMenu("Split Screen")]
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
                ProcessUtilities.ProcessStart("https://apsimnextgeneration.netlify.app/");
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
        [MainMenu("Run", "F5")]
        public void OnRun(object sender, EventArgs e)
        {
            try
            {
                // If we're already running some simulations, don't do anything.
                if (command != null && command.IsRunning)
                    return;

                // If no explorer presenter has focus, don't do anything.
                ExplorerPresenter explorer = presenter.GetCurrentExplorerPresenter();
                if (explorer == null)
                    return;

                // Write .apsimx file to disk.
                if (Configuration.Settings.AutoSave)
                    explorer.Save();

                if (string.IsNullOrEmpty(explorer.ApsimXFile.FileName))
                     throw new InvalidOperationException("Please save before running simulation.");

                IModel model = FindRunnable(explorer.CurrentNode);
                if (model == null)
                    throw new InvalidOperationException("Unable to find a model which may be run.");

                Runner runner = new Runner(model, runType: Runner.RunTypeEnum.MultiThreaded, wait: false);
                command = new RunCommand(model.Name, runner, explorer);
                command.Do();
            }
            catch (Exception err)
            {
                presenter.ShowError(err);
            }
        }

        public static IModel FindRunnable(IModel currentNode)
        {
            if (currentNode is Folder && currentNode.FindDescendant<ISimulationDescriptionGenerator>() != null)
                return currentNode;
            IEnumerable<ISimulationDescriptionGenerator> runnableModels = currentNode.FindAllAncestors<ISimulationDescriptionGenerator>();
            if (currentNode is ISimulationDescriptionGenerator runnable)
                runnableModels = runnableModels.Prepend(runnable);
            if (runnableModels.Any())
                return runnableModels.LastOrDefault() as IModel;
            Simulations topLevel = currentNode as Simulations;
            if (topLevel != null)
                return topLevel;
            if (currentNode is Playlist)
                return currentNode;
            return currentNode.FindAncestor<Simulations>();
        }
    }
}