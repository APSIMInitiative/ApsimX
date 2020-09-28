using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;  //enumerator
using Newtonsoft.Json;
using System.Runtime.Serialization;
using Models.Core;
using Models.CLEM.Reporting;
using Models.Core.Attributes;

namespace Models.CLEM.Resources
{

    ///<summary>
    /// Store for all the biomass growing in the fields (pasture, crop residue etc)
    /// This acts like an AnimalFoodStore but in reality the food is in a field
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("This resource group holds all graze food store types (pastures) for the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Graze food store/GrazeFoodStore.htm")]
    public class GrazeFoodStore: ResourceBaseWithTransactions
    {
        /// <summary>
        /// Current state of this resource.
        /// </summary>
        [JsonIgnore]
        public List<GrazeFoodStoreType> Items;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Items = new List<GrazeFoodStoreType>();

            IEnumerable<IModel> childNodes = FindAllChildren<IModel>();

            foreach (IModel childModel in childNodes)
            {
                switch (childModel.GetType().ToString())
                {
                    case "Models.CLEM.Resources.GrazeFoodStoreType":
                        GrazeFoodStoreType grazefood = childModel as GrazeFoodStoreType;
                        grazefood.TransactionOccurred += Resource_TransactionOccurred;
                        grazefood.EcologicalIndicatorsCalculated += Resource_EcologicalIndicatorsCalculated;
                        Items.Add(grazefood);
                        break;
                    case "Models.CLEM.Resources.CommonLandFoodStoreType":
                        CommonLandFoodStoreType commonfood = childModel as CommonLandFoodStoreType;
                        commonfood.TransactionOccurred += Resource_TransactionOccurred;
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
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            foreach (GrazeFoodStoreType childModel in this.FindAllChildren<GrazeFoodStoreType>())
            {
                childModel.TransactionOccurred -= Resource_TransactionOccurred;
                childModel.EcologicalIndicatorsCalculated -= Resource_EcologicalIndicatorsCalculated;
            }
            foreach (CommonLandFoodStoreType childModel in this.FindAllChildren<CommonLandFoodStoreType>())
            {
                childModel.TransactionOccurred -= Resource_TransactionOccurred;
            }
            if (Items != null)
            {
                Items.Clear();
            }
            Items = null;
        }

        #region Ecological Indicators calculated

        private void Resource_EcologicalIndicatorsCalculated(object sender, EventArgs e)
        {
            LastEcologicalIndicators = (e as EcolIndicatorsEventArgs).Indicators;
            OnEcologicalIndicatorsCalculated(e);
        }

        /// <summary>
        /// Override base event
        /// </summary>
        protected void OnEcologicalIndicatorsCalculated(EventArgs e)
        {
            EcologicalIndicatorsCalculated?.Invoke(this, e);
        }

        /// <summary>
        /// Override base event
        /// </summary>
        public event EventHandler EcologicalIndicatorsCalculated;

        /// <summary>
        /// Last ecological indicators received
        /// </summary>
        [JsonIgnore]
        public EcologicalIndicators LastEcologicalIndicators { get; set; }

        #endregion

        #region Transactions

        // Must be included away from base class so that APSIM Event.Subscriber can find them 

        /// <summary>
        /// Override base event
        /// </summary>
        protected new void OnTransactionOccurred(EventArgs e)
        {
            TransactionOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Override base event
        /// </summary>
        public new event EventHandler TransactionOccurred;

        private void Resource_TransactionOccurred(object sender, EventArgs e)
        {
            LastTransaction = (e as TransactionEventArgs).Transaction;
            OnTransactionOccurred(e);
        }

        #endregion

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            return html;
        }

    }


}
