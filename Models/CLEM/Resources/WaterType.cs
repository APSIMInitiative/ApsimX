using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for water type
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(WaterStore))]
    [Description("This resource represents a water store (e.g. dam, bore, tank)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Water/WaterStoreType.htm")]
    public class WaterType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType
    {
        /// <summary>
        /// Unit type
        /// </summary>
        [Description("Units (nominal)")]
        public string Units { get; set; }

        /// <summary>
        /// Starting amount
        /// </summary>
        [Description("Starting amount")]
        [Required]
        public double StartingAmount { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            if (StartingAmount > 0)
                Add(StartingAmount, null, null, "Starting value");
        }
    }
}
