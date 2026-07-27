using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for equipment type
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Equipment))]
    [Description("This resource represents a piece of equipment (e.g. Tractor, bore)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Equipment/Equipmenttype.htm")]
    public class EquipmentType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType
    {
        /// <summary>
        /// Unit type
        /// </summary>
        [Description("Units (nominal)")]
        public string Units { get; set; } = "Items";

        /// <summary>
        /// Starting amount
        /// </summary>
        [Description("Starting amount")]
        [Required, GreaterThanEqualValue(0)]
        public double StartingAmount { get; set; }

        /// <summary>
        /// Service interval
        /// </summary>
        [Description("Servicing interval")]
        [Required, GreaterThanEqualValue(0)]
        public double ServiceInterval { get; set; }

        /// <summary>
        /// Odometer
        /// </summary>
        [JsonIgnore]
        public double Odometer { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            Initialise();
        }

        /// <summary>
        /// Initialise resource type
        /// </summary>
        public void Initialise()
        {
            if (StartingAmount > 0)
            {
                AddToResource(StartingAmount, null, null, "Starting value");
            }
        }
    }
}
