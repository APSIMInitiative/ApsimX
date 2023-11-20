﻿using Models.CLEM.Interfaces;
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
        /// Initial age
        /// </summary>
        [Description("Age")]
        [Core.Display(SubstituteSubPropertyName = "AgeParts")]
        [Units("years, months, days")]
        [Required, ArrayItemCount(1, 3)]
        public AgeSpecifier AgeDetails { get; set; }

        /// <summary>
        /// Current age
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// Starting Number
        /// </summary>
        [Description("Number")]
        [Required, GreaterThanEqualValue(0)]
        public double Number { get; set; }

        /// <summary>
        /// Starting Weight
        /// </summary>
        [Description("Weight (kg)")]
        [Units("kg")]
        [Required, GreaterThanEqualValue(0)]
        public double Weight { get; set; }

        /// <summary>
        /// Standard deviation of starting weight. Use 0 to use starting weight only
        /// </summary>
        [Description("Standard deviation of starting weight")]
        [Required]
        public double StartingWeightSD { get; set; }

        /// <summary>
        /// Flag to identify individual ready for sale
        /// </summary>
        public HerdChangeReason SaleFlag { get; set; }
    }
}
