using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;  //enumerator
using Newtonsoft.Json;
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
        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            foreach (HumanFoodStoreType childModel in this.FindAllChildren<HumanFoodStoreType>())
            {
                childModel.TransactionOccurred += Resource_TransactionOccurred;
            }
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            foreach (HumanFoodStoreType childModel in this.FindAllChildren<HumanFoodStoreType>())
            {
                childModel.TransactionOccurred -= Resource_TransactionOccurred;
            }
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

        /// <summary>
        /// Add all events when a new child is added to this resource in run time
        /// </summary>
        /// <param name="child"></param>
        public override void AddNewResourceType(IResourceWithTransactionType child)
        {
            (child as HumanFoodStoreType).Pools.Clear();
            child.TransactionOccurred += Resource_TransactionOccurred;
            this.Children.Add(child as Model);
        }

        #endregion

        #region descriptive summary
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

        #endregion
    }

}
