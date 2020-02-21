using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Models.Core.Attributes;
using System.Xml.Serialization;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual other animals
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("This other animal filter group selects specific individuals from the other animals using any number of Other Animal Filters.")]
    [Version(1, 0, 1, "")]
    public class OtherAnimalsFilterGroup: CLEMModel, IFilterGroup
    {
        [Link]
        private ResourcesHolder Resources = null;

        /// <summary>
        /// Combined ML ruleset for LINQ expression tree
        /// </summary>
        [XmlIgnore]
        public object CombinedRules { get; set; } = null;

        /// <summary>
        /// Daily amount to supply selected individuals each month
        /// </summary>
        [Description("Daily amount to supply selected individuals each month")]
        [ArrayItemCount(12)]
        public double[] MonthlyValues { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public OtherAnimalsFilterGroup()
        {
            MonthlyValues = new double[12];
        }

        /// <summary>
        /// name of other animal type
        /// </summary>
        [Description("Name of other animal type")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of Other Animal Type required")]
        public string AnimalType { get; set; }

        /// <summary>
        /// The Other animal type this group points to
        /// </summary>
        public OtherAnimalsType SelectedOtherAnimalsType;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            SelectedOtherAnimalsType = Resources.OtherAnimalsStore().GetByName(AnimalType) as OtherAnimalsType;
            if(SelectedOtherAnimalsType == null)
            {
                throw new Exception("Unknown other animal type: " + AnimalType + " in OtherAnimalsActivityFeed : " + this.Name);
            }
        }

    }
}
