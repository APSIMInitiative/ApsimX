using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Run;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace UnitTests.Core
{
    /// <summary>This is a test class for the RunnableSimulationList class</summary>
    [TestFixture]
    public class PlaylistTests
    {
        /// <summary>Testing a number of variations of playlist text to make sure they run correctly.</summary>
        [Test]
        public void RunSimulationsWithPlaylist()
        {
            //read in our base test that we'll use for this
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.Run.PlaylistTests.apsimx");

            //Lists to hold our variations
            List<string> playlistText = new List<string>();
            List<string[]> expectedSimulations = new List<string[]>();

            string[] names;

            //Empty Playlist, should run all
            playlistText.Add("");
            names = new string[9]{ "Sim", "Sim2", "Sim3", "Sim4", "ExpFactor2000-01-01", "ExpFactor2000-02-01", "ExpFactor2000-03-01", "ExpFactor2000-04-01", "ExpFactor2000-05-01" };
            expectedSimulations.Add(names);

            //Full Wildcard, should run all
            playlistText.Add("*");
            expectedSimulations.Add(names);

            //More than one Wildcard, should run all
            playlistText.Add("**");
            expectedSimulations.Add(names);

            playlistText.Add("**#");
            expectedSimulations.Add(names);

            //Wildcards with spaces, should run all
            playlistText.Add("Sim");
            names = new string[1] { "Sim" };
            expectedSimulations.Add(names);

            //Actual Names, should run all
            playlistText.Add("Sim\nSim2\nSim4");
            names = new string[3] { "Sim", "Sim2", "Sim4" };
            expectedSimulations.Add(names);
            
            //with commas and sqaure brackets
            playlistText.Add("[Sim,Sim2,Sim4]");
            expectedSimulations.Add(names);

            //Name with single wildcard, should pick up 3 of them
            playlistText.Add("Sim#");
            names = new string[3] { "Sim2", "Sim3", "Sim4" };
            expectedSimulations.Add(names);

            //Name with single wildcard, should pick up 4 of them
            playlistText.Add("Sim*");
            names = new string[4] { "Sim", "Sim2", "Sim3", "Sim4" };
            expectedSimulations.Add(names);

            //Experiement sims, should be 5
            playlistText.Add("*Factor*");
            names = new string[5] { "ExpFactor2000-01-01", "ExpFactor2000-02-01", "ExpFactor2000-03-01", "ExpFactor2000-04-01", "ExpFactor2000-05-01" };
            expectedSimulations.Add(names);

            //Sims with 4 Letter names
            playlistText.Add("####");
            names = new string[3] { "Sim2", "Sim3", "Sim4" };
            expectedSimulations.Add(names);

            //Experiment
            playlistText.Add("Exp");
            names = new string[5] { "ExpFactor2000-01-01", "ExpFactor2000-02-01", "ExpFactor2000-03-01", "ExpFactor2000-04-01", "ExpFactor2000-05-01" };
            expectedSimulations.Add(names);

            //Getting the same simulation multiple times
            playlistText.Add("Sim, Sim, Sim");
            names = new string[1] { "Sim" };
            expectedSimulations.Add(names);

            //All caps
            playlistText.Add("SIM");
            names = new string[1] { "Sim" };
            expectedSimulations.Add(names);

            //Tests that should fail as they find no matching sims
            names = new string[0] { };

            //The base experiement Sim
            playlistText.Add("ExpSim");
            expectedSimulations.Add(names);

            //symbols
            playlistText.Add("~!@#$%^&*()_+{}[]:\";'<>?,./`' |\\");
            expectedSimulations.Add(names);

            for (int i = 0; i < expectedSimulations.Count; i++)
            {
                Simulations sims = FileFormat.ReadFromString<Simulations>(json, e => throw e, false).NewModel as Simulations;

                Playlist playlist = sims.FindChild<Playlist>();
                playlist.Text = playlistText[i];

                if (expectedSimulations[i].Length > 0)
                {
                    Runner runner = new Runner(playlist);
                    List<Exception> errors = runner.Run();
                    // Check that no errors were thrown
                    Assert.That(errors.Count, Is.EqualTo(0));

                    DataStore dataStore = sims.FindChild<DataStore>();
                    List<String> dataStoreNames = dataStore.Reader.SimulationNames;

                    //check that the datastore and expected simulations have the same amount of entries
                    Assert.That(dataStoreNames.Count, Is.EqualTo(expectedSimulations[i].Length));
                    for (int j = 0; j < expectedSimulations[i].Length; j++)
                    {
                        if (!dataStoreNames.Contains(expectedSimulations[i][j]))
                        {
                            Assert.Fail();
                        }
                    }
                } 
                else
                {
                    Assert.Throws<Exception>(() => new Runner(playlist));
                }
                
            }           
        }

        /// <summary>Testing a number of variations of playlist text to make sure they run correctly.</summary>
        [Test]
        public void RunSimulationsWithPlaylistCheckCaching()
        {
            string[] expectedSimulations1 = new string[4] { "Sim", "Sim2", "Sim3", "Sim4" };
            string[] expectedSimulations2 = new string[3] { "Sim", "Sim2", "Sim4" };

            //read in our base test that we'll use for this
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.Run.PlaylistTests.apsimx");
            Simulations sims = FileFormat.ReadFromString<Simulations>(json, e => throw e, false).NewModel as Simulations;
            Playlist playlist = sims.FindChild<Playlist>();
            playlist.Text = "Sim*\n";

            Assert.That(playlist.GenerateListOfSimulations(), Is.EqualTo(expectedSimulations1));

            //now change the name of one of the simulations without clearing the cache,
            //should give the same 4 names if reading from cache correctly.
            sims.FindChild("Sim3").Name = "DifferentName";
            Assert.That(playlist.GenerateListOfSimulations(), Is.EqualTo(expectedSimulations1));

            //now clear the cache and run again, should only get 3 sims this time.
            playlist.ClearSearchCache();
            Assert.That(playlist.GenerateListOfSimulations(), Is.EqualTo(expectedSimulations2));

            //Check that GetListOfSimulations is working, should return the previous result again.
            Assert.That(playlist.GetListOfSimulations(), Is.EqualTo(expectedSimulations2));
        }
    }
}