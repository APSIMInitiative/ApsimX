using Models.Core;
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
            EventHandler handler = TransactionOccurred;
            if (handler != null)
                handler(this, e);
        }

        /// <summary>
        /// Get resource by name
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public object GetByName(string Name)
        {
            return this.Children.Where(a => a.Name == Name).FirstOrDefault();
        }

        /// <summary>
        /// Get main/first account
        /// </summary>
        /// <returns></returns>
        public object GetFirst()
        {
            if (this.Children.Count() > 0)
            {
                return this.Children.FirstOrDefault();
            }
            return null;
        }

    }
}
