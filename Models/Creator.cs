using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

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
        [Link] Simulations Simulations = null;

        [Serializable]
        public class Description
        {
            [Serializable]
            public class ActionSpecifier
            {
                public enum ActionEnum { Set };
                public ActionEnum Action { get; set; }
                public string Path { get; set; }
                [XmlElement(typeof(DateTime))]
                [XmlElement(typeof(double))]
                [XmlElement(typeof(int))]
                [XmlElement(typeof(string))]
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
            // Create all simulations.
            newModels.Clear();
            foreach (Description description in Descriptions)
            {
                Simulation baseModel = this.Get(description.Base) as Simulation;
                Simulation newModel = Utility.Reflection.Clone<Simulation>(baseModel);
                newModel.Name = description.Name;
                newModels.Add(newModel);
            }

            // Setup all simulations.
            for (int i = 0; i < newModels.Count; i++)
            {
                if (newModels[i] is Simulation)
                {
                    Simulations.AddModel(newModels[i]);

                    // We don't want the event connections added by the line above "Simulations.AddModel". Remove
                    // them and then connect all events in all models.
                    Utility.ModelFunctions.DisconnectEventsInAllModels(newModels[i]);
                    Utility.ModelFunctions.ConnectEventsInAllModels(newModels[i]);

                    // Need to initialise now because Manager modules won't have created their Script child
                    // modules yet.
                    (newModels[i] as Simulation).Initialise();
                }

                // Apply all actions.
                foreach (Description.ActionSpecifier action in Descriptions[i].Actions)
                {
                    if (action.Path != null && action.Value != null)
                        newModels[i].Set(action.Path, action.Value);
                }


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
