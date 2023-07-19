using Models;
using UserInterface.Views;
using System;
using APSIM.Shared.Utilities;

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
        private TextAndCodeView playlistView;

        /// <summary>
        /// The presenter
        /// </summary>
        private ExplorerPresenter explorerPresenter;

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

            playlistView = view as TextAndCodeView;
            playlistView.editorView.Text = playlistModel.Text;
            playlistView.editorView.TextHasChangedByUser += this.OnTextHasChangedByUser;
            
            string instructions = "";
            instructions += "Enter a list of names of simulations that you want to run. If this list is left empty, all active simulations will be run.\n";
            instructions += "A wildcard * can be used to represent any number of characters.\n";
            instructions += "A wildcard # can be used to represent any single character.\n";
            instructions += "\n";
            instructions += "This node can be disabled to restore the default simulation running behaviour.\n";
            playlistView.SetLabelText(instructions);
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

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            playlistView.editorView.TextHasChangedByUser -= this.OnTextHasChangedByUser;
            explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
        }
    }
}
