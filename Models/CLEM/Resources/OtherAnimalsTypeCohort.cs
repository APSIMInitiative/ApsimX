using Microsoft.VisualBasic.FileIO;
using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the initialisation parameters for a Cohort of a specific Other Animal Type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(OtherAnimalsType))]
    [ValidParent(ParentType = typeof(OtherAnimalsActivityBuy))]
    [Description("Specifies an other animal cohort for initialisation or purchase")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Other animals/OtherAnimalsTypeCohort.htm")]
    public class OtherAnimalsTypeCohort : CLEMModel, IFilterable, ICloneable
    {
        /// <summary>
        /// A protected link to the CLEM resource holder
        /// </summary>
        [Link(ByName = true)]
        protected ResourcesHolder Resources = null;

        /// <summary>
        /// name of other animal type
        /// </summary>
        [Description("Name of other animal type")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of Other Animal Type required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(OtherAnimals) } }, VisibleCallback = "IsVisible")]
        [FilterByProperty]
        public string AnimalTypeName { get; set; }

        /// <summary>
        /// Unique cohort id
        /// </summary>
        [Required]
        [FilterByProperty]
        public int ID { get; set; }

        /// <summary>
        /// Sex
        /// </summary>
        [Description("Sex")]
        [Required]
        [FilterByProperty]
        public Sex Sex { get; set; }

        /// <summary>
        /// Age (Months)
        /// </summary>
        [Description("Age (months)")]
        [Units("Months")]
        [Required, GreaterThanEqualValue(0)]
        [FilterByProperty]
        public int Age { get; set; }

        /// <summary>
        /// Starting Number
        /// </summary>
        [Description("Number")]
        [Required, GreaterThanEqualValue(0)]
        [FilterByProperty]
        public int Number { get; set; }

        /// <summary>
        /// Starting Weight
        /// </summary>
        [FilterByProperty]
        public double Weight { get; set; }

        /// <summary>
        /// Starting Number
        /// </summary>
        [JsonIgnore]
        public int AdjustedNumber { get; set; }

        /// <summary>
        /// Flag to identify individual ready for sale
        /// </summary>
        [JsonIgnore]
        public HerdChangeReason SaleFlag { get; set; }

        /// <summary>
        /// The other animal type associated with this cohort
        /// </summary>
        [JsonIgnore]
        public OtherAnimalsType AnimalType { get; set; }

        /// <summary>
        /// Current animal price group for this individual 
        /// </summary>
        [JsonIgnore]
        public (AnimalPriceGroup Buy, AnimalPriceGroup Sell) CurrentPriceGroups { get; set; } = (null, null);

        /// <summary>
        /// Flag to identify cohorts that have already been considered in this time step
        /// </summary>
        [JsonIgnore]
        public bool Considered { get; set; } = false;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            if (Parent is OtherAnimalsType otherAnimal)
            {
                AnimalTypeName = otherAnimal.NameWithParent;
                AnimalType = otherAnimal;
            }
            else
                AnimalType = Resources.FindResourceType<OtherAnimals, OtherAnimalsType>(this, AnimalTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
        }

        /// <summary>A method to arrange clearing the activity status on CLEMStartOfTimeStep event.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        protected virtual void ResetAdjustedNumber(object sender, EventArgs e)
        {
            AdjustedNumber = Number;
        }

        /// <summary>
        /// Determine if the other animal type dropdown should be displayed.
        /// </summary>
        /// <returns>True if the parent is not an OtherAnimalsType</returns>
        public bool IsVisible()
        {
            return (Parent is OtherAnimalsType) == false;
        }

        #region ICloneable Members

        /// <summary>
        /// Clone the current object
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion

    }
}
