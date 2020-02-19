using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;  //enumerator
using System.Xml.Serialization;
using System.Runtime.Serialization;
using Models.Core;
using Models.Core.Attributes;

namespace Models.CLEM.Resources
{

    ///<summary>
    /// Store for all the food designated for Household to eat (eg. Grain, Tree Crops (nuts) etc.)
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("This resource group holds all human food store types for the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Human food store/HumanFoodStore.htm")]
    public class HumanFoodStore: ResourceBaseWithTransactions
    {
        /// <summary>
        /// Current state of this resource.
        /// </summary>
        [XmlIgnore]
        public List<HumanFoodStoreType> Items;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Items = new List<HumanFoodStoreType>();
            foreach (HumanFoodStoreType childModel in Apsim.Children(this, typeof(HumanFoodStoreType)))
            {
                childModel.TransactionOccurred += Resource_TransactionOccurred;
                Items.Add(childModel);
            }
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            foreach (HumanFoodStoreType childModel in Apsim.Children(this, typeof(HumanFoodStoreType)))
            {
                childModel.TransactionOccurred -= Resource_TransactionOccurred;
            }
            if (Items != null)
            {
                Items.Clear();
            }
            Items = null;
        }

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
