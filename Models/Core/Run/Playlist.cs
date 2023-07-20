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
        private Simulations simulations = null;

        /// <summary>Gets or sets the memo text.</summary>
        [Description("Text of the playlist")]
        public string Text { get; set; }

        /// <summary>
        /// Returns the name of all simulations that match the text
        /// Returns null if empty
        /// Throws an exception if no valid simulations were found
        /// </summary>
        public List<string> GetListOfSimulations()
        {
            simulations = this.Parent as Simulations;

            List<string> names = new List<string>();

            string[] lines = Text.Split('\n');
            if (Text.Length == 0)
                return null; // this will let the runner run normally

            List<Simulation> allSimulations = simulations.FindAllDescendants<Simulation>().ToList();
            List<Experiment> allExperiments = simulations.FindAllDescendants<Experiment>().ToList();

            foreach (string line in lines)
            {
                //convert our wildcard to regex symbol
                string expression = line.ToLower();
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
            if (names.Count == 0)
                throw new Exception("Playlist is enabled but no simulations or experiments match the contents of the list.");
            else
                return names;
        }
    }
}