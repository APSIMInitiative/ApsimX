namespace Models.PMF
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Models.Core;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// # [Name]
    /// Class for holding parameter overrides that are used to define a cultivar.
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
    public class Cultivar : Model, ICustomDocumentation
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
        public string[] Alias { get => FindAllChildren<Alias>().Select(a => a.Name).ToArray(); }

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
            if (string.Equals(Name, name, StringComparison.InvariantCultureIgnoreCase))
                return true;
            
            return Alias.Any(a => string.Equals(a, name, StringComparison.InvariantCultureIgnoreCase));
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

        /// <summary>
        /// Writes documentation for this function by adding to the list of documentation tags.
        /// </summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));
                tags.Add(new AutoDocumentation.Paragraph("This cultivar is defined by overriding some of the base parameters of the plant model.", indent));
                tags.Add(new AutoDocumentation.Paragraph(Name + " makes the following changes:", indent));
                if (Command != null && Command.Length > 0)
                    tags.Add(new AutoDocumentation.Paragraph(Command.Aggregate((a, b) => a + "<br>" + b), indent));
            }
        }
    }
}
