namespace Models.PMF
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// # [Name] 
    /// [Name] overrides the following properties:
    /// 
    /// [Command]
    /// </summary>
    /// <remarks>
    /// A cultivar includes \p Aliases to indicate other common names
    /// and \p Commands to specify genotypic parameters.
    /// The format of \p Commands is "name=value". The "name" of parameter
    /// should include the full path under Plant function,
    /// e.g. [Phenology].Vernalisation.PhotopSens = 3.5.
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.EditorView")]
    [PresenterName("UserInterface.Presenters.CultivarPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    [ValidParent(ParentType = typeof(GrazPlan.Stock))]
    [ValidParent(ParentType = typeof(CultivarFolder))]
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
        /// Gets or sets a collection of commands that must be executed when applying this cultivar.
        /// </summary>
        public string[] Command { get; set; }

        /// <summary>
        /// Return true iff this cultivar has the same name as, or is an
        /// alias for, the givem name.
        /// </summary>
        /// <param name="name">The name.</param>
        public bool IsKnownAs(string name)
        {
            return GetNames().Any(a => string.Equals(a, name, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Return all names by which this cultivar is known.
        /// </summary>
        public IEnumerable<string> GetNames()
        {
            yield return Name;
            foreach (string name in FindAllChildren<Alias>().Select(a => a.Name))
                yield return name;
        }

        /// <summary>
        /// Apply commands.
        /// </summary>
        /// <param name="model">The underlying model to apply the commands to</param>
        public void Apply(Model model)
        {
            if (this.Command != null)
            {
                foreach (string command in this.Command)
                {
                    try
                    {
                        string propertyName = command;
                        string propertyValue = StringUtilities.SplitOffAfterDelimiter(ref propertyName, "=");

                        propertyName = propertyName.TrimEnd();
                        propertyValue = propertyValue.TrimEnd();

                        if (propertyName != string.Empty && propertyValue != string.Empty)
                        {
                            IVariable property = model.FindByPath(propertyName) as IVariable;
                            if (property == null)
                                throw new Exception(string.Format("Invalid command in cultivar {0}: {1}", Name, propertyName));
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
                    catch (Exception err)
                    {
                        throw new Exception($"Error in cultivar {Name}: Unable to apply command '{command}'", err);
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
