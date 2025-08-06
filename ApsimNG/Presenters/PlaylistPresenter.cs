using Models;
using UserInterface.Views;
using System;
using System.Collections.Generic;
using Models.Core;
using Models.Factorial;
using System.Linq;
using System.Threading.Tasks;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Presents the text from a memo component.
    /// </summary>
    public class PlaylistPresenter : IPresenter
    {
        /// <summary>Reference to the playlist model</summary>
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
        /// A list of all simulations in this file that is saved in a cache to help search times
        /// </summary>
        private List<Simulation> simNameCache;

        /// <summary>
        /// A list of all experiments in this file that is saved in a cache to help search times
        /// </summary>
        private List<Experiment> expNameCache;

        /// <summary>
        /// Used to id search requests so that they cannot overwrite each other if returned out of order
        /// Incremented once each time a name search is conducted
        /// </summary>
        private int searchCounter;

        /// <summary>
        /// Attach the 'Model' and the 'View' to this presenter.
        /// </summary>
        /// <param name="model">The model to use</param>
        /// <param name="view">The view object</param>
        /// <param name="parentPresenter">The explorer presenter used</param>
        public void Attach(object model, object view, ExplorerPresenter parentPresenter)
        {
            playlistModel = model as Playlist;
            playlistModel.ClearSearchCache();

            explorerPresenter = parentPresenter;
            explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;

            playlistView = view as TextAndCodeView;
            playlistView.editorView.Text = playlistModel.Text;
            playlistView.editorView.TextHasChangedByUser += this.OnTextHasChangedByUser;

            //This is the instructions at the top of the page.
            string instructions = "";
            instructions += "<b><big>Playlist</big></b>\n";
            instructions += "Enter a list of names of simulations that you want to run. Case insensitive.\n";
            instructions += "A wildcard * can be used to represent any number of characters.\n";
            instructions += "A wildcard # can be used to represent any single character.\n";
            instructions += "\n";
            instructions += "Simulations and Experiments can also be added to this playlist by right-clicking on them in the GUI.\n";
            instructions += "\n";
            instructions += "<b>Examples:</b>\n";
            instructions += "Sim1, Sim2, Sim3   - <i>Runs simulations with exactly these names</i>\n";
            instructions += "[Sim1, Sim2, Sim3] - <i>Also allows [ ] around the entry</i>\n";
            instructions += "\nSim1  - <i>Entries can be entered over multiple lines</i>\nSim2\n\n";
            instructions += "Sim#  - <i>Runs simulations like Sim1, SimA, Simm, but will not run Sim or Sim11</i>\n";
            instructions += "Sim*  - <i>Runs simulations that start with Sim</i>\n";
            instructions += "*Sim  - <i>Runs simulations that end with Sim</i>\n";
            instructions += "*Sim* - <i>Runs simulations with Sim anywhere in the name</i>\n";
            playlistView.SetLabelText(instructions);

            //load in all the simulation and experiment names
            Simulations sims = playlistModel.Node.FindParent<Simulations>(recurse: true);
            simNameCache = sims.Node.FindChildren<Simulation>(recurse: true).ToList();
            expNameCache = sims.Node.FindChildren<Experiment>(recurse: true).ToList();

            //search our search timer to 0
            searchCounter = 0;

            //update the output at the bottom based on what is in the playlist
            UpdateListOfSimulations();
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
                UpdateListOfSimulations();
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
            if (playlistView.editorView.Text != null)
                playlistView.editorView.Text = playlistModel.Text;

            UpdateListOfSimulations();
        }

        /// <summary>
        /// Update the output text with a list of all simulations that match the entries in the playlist
        /// Uses an async function to prevent the GUI from freezing.
        /// </summary>
        private async void UpdateListOfSimulations()
        {
            playlistView.SetOutputText("Matching Simulations:\nSearching...");

            if (searchCounter >= 1)
            {
                searchCounter = 2;
                return;
            }
            else
            {
                searchCounter = 1;
            }

            string[] names = (await GetSimNamesAsync());

            string output = $"Matching Simulations: {names.Length}\n";
            if (names != null)
            {
                output += string.Join(", ", names);
            }
            else
            {
                output += "[No Matches Found]";
            }
            playlistView.SetOutputText(output);

            if (searchCounter == 2) //if we have had another search pending
            {
                searchCounter = 0;
                UpdateListOfSimulations();
            } else
            {
                searchCounter = 0;
            }
        }

        /// <summary>Async wrapper around GetListOfSimulations</summary>
        private async Task<string[]> GetSimNamesAsync()
        {
            return await Task.Run(() => playlistModel.GenerateListOfSimulations(simNameCache, expNameCache));
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            playlistView.editorView.TextHasChangedByUser -= this.OnTextHasChangedByUser;
            explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
        }
    }
}
