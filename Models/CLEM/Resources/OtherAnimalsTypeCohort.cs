using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the initialisation parameters for a Cohort of a specific Other Animal Type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(OtherAnimalsType))]
    [Description("This specifies an other animal cohort at the start of the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Other animals/OtherAnimalsTypeCohort.htm")]
    public class OtherAnimalsTypeCohort: CLEMModel
    {
        /// <summary>
        /// Gender
        /// </summary>
        [Description("Gender")]
        [Required]
        public Sex Gender { get; set; }

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

        /// <summary>
        /// Gender as string for reports
        /// </summary>
        public string GenderAsString { get { return Gender.ToString().Substring(0, 1); } }

        /// <summary>
        /// SaleFlag as string for reports
        /// </summary>
        public string SaleFlagAsString { get { return SaleFlag.ToString(); } }
    }
}
