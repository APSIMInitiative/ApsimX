using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the initialisation parameters for a Cohort of a specific Other Animal Type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(OtherAnimalsType))]
    [Description("Specifies an other animal cohort at the start of the simulation")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Other animals/OtherAnimalsTypeCohort.htm")]
    public class OtherAnimalsTypeCohort : CLEMModel, IFilterable
    {
        /// <summary>
        /// Sex
        /// </summary>
        [Description("Sex")]
        [Required]
        public Sex Sex { get; set; }

        /// <summary>
        /// Age (Months)
        /// </summary>
        [Description("Age (months)")]
        [Units("Months")]
        [Required, GreaterThanEqualValue(0)]
        public int Age { get; set; }

        /// <summary>
        /// Starting Number
        /// </summary>
        [Description("Number")]
        [Required, GreaterThanEqualValue(0)]
        public int Number { get; set; }

        /// <summary>
        /// Starting Weight
        /// </summary>
        public double Weight { get; set; }

        ///// <summary>
        ///// Standard deviation of starting weight. Use 0 to use starting weight only
        ///// </summary>
        //[Description("Standard deviation of starting weight")]
        //[Required]
        //public double StartingWeightSD { get; set; }

        /// <summary>
        /// Flag to identify individual ready for sale
        /// </summary>
        public HerdChangeReason SaleFlag { get; set; }
    }
}
