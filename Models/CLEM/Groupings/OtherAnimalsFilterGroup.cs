using Models.Core;
using Models.CLEM.Resources;
using System;
using System.ComponentModel.DataAnnotations;
using Models.Core.Attributes;
using Newtonsoft.Json;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual other animals
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Selects specific individuals from the other animals")]
    [Version(1, 0, 1, "")]
    public class OtherAnimalsFilterGroup : FilterGroup<OtherAnimalsTypeCohort>
    {
        //[Link]
        //private ResourcesHolder resources = null;

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

        ///// <summary>An event handler to allow us to perform checks when simulation commences</summary>
        ///// <param name="sender">The sender.</param>
        ///// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        //[EventSubscribe("Commencing")]
        //private void OnSimulationCommencing(object sender, EventArgs e)
        //{
        //    SelectedOtherAnimalsType = resources.FindResourceGroup<OtherAnimals>().FindChild(AnimalType) as OtherAnimalsType;
        //    if (SelectedOtherAnimalsType == null)
        //    {
        //        throw new Exception("Unknown other animal type: " + AnimalType + " in OtherAnimalsActivityFeed : " + this.Name);
        //    }
        //}

    }
}
