using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using APSIM.Shared.Utilities;
using Models.Interfaces;

namespace Models.PMF.Functions.SupplyFunctions
{
    /// <summary>
    /// Biomass accumulation is the product of the amount of intercepted radiation and its 
    /// conversion efficiency, the radiation use efficiency (RUE) [Monteith1977].  
    /// This approach simulates net photosynthesis rather than providing separate estimates 
    /// of growth and respiration.  RUE is calculated from a potential value which is discounted 
    /// using stress factors that account for plant nutrition (Fn), air temperature(Ft), vapour pressure deficit (Fvpd), water supply (Fw) and atmospheric CO2 concentration (Fco2).
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ILeaf))]
    public class CanopyPhotosynthesis : Model, IFunction
    {
        /// <summary>
        /// The amount of DM that is fixed by photosynthesis
        /// </summary>
        public double GrossPhotosynthesis { get; set; }

        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            GrossPhotosynthesis = 10;
        }

        /// <summary>Daily growth increment of total plant biomass</summary>
        /// <returns>g dry matter/m2 soil/day</returns>
        public double Value
        {
            get
            {
                return GrossPhotosynthesis;
            }
        }
    }
}