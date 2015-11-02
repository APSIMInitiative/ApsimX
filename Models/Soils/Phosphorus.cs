using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.Core;

namespace Models.Soils
{
    /// <summary>
    /// Model for holding information about the phosphorus model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType=typeof(Soil))]
    public class Phosphorus :Model
    {
        /// <summary>Gets or sets the root cp.</summary>
        /// <value>The root cp.</value>
        [Description("Root C:P ratio")]
        public double RootCP { get; set; }
        /// <summary>Gets or sets the rate dissol rock.</summary>
        /// <value>The rate dissol rock.</value>
        [Description("Rate disolved rock")]
        public double RateDissolRock { get; set; }
        /// <summary>Gets or sets the rate loss avail.</summary>
        /// <value>The rate loss avail.</value>
        [Description("Rate loss available")]
        public double RateLossAvail { get; set; }
        /// <summary>Gets or sets the sorption coeff.</summary>
        /// <value>The sorption coeff.</value>
        [Description("Sorption coefficient")]
        public double SorptionCoeff { get; set; }

        /// <summary>Gets or sets the thickness.</summary>
        /// <value>The thickness.</value>
        public double[] Thickness { get; set; }
        /// <summary>Gets or sets the labile p.</summary>
        /// <value>The labile p.</value>
        public double[] LabileP { get; set; }
        /// <summary>Gets or sets the sorption.</summary>
        /// <value>The sorption.</value>
        public double[] Sorption { get; set; }
        /// <summary>Gets or sets the banded p.</summary>
        /// <value>The banded p.</value>
        public double[] BandedP { get; set; }
        /// <summary>Gets or sets the rock p.</summary>
        /// <value>The rock p.</value>
        public double[] RockP { get; set; }
    }


}
