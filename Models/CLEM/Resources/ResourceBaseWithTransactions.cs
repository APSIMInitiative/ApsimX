using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
    public class ResourceBaseWithTransactions: CLEMModel
    {
        /// <summary>
        /// Last transaction received
        /// </summary>
        [JsonIgnore]
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
        /// Add all events when a new child is added to this resource in run time
        /// </summary>
        /// <param name="child"></param>
        public virtual void AddNewResourceType(IResourceWithTransactionType child)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Resource_TransactionOccurred(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Provie full name of resource StoreName.TypeName
        /// </summary>
        public string FullName => $"{CLEMParentName}.{Name}";
    }
}
