using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Models.Core;
using Models.Factorial;
using Newtonsoft.Json;

namespace Models
{

    /// <summary>
    /// A report class for writing output to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.TextAndCodeView")]
    [PresenterName("UserInterface.Presenters.PlaylistPresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    public class Playlist : Model
    {
        [Serializable]
        private class PlaylistPrevSearch
        {
            public string searchString = null;
            public string[] results = null;
        }

        /// <summary>Link to simulations</summary>
        [Link]
        private Simulations Simulations = null;

        /// <summary>Link to simulations</summary>
        [JsonIgnore]
        private List<PlaylistPrevSearch> searchCache = new List<PlaylistPrevSearch>();

        [JsonIgnore]
        private string[] lastSearch = null;

        /// <summary>The Playlist text that is used for comparisions</summary>
        [Description("Text of the playlist")]
        public string Text { get; set; }

        /// <summary>
        /// Returns the last generated list of simulation names.
        /// If that list has not been searched yet, this will generate the list.
        /// </summary>
        /// <returns>
        /// An array of simulation and simulation variations names that were found during the last search. 
        /// Will return an empty array if no matches are found.
        /// </returns>
        public string[] GetListOfSimulations()
        {
            if (lastSearch == null)
                return GenerateListOfSimulations();
            else
                return lastSearch;
        }

        /// <summary>
        /// Returns the name of all simulations that match the text
        /// Returns null if empty
        /// Throws an exception if no valid simulations were found
        /// In order to allow the presenter to run better, you can pass in a list of simulations and experiments
        /// </summary>
        /// <param name="allSimulations">Optional: A list of all simulations to compare against</param>
        /// <param name="allExperiments">Optional: A list of all experiments to compare against</param>
        /// <returns>
        /// An array of simulation and simulation variations names that match the text of this playlist. 
        /// Will return an empty array if no matches are found.
        /// </returns>
        public string[] GenerateListOfSimulations(List<Simulation> allSimulations = null, List<Experiment> allExperiments = null)
        {
            if (Simulations == null)
                Simulations = this.FindAncestor<Simulations>();

            if (allSimulations == null)
                allSimulations = Simulations.FindAllDescendants<Simulation>().ToList();

            if (allExperiments == null)
                allExperiments = Simulations.FindAllDescendants<Experiment>().ToList();

            Simulations = this.Parent as Simulations;

            List<string> names = new List<string>();

            if (Text == null || Text.Length == 0)
                return names.ToArray(); // this will let the runner run normally

            string[] lines = Text.Split('\n');

            foreach (string line in lines)
            {
                List<string> parts = new List<string>();
                if (line.Contains(','))
                {
                    parts = line.Split(',').ToList();
                } 
                else
                {
                    parts.Add(line);
                }

                foreach (string part in parts)
                {
                    List<string> resultsForThisPart = new List<string>();
                    string cleanPart = cleanString(part);

                    string expression = cleanPart;
                    
                    //convert our wildcard to regex symbol
                    expression = expression.Replace("*", "[\\s\\S]*");
                    expression = expression.Replace("#", ".");
                    expression = "^" + expression + "$";
                    Regex regex = new Regex(expression);

                    bool inCache = false;
                    foreach (PlaylistPrevSearch search in searchCache)
                    {
                        if (cleanPart == search.searchString)
                        {
                            inCache = true;
                            for (int i = 0; i < search.results.Length; i++)
                                names.Add(search.results[i]);
                        }
                    }

                    if(!inCache)
                    {
                        foreach (Simulation sim in allSimulations)
                        {
                            if (regex.IsMatch(sim.Name.ToLower()))
                            {
                                if (sim.FindAncestor<Experiment>() == null)//don't add if under experiment
                                {
                                    if (names.Contains(sim.Name) == false)
                                    {
                                        names.Add(sim.Name);
                                        resultsForThisPart.Add(sim.Name);
                                    }
                                }
                            }      
                        }

                        foreach (Experiment exp in allExperiments)
                        {
                            List<Core.Run.SimulationDescription> expNames = exp.GetSimulationDescriptions(false).ToList();
                            //match experiment name
                            if (regex.IsMatch(exp.Name.ToLower()))
                            {
                                if (names.Contains(exp.Name) == false)
                                {
                                    foreach (Core.Run.SimulationDescription expN in expNames)
                                    {
                                        if (names.Contains(expN.Name) == false)
                                        {
                                            names.Add(expN.Name);
                                            resultsForThisPart.Add(expN.Name);
                                        }
                                    }
                                }        
                            }
                            else
                            {
                                //match against the experiment variations
                                foreach (Core.Run.SimulationDescription expN in expNames)
                                {
                                    if (regex.IsMatch(expN.Name.ToLower()))
                                    {
                                        if (names.Contains(expN.Name) == false)
                                        {
                                            names.Add(expN.Name);
                                            resultsForThisPart.Add(expN.Name);
                                        }
                                    }
                                }
                            }
                        }
                        PlaylistPrevSearch search = new PlaylistPrevSearch();
                        search.searchString = cleanPart;
                        search.results = resultsForThisPart.ToArray();
                        searchCache.Add(search);
                    }
                }
            }
            lastSearch = names.ToArray();
            return lastSearch;
        }

        /// <summary>
        /// Adds an array of strings to the playlist.
        /// Will make a newline if the last line of the playlist already has content
        /// </summary>
        /// <param name="lines">An array of strings that should be added</param>
        public void AddSimulationNamesToList(string[] lines)
        {
            if (Text.Length > 0 && Text.Last<char>() != '\n')//make sure we are adding this to an empty line
                Text += "\n";

            foreach (string line in lines)
                Text += line + "\n";
        }

        /// <summary>
        /// Clears the previous search cache so that new results can be searched for.
        /// </summary>
        public void ClearSearchCache()
        {
            searchCache = new List<PlaylistPrevSearch>();
        }

        private string cleanString(string input)
        {
            string output = "";
            output = output.Replace("\t", " ");

            foreach (char c in input)
                if (char.IsLetter(c) || char.IsNumber(c) || c == '*' || c == '#' || c == ' ' || c == '_' || c == '-' || c == '.')
                    output += c;

            //trim whitespace
            output = output.Trim();

            //convert to lower
            output = output.ToLower();

            return output;
        }
    }
}