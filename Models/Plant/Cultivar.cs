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
    using APSIM.Shared.Utilities;

    /// <summary>
    /// Cultivar class for holding cultivar overrides.
    /// </summary>
    /// <remarks>
    /// A cultivar includes \p Aliases to indicate other common names
    /// and \p Commands to specify genotypic parameters.
    /// The format of \p Commands is "name=value". The "name" of parameter
    /// should include the full path under Plant function,
    /// e.g. [Phenology].Vernalisation.PhotopSens = 3.5.
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.CultivarView")]
    [PresenterName("UserInterface.Presenters.CultivarPresenter")]
    public class Cultivar : Model
    {
        /// <summary>
        /// The properties for each command
        /// </summary>
        private List<IVariable> properties = new List<IVariable>();

        /// <summary>
        /// The original property values before the command was applied. Allows undo.
        /// </summary>
        private List<object> oldPropertyValues = new List<object>();

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
                    (cultivar.Aliases != null && StringUtilities.Contains(cultivar.Aliases, cultivarName)))
                {
                    return cultivar;
                }
            }

            // If we get this far then we didn't find the cultivar - throw.
            throw new ApsimXException(cultivars[0].Parent, "Cannot find a cultivar definition for '" + cultivarName + "'");
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
                    string propertyValue = StringUtilities.SplitOffAfterDelimiter(ref propertyName, "=");

                    propertyName = propertyName.TrimEnd();
                    propertyValue = propertyValue.TrimEnd();

                    if (propertyName != string.Empty && propertyValue != string.Empty)
                    {
                        IVariable property = Apsim.GetVariableObject(model, propertyName) as IVariable;
                        if (property == null)
                            throw new Exception("Cannot find cultivar property: " + propertyName);
                        if (property.GetType() != null)
                        {
                            object oldValue = property.Value;
                            if (oldValue is string || oldValue.GetType().IsArray || !oldValue.GetType().IsClass)
                            {
                                this.oldPropertyValues.Add(oldValue);
                                property.Value = propertyValue;
                                this.properties.Add(property);
                            }
                            else
                                throw new ApsimXException(this, "Invalid type for setting cultivar parameter: " + propertyName +
                                                                ". Must be a built-in type e.g. double");
                        }
                        else
                        {
                            throw new ApsimXException(this, "While applying cultivar '" + Name + "', could not find property name '" + propertyName + "'");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Simulation is now completed. Make sure that we undo any commands. i.e. reset
        /// back to default state.
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            this.Unapply();
        }

        /// <summary>
        /// Undo the cultivar commands. i.e. put the model back into its original state
        /// </summary>
        public void Unapply()
        {
            for (int i = 0; i < this.properties.Count; i++)
            {
                this.properties[i].Value = this.oldPropertyValues[i];
            }

            this.properties.Clear();
            this.oldPropertyValues.Clear();
        }
    }
}
