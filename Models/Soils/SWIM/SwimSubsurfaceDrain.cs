using System;
using Models.Core;

namespace Models.Soils
{
    /// <summary>
    /// SWIM sub surface drain model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Swim3))]
    public class SwimSubsurfaceDrain : Model
    {
        /// <summary>Gets or sets the drain depth.</summary>
        /// <value>The drain depth.</value>
        [Description("Depth of subsurface drain (mm)")]
        [Bounds(Lower = 1.0, Upper = 1.0e6)]
        [Units("mm")]
        public double DrainDepth { get; set; } = double.NaN;

        /// <summary>Gets or sets the drain spacing.</summary>
        /// <value>The drain spacing.</value>
        [Description("Distance between subsurface drains (mm)")]
        [Bounds(Lower = 1.0, Upper = 1.0e5)]
        [Units("mm")]
        public double DrainSpacing { get; set; } = double.NaN;

        /// <summary>Gets or sets the drain radius.</summary>
        /// <value>The drain radius.</value>
        [Description("Radius of each subsurface drain (mm)")]
        [Bounds(Lower = 1.0, Upper = 1000.0)]
        [Units("mm")]
        public double DrainRadius { get; set; } = double.NaN;

        /// <summary>Gets or sets the klat.</summary>
        /// <value>The klat.</value>
        [Description("Lateral saturated soil water conductivity (mm/d)")]
        [Bounds(Lower = 1.0, Upper = 10000.0)]
        [Units("mm/d")]
        public double Klat { get; set; } = double.NaN;

        /// <summary>Gets or sets the imperm depth.</summary>
        /// <value>The imperm depth.</value>
        [Description("Depth to impermeable soil (mm)")]
        [Bounds(Lower = 1.0, Upper = 1.0e6)]
        [Units("mm")]
        public double ImpermDepth { get; set; } = double.NaN;

        /// <summary>Gets or sets whether or not the drain is open.</summary>
        /// <value>Open or not.</value>
        [Description("Does the drain start open?")]
        public bool Open { get; set; } = true;
    }
}
