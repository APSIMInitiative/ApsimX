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
    [ViewName("UserInterface.Views.PlaylistView")]
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

            List<Model> children = simulations.FindAllChildren<Model>().ToList();

            foreach (string line in lines)
            {
                //convert our wildcard to regex symbol
                string expression = line.Replace("*", "[\\s\\S]*");
                expression = "^" + expression + "$";
                Regex regex = new Regex(expression);

                foreach (Model child in children)
                {
                    if (child is Simulation)
                    {
                        Simulation sim = child as Simulation;
                        if (regex.IsMatch(sim.Name))
                            if (names.Contains(sim.Name) == false)
                                names.Add(sim.Name);
                    }
                    else if (child is Experiment)
                    {
                        Experiment exp = child as Experiment;
                        if (regex.IsMatch(exp.Name))
                        {
                            if (names.Contains(exp.Name) == false)
                            {
                                List<Core.Run.SimulationDescription> expNames = exp.GetSimulationDescriptions().ToList();
                                foreach (Core.Run.SimulationDescription expN in expNames)
                                    names.Add(expN.Name);
                            }
                        }
                    }
                    
                }
            }
            if (names.Count == 0)
                throw new Exception("Playlist is enabled but no simuations or experiments match the contents of the list.");
            else
                return names;
        }
    }
}