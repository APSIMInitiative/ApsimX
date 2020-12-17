using System;
using System.Collections.Generic;
using System.Linq;
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
        [Display(Type = DisplayType.DropDown, Values = nameof(FindPlantNames))]
        public string CropName { get; set; }

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
    
        /// <summary>
        /// Find the names of all plants in scope.
        /// </summary>
        /// <remarks>
        /// Used to find valid values for <see cref="CropName" />.
        /// </remarks>
        private IEnumerable<string> FindPlantNames()
        {
            return FindAllInScope<Plant>().Select(p => p.Name);
        }
}
}