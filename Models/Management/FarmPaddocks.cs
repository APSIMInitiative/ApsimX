using System;
using Models.Core;

namespace Models.Management
{
    /// <summary>
    /// A crop cost / price 
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Manager))]
    [ViewName("UserInterface.Views.PropertyAndGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndGridPresenter")]
    public class FarmPaddocks : Model
    {
        /// <summary> </summary>
        [Display(DisplayName = "Paddock")]
        public string[] PaddockNames {get; set;}

        /// <summary> </summary>
        [Display(DisplayName = "Managed? (X = yes)")]
        public bool[] IsManaged {get; set;}

        /// <summary> </summary>
        [Display(DisplayName = "Initial State")]
        public string[] InitialState {get; set;}

        /// <summary> </summary>
        [Display(DisplayName = "Days since harvest (d)")]
        public int[] DaysSinceHarvest {get; set;}
    }
}
