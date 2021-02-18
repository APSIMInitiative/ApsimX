using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Parent model of products stores.
    /// e.g. manure, cotton, wool
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("This resource group holds all products store types (e.g. wool, timber, manure) for the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Products/ProductStore.htm")]
    public class ProductStore : ResourceBaseWithTransactions
    {
        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            foreach (var child in Children)
            {
                if (child is IResourceWithTransactionType)
                {
                    (child as IResourceWithTransactionType).TransactionOccurred += Resource_TransactionOccurred; ;
                }
            }
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            foreach (IResourceWithTransactionType childModel in this.FindAllChildren<IResourceWithTransactionType>())
            {
                childModel.TransactionOccurred -= Resource_TransactionOccurred;
            }
        }

        /// <summary>
        /// Add all events when a new child is added to this resource in run time
        /// </summary>
        /// <param name="child"></param>
        public override void AddNewResourceType(IResourceWithTransactionType child)
        {
            child.TransactionOccurred += Resource_TransactionOccurred;
            this.Children.Add(child as Model);
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
