using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Base resource model to implement transaction tracking
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("This is the CLEM Resource Base Class and should not be used directly.")]
    [Version(1, 0, 1, "")]
    public class ResourceBaseWithTransactions: CLEMModel
    {
        /// <summary>
        /// Last transaction received
        /// </summary>
        [XmlIgnore]
        public ResourceTransaction LastTransaction { get; set; }

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
        /// Get resource by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object GetByName(string name)
        {
            return this.Children.Where(a => a.Name == name).FirstOrDefault();
        }

        /// <summary>
        /// Add all events when a new child is added to this resource in run time
        /// </summary>
        /// <param name="child"></param>
        public void AddChildEvents(IResourceWithTransactionType child)
        {
            child.TransactionOccurred += Resource_TransactionOccurred;
        }

        private void Resource_TransactionOccurred(object sender, EventArgs e)
        {
            LastTransaction = (e as TransactionEventArgs).Transaction;
            OnTransactionOccurred(e);
        }
    }
}
