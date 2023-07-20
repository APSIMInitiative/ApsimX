using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Models.Core;
using Models.Factorial;

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
        //// <summary>Link to simulations</summary>
        [Link]
        private Simulations Simulations = null;

        /// <summary>Gets or sets the playlist text.</summary>
        [Description("Text of the playlist")]
        public string Text { get; set; }

        /// <summary>
        /// Returns the name of all simulations that match the text
        /// Returns null if empty
        /// Throws an exception if no valid simulations were found
        /// </summary>
        public string[] GetListOfSimulations(List<Simulation> allSimulations = null, List<Experiment> allExperiments = null)
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
                return null; // this will let the runner run normally

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
                    string expression = part;
                    //remove symbols [ ]
                    expression = expression.Replace("[", "");
                    expression = expression.Replace("]", "");

                    //trim whitespace
                    expression = expression.Trim();

                    //convert to lower
                    expression = expression.ToLower();

                    //convert our wildcard to regex symbol
                    expression = expression.Replace("*", "[\\s\\S]*");
                    expression = expression.Replace("#", ".");
                    expression = "^" + expression + "$";
                    Regex regex = new Regex(expression);

                    foreach (Simulation sim in allSimulations)
                    {
                        if (regex.IsMatch(sim.Name.ToLower()))
                            if (sim.FindAncestor<Experiment>() == null) //don't add if under experiment
                                if (names.Contains(sim.Name.ToLower()) == false)
                                    names.Add(sim.Name);
                    }

                    foreach (Experiment exp in allExperiments)
                    {
                        List<Core.Run.SimulationDescription> expNames = exp.GetSimulationDescriptions().ToList();
                        //match experiment name
                        if (regex.IsMatch(exp.Name.ToLower()))
                        {
                            if (names.Contains(exp.Name.ToLower()) == false)
                                foreach (Core.Run.SimulationDescription expN in expNames)
                                    if (names.Contains(expN.Name.ToLower()) == false)
                                        names.Add(expN.Name);
                        }
                        else
                        {
                            //match against the experiment variations
                            foreach (Core.Run.SimulationDescription expN in expNames)
                                if (regex.IsMatch(expN.Name.ToLower()))
                                    if (names.Contains(expN.Name.ToLower()) == false)
                                        names.Add(expN.Name);
                        }
                    }
                }
            }
            if (names.Count == 0)
                return null;
            else
                return names.ToArray();
        }
    }
}