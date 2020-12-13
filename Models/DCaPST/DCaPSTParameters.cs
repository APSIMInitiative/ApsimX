using System;
using Models.Core;
using Models.PMF;

namespace Models.DCAPST
{
    /// <summary>
    /// Encapsulates all parameters passed to DCaPST.
    /// </summary>
    [ValidParent(typeof(DCaPSTModelNG))]
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class DCaPSTParameters : Model
    {
        /// <summary>
        /// The crop against which DCaPST will be run.
        /// </summary>
        [Description("The crop")]
        public Plant Crop { get; set; }

        /// <summary>
        /// PAR energy fraction
        /// </summary>
        [Description("PAR energy fraction")]
        public double Rpar { get; set; }

        /// <summary>
        /// Canopy parameters.
        /// </summary>
        [Description("Canopy Parameters")]
        [Display(Type = DisplayType.SubModel)]
        public CanopyParameters Canopy { get; set; } = new CanopyParameters();

        /// <summary>
        /// Pathway parameters.
        /// </summary>
        [Description("Pathway Parameters")]
        [Display(Type = DisplayType.SubModel)]
        public PathwayParameters Pathway { get; set; } = new PathwayParameters();
    }
}