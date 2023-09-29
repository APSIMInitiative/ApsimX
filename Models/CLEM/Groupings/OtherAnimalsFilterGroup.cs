using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

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
    }
}
