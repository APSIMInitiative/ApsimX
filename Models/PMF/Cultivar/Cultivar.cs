﻿using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using Models.Core;
using Newtonsoft.Json;

namespace Models.PMF
{
    /// <summary>
    /// A cultivar model - used to override properties of another model (typically a plant) at runtime.
    /// </summary>
    /// <remarks>
    /// This includes aliases to indicate other common crop names and commands to specify genotype parameters.
    /// The format of commands is "name=value". The "name" of parameter should include the full path under
    /// Plant function, e.g. [Phenology].Vernalisation.PhotopSens = 3.5.
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.EditorView")]
    [PresenterName("UserInterface.Presenters.EditorPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    [ValidParent(ParentType = typeof(GrazPlan.Stock))]
    [ValidParent(ParentType = typeof(Folder))]
    [ValidParent(ParentType = typeof(ModelOverrides))]
    [ValidParent(ParentType = typeof(Sugarcane))]
    [ValidParent(ParentType = typeof(OilPalm.OilPalm))]
    [ValidParent(ParentType = typeof(AgPasture.PastureSpecies))]
    public class Cultivar : Model, ILineEditor, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        /// <summary>Default constructor.</summary>
        /// <remarks>This is needed for AddModel to work.</remarks>
        public Cultivar()
        {
        }

        /// <summary>
        /// Constructor to initialise cultivar instance with specified commands
        /// </summary>
        /// <param name="commands">list of parameter overwrite commands</param>
        /// <param name="name">The name of the cultivar</param>
        public Cultivar (string name, string[] commands)
        {
            this.Name = name;
            Command = commands;
        }

        /// <summary>The model the cultivar is relative to.</summary>
        private INodeModel relativeToModel;

        /// <summary>The collection of commands.</summary>
        private IEnumerable<IModelCommand> commands;

        /// <summary>The collection of commands that must be executed when applying this cultivar.</summary>
        public string[] Command { get; set; }

        /// <summary>The lines to return to the editor.</summary>
        [JsonIgnore]
        public IEnumerable<string> Lines { get { return Command; } set { Command = value.ToArray(); } }

        /// <summary>
        /// Return true if this cultivar has the same name as, or is an alias for, the given name.
        /// </summary>
        /// <param name="name">The name.</param>
        public bool IsKnownAs(string name)
        {
            return GetNames().Any(a => string.Equals(a, name, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Return all names by which this cultivar is known.
        /// </summary>
        public List<string> GetNames()
        {
            List<string> names = new List<string>();
            names.Add(Name);
            foreach (string name in Structure.FindChildren<Alias>().Select(a => a.Name))
                names.Add(name);

            return names;
        }

        /// <summary>Apply commands.</summary>
        /// <param name="model">The underlying model to apply the commands to</param>
        public void Apply(IModel model)
        {
            if (Command == null)
                return;

            relativeToModel = model as INodeModel;

            commands = CommandLanguage.StringToCommands(Command, relativeToModel, relativeToDirectory: null);
            CommandProcessor.Run(commands, relativeToModel, runner: null);
        }

        /// <summary>Undoes cultivar changes, if any.</summary>
        public void Unapply()
        {
            if (commands != null)
            {
                foreach (SetPropertyCommand command in commands)
                    command.Undo();
                commands = null;
            }
        }

        /// <summary>
        /// Simulation is now completed. Make sure that we undo any commands. i.e. reset
        /// back to default state.
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            Unapply();
        }
    }
}
