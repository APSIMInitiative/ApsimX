using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;  //enumerator
using System.Xml.Serialization;
using System.Runtime.Serialization;
using Models.Core;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for all the food designated for animals to eat (eg. Forages and Supplements)
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("This resource group holds all animal food store types for the simulation.")]
    public class AnimalFoodStore: ResourceBaseWithTransactions
    {
        /// <summary>
        /// List of all the Food Types in this Resource Group.
        /// </summary>
        [XmlIgnore]
        public List<AnimalFoodStoreType> Items;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Items = new List<AnimalFoodStoreType>();
            foreach (AnimalFoodStoreType childModel in Apsim.Children(this, typeof(AnimalFoodStoreType)))
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
            foreach (AnimalFoodStoreType childModel in Apsim.Children(this, typeof(AnimalFoodStoreType)))
            {
                childModel.TransactionOccurred -= Resource_TransactionOccurred;
            }
            if(Items!=null)
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
            EventHandler invoker = TransactionOccurred;
            if (invoker != null) invoker(this, e);
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

    }


}
