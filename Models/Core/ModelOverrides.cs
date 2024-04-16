using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Models.Core
{

    /// <summary>
    /// When dropped on a model, this model will apply a set of overides to its parent model.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.EditorView")]
    [PresenterName("UserInterface.Presenters.EditorPresenter")]
    [ValidParent(ParentType = typeof(IPlant))]
    public class ModelOverrides : Model, ILineEditor
    {
        /// <summary>The collection of undo overrides that undo the overrides.</summary>
        private IEnumerable<Overrides.Override> undos;

        /// <summary>The collection of property overrides./// </summary>
        public string[] Overrides { get; set; }

        /// <summary>The lines to return to the editor./// </summary>
        [JsonIgnore]
        public IEnumerable<string> Lines { get { return Overrides; } set { Overrides = value.ToArray(); } }


        /// <summary>
        /// Simulation is now completed. Make sure that we undo any commands. i.e. reset
        /// back to default state.
        /// </summary>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            undos = Core.Overrides.Apply(Parent, Core.Overrides.ParseStrings(Overrides));
        }

        /// <summary>
        /// Simulation is now completed. Make sure that we undo any commands. i.e. reset
        /// back to default state.
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (undos != null)
            {
                Core.Overrides.Apply(Parent, undos);
                undos = null;
            }
        }
    }
}
