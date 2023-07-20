using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Wordprocessing;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Run;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnitTests.Core
{
    /// <summary>This is a test class for the RunnableSimulationList class</summary>
    [TestFixture]
    public class PlaylistTests
    {
        /// <summary>Ensure a single simulation runs.</summary>
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

            //Tests that should fail as they find no matching sims
            names = new string[0] { };
            //All caps
            playlistText.Add("SIM");
            expectedSimulations.Add(names);

            //The base experiement Sim
            playlistText.Add("ExpSim");
            expectedSimulations.Add(names);

            for (int i = 0; i < expectedSimulations.Count; i++)
            {
                Simulations sims = FileFormat.ReadFromString<Simulations>(json, e => throw e, false).NewModel as Simulations;
                Playlist playlist = sims.FindChild<Playlist>();
                playlist.Text = playlistText[i];

                if (expectedSimulations[i].Length > 0)
                {
                    Runner runner = new Runner(sims);
                    List<Exception> errors = runner.Run();

                    // Check that no errors were thrown
                    Assert.AreEqual(0, errors.Count);

                    DataStore dataStore = sims.FindChild<DataStore>();
                    List<String> dataStoreNames = dataStore.Reader.SimulationNames;
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
                    Assert.Throws<Exception>(() => new Runner(sims));
                }
                
            }           
        }
    }
}