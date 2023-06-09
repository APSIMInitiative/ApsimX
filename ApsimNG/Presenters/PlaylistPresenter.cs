using Models;
using UserInterface.Views;
using System;
using APSIM.Shared.Utilities;
using UserInterface.Interfaces;
using Models.Factorial;
using System.Collections.Generic;
using UserInterface.EventArguments;
using static Models.Core.AutoDocumentation;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Presents the text from a memo component.
    /// </summary>
    public class PlaylistPresenter : IPresenter
    {
        /// <summary>The memo model.</summary>
        private Playlist playlistModel;

        /// <summary>
        /// The view object
        /// </summary>
        private PlaylistView playlistView;

        /// <summary>
        /// The presenter
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// The intellisense object used to generate completion options.
        /// </summary>
        private IntellisensePresenter intellisense;

        /// <summary>
        /// Attach the 'Model' and the 'View' to this presenter.
        /// </summary>
        /// <param name="model">The model to use</param>
        /// <param name="view">The view object</param>
        /// <param name="parentPresenter">The explorer presenter used</param>
        public void Attach(object model, object view, ExplorerPresenter parentPresenter)
        {
            playlistModel = model as Playlist;

            explorerPresenter = parentPresenter;
            explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;

            playlistView = view as PlaylistView;
            playlistView.editorView.TextHasChangedByUser += this.OnTextHasChangedByUser;
            playlistView.editorView.Text = "Simulation";//debugging

            //if (factor.Specifications != null)
            //    this.playlistView.Lines = factor.Specifications.ToArray();

            
            //playlistView.ContextItemsNeeded += this.OnContextItemsNeeded;
            

            intellisense = new IntellisensePresenter(playlistView as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;
        }

        private void HelpBtnClicked(object sender, EventArgs e)
        {
            try
            {
                ProcessUtilities.ProcessStart("https://apsimnextgeneration.netlify.com/usage/memo/");
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// User has changed the paths. Save to model.
        /// </summary>
        /// <param name="sender">The text control</param>
        /// <param name="e">Event arguments</param>
        private void OnTextHasChangedByUser(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(playlistModel, "Text", playlistView.editorView.Text));
                explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// The model has changed probably by an undo.
        /// </summary>
        /// <param name="changedModel">The model</param>
        private void OnModelChanged(object changedModel)
        {
            //playlistView.Lines = model.Specifications.ToArray();
        }

        /// <summary>
        /// Invoked when the user selects an item in the intellisense.
        /// Inserts the selected item at the caret.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs args)
        {
            if (string.IsNullOrEmpty(args.ItemSelected))
                playlistView.editorView.InsertAtCaret(args.ItemSelected);
            else
                playlistView.editorView.InsertCompletionOption(args.ItemSelected, args.TriggerWord);
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            intellisense.Cleanup();
            playlistView.editorView.TextHasChangedByUser -= this.OnTextHasChangedByUser;
            //playlistView.ContextItemsNeeded -= this.OnContextItemsNeeded;
            explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
        }
    }
}
