using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for emission type
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(GreenhouseGases))]
    [Description("This resource represents a greenhouse gas (e.g. CO2)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Greenhouse gases/GreenhouseGasType.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class GreenhouseGasesType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType
    {
        /// <summary>
        /// Unit type
        /// </summary>
        [JsonIgnore]
        [Description("Units (nominal)")]
        public string Units { get { return "kg"; } }

        /// <summary>
        /// Auto collect emissions
        /// </summary>
        [Description("Auto collect")]
        [Required]
        public GreenhouseGasTypes AutoCollectType { get; set; }

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
            {
                AddToResource(StartingAmount, null, null, "Starting value");
            }
        }
    }
}
