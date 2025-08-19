using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Base resource model to implement transaction tracking
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("This is the CLEM Resource Base Class and should not be used directly.")]
    [Version(1, 0, 1, "")]
    public class ResourceBaseWithTransactions : CLEMModel
    {
        /// <summary>
        /// Last transaction received
        /// </summary>
        [JsonIgnore]
        public ResourceTransaction LastTransaction { get; set; } = new ResourceTransaction();

        /// <summary>
        /// Provide full name of resource StoreName.TypeName
        /// </summary>
        public string FullName => $"{CLEMParentName}.{Name}";

        /// <summary>
        /// Resource transaction occured Event handler
        /// </summary>
        public event EventHandler TransactionOccurred;

        /// <summary>
        /// Transcation occurred event
        /// </summary>
        /// <param name="e"></param>
        protected void OnTransactionOccurred(EventArgs e)
        {
            TransactionOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Add all events when a new child is added to this resource in run time
        /// </summary>
        /// <param name="child"></param>
        public virtual void AddNewResourceType(IResourceWithTransactionType child)
        {
            throw new NotImplementedException();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            foreach (IResourceWithTransactionType childModel in Structure.FindChildren<IResourceWithTransactionType>())
                childModel.TransactionOccurred += Resource_TransactionOccurred;
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        protected void OnSimulationCompleted(object sender, EventArgs e)
        {
            foreach (IResourceWithTransactionType childModel in Structure.FindChildren<IResourceWithTransactionType>())
                childModel.TransactionOccurred -= Resource_TransactionOccurred;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Resource_TransactionOccurred(object sender, EventArgs e)
        {
            OnTransactionOccurred(e);
        }

        /// <summary>
        /// Handles reporting transactions
        /// </summary>
        public void PerformTransactionOccurred()
        {
            TransactionOccurred?.Invoke(this, null);
        }

    }
}
