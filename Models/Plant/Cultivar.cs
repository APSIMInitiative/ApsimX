// -----------------------------------------------------------------------
// <copyright file="Cultivar.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.PMF
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Models.Core;

    /// <summary>
    /// Cultivar class for holding cultivar overrides.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.CultivarView")]
    [PresenterName("UserInterface.Presenters.CultivarPresenter")]
    public class Cultivar : Model
    {
        /// <summary>
        /// The previous value of the properties before applying the commands.
        /// </summary>
        private List<VariableProperty> properties = new List<VariableProperty>();

        /// <summary>
        /// Gets or sets a collection of names this cultivar is known as.
        /// </summary>
        [XmlElement("Alias")]
        public string[] Aliases { get; set; }

        /// <summary>
        /// Gets or sets a collection of commands that must be executed when applying this cultivar.
        /// </summary>
        [XmlElement("Command")]
        public string[] Commands { get; set; }

        /// <summary>
        /// Find a cultivar in a list of cultivars and return it. Will throw if not found
        /// </summary>
        /// <param name="cultivars">The list of cultivars to look through</param>
        /// <param name="cultivarName">The cultivar name to look for</param>
        /// <returns>The found cultivar. Never returns null</returns>
        public static Cultivar Find(List<Cultivar> cultivars, string cultivarName)
        {
            // Look for cultivar and return it.
            foreach (Cultivar cultivar in cultivars)
            {
                if (cultivar.Name.Equals(cultivarName, StringComparison.CurrentCultureIgnoreCase) ||
                    Utility.String.Contains(cultivar.Aliases, cultivarName))
                {
                    return cultivar;
                }
            }

            // If we get this far then we didn't find the cultivar - throw.
            string parentPath = string.Empty;
            if (cultivars.Count > 0)
            {
                parentPath = Utility.String.ParentName(cultivars[0].FullPath);
            }

            throw new ApsimXException(parentPath, "Cannot find a cultivar definition for '" + cultivarName + "'");
        }

        /// <summary>
        /// Apply commands.
        /// </summary>
        /// <param name="model">The underlying model to apply the commands to</param>
        public void Apply(Model model)
        {
            if (this.Commands != null)
            {
                foreach (string command in this.Commands)
                {
                    string propertyName = command;
                    string propertyValue = Utility.String.SplitOffAfterDelimiter(ref propertyName, "=");

                    propertyName = propertyName.TrimEnd();
                    propertyValue = propertyValue.TrimEnd();

                    if (propertyName != string.Empty && propertyValue != string.Empty)
                    {
                        VariableProperty property = model.GetVariableObject(propertyName) as VariableProperty;
                        if (property != null)
                        {
                            property.ValueWithArrayHandling = propertyValue;
                            this.properties.Add(property);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Simulation is now completed. Make sure that we undo any commands. i.e. reset
        /// back to default state.
        /// </summary>
        public override void OnSimulationCompleted()
        {
            base.OnSimulationCompleted();
            this.Unapply();
        }

        /// <summary>
        /// Undo the cultivar commands. i.e. put the model back into its original state
        /// </summary>
        public void Unapply()
        {
            foreach (VariableProperty property in this.properties)
            {
                property.Undo();
            }

            this.properties.Clear();
        }
    }
}
