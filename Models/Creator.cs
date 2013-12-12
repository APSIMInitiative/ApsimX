using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models
{
    /// <summary>
    /// This class duplicates (clones) models, allowing user to change properties or entire models
    /// in the duplicated model. Can be used for shortcutting or factorials.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.CreatorPresenter")]
    public class Creator : Model
    {   
        private List<Model> newModels = new List<Model>();

        [Serializable]
        public class Description
        {
            [Serializable]
            public class ActionSpecifier
            {
                public enum ActionEnum { Set };
                public ActionEnum Action { get; set; }
                public string Path { get; set; }
                public object Value { get; set; }
            }

            public string Name { get; set; }
            public string Base { get; set; }
            public List<ActionSpecifier> Actions { get; set; }

            public Description() { Actions = new List<ActionSpecifier>(); }

        }


        public Description[] Descriptions { get; set; }

        /// <summary>
        /// Main method for creating all cloned models.
        /// </summary>
        public Model[] Create()
        {
            newModels.Clear();
            foreach (Description model in Descriptions)
            {
                Simulation baseModel = this.Get(model.Base) as Simulation;
                Simulation newModel = Utility.Reflection.Clone<Simulation>(baseModel);
                newModel.Name = model.Name;
                Utility.ModelFunctions.DisconnectEventsInAllModels(newModel);
                newModels.Add(newModel);
            }

            return newModels.ToArray();
        }

        /// <summary>
        /// All simulations have finished running - get rid of any that we created.
        /// </summary>
        [EventSubscribe("AllCompleted")]
        private void AllCompleted(object sender, EventArgs e)
        {
            foreach (Model model in newModels)
                model.Parent.RemoveModel(model);
        }



    }
}
