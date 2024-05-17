using Models.CLEM.Reporting;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for all the biomass growing in the fields (pasture, crop residue etc)
    /// This acts like an AnimalFoodStore but in reality the food is in a field
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("Resource group for all graze food store types (pastures) in the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Graze food store/GrazeFoodStore.htm")]
    public class GrazeFoodStore : ResourceBaseWithTransactions
    {
        /// <summary>
        /// Current state of this resource.
        /// </summary>
        [JsonIgnore]
        public List<GrazeFoodStoreType> Items;

        /// <summary>
        /// Ecological indicators calculated event
        /// </summary>
        public event EventHandler EcologicalIndicatorsCalculated;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private new void OnSimulationCommencing(object sender, EventArgs e)
        {
            Items = new List<GrazeFoodStoreType>();

            foreach (IModel childModel in FindAllChildren<IModel>())
            {
                switch (childModel.GetType().ToString())
                {
                    case "Models.CLEM.Resources.GrazeFoodStoreType":
                        GrazeFoodStoreType grazefood = childModel as GrazeFoodStoreType;
                        grazefood.EcologicalIndicatorsCalculated += Resource_EcologicalIndicatorsCalculated;
                        Items.Add(grazefood);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private new void OnSimulationCompleted(object sender, EventArgs e)
        {
            foreach (GrazeFoodStoreType childModel in this.FindAllChildren<GrazeFoodStoreType>())
                childModel.EcologicalIndicatorsCalculated -= Resource_EcologicalIndicatorsCalculated;

            Items?.Clear();
            Items = null;
        }

        #region Ecological Indicators calculated

        private void Resource_EcologicalIndicatorsCalculated(object sender, EventArgs e)
        {
            LastEcologicalIndicators = (e as EcolIndicatorsEventArgs).Indicators;
            OnEcologicalIndicatorsCalculated(e);
        }

        /// <summary>
        /// On ecological indicators calculated event
        /// </summary>
        protected void OnEcologicalIndicatorsCalculated(EventArgs e)
        {
            EcologicalIndicatorsCalculated?.Invoke(this, e);
        }

        /// <summary>
        /// Last ecological indicators received
        /// </summary>
        [JsonIgnore]
        public EcologicalIndicators LastEcologicalIndicators { get; set; }

        #endregion
    }
}
