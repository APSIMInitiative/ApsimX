using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Management
{
    /// <summary>
    /// A crop cost / price 
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Manager))]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class FarmCropMgtInfo : Model
    {
        /// <summary>.</summary>
        [Description("The real (PMF) name that this crop represents")]
        public string RealName { get; set; }
        /// <summary>.</summary>
        [Description("")]
        public int RainDays { get; set; }
        /// <summary>.</summary>
        [Description("")]
           public string StartDate{ get; set; }
        /// <summary>.</summary>
        [Description("")]
           public string EndDate{ get; set; }
        /// <summary>.</summary>
        [Description("")]
           public double MinRain{ get; set; }
        /// <summary>.</summary>
        [Description("")]
           public double MinESW{ get; set; }
        /// <summary>.</summary>
        [Description("")]
           public bool MustSow{ get; set; }
        /// <summary>.</summary>
        [Description("")]
           public double Population{ get; set; }
        /// <summary>.</summary>
        [Description("")]
           public string CultivarName{ get; set; }
        /// <summary>.</summary>
        [Description("")]
           public double SowingDepth{ get; set; }
        /// <summary>.</summary>
        [Description("")]
           public double RowSpacing{ get; set; }
        /// <summary>.</summary>
        [Description("")]
           public double maxArea{ get; set; }
        /// <summary>.</summary>
        [Description("")]
           public string fertType{ get; set; }
        /// <summary>.</summary>
        [Description("")]
           public double fertAmount{ get; set; }
    }
}