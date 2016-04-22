using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using System.Xml.Serialization;
using Models.Interfaces;

namespace Models.PMF.Phen
{

    ///<summary>
    /// The vernalization and photoperiod effects from CERES wheat.
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class VernalisationEffect : Model, IFunction
    {
        /// <summary>The amount of vernal days accumulated each day</summary>
        [Link]
        IFunction CumulativeVernalisationDays = null;

        /// <summary>The vernalisation sensitivity factor</summary>
        [Link]
        IFunction VernSens = null;
        
        /// <summary>The Maximum number of Vernal days to accumulate</summary>
        [Link]
        IFunction MaxVernalisationRequirement = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        [Units("0-1")]
        public double Value
        {
            get
            {
                double vfac;   
                double vern_sens_fac;
                double vern_effect = 1.0;

                vern_sens_fac = VernSens.Value * 0.0054545 + 0.0003;
                vfac = 1.0 - vern_sens_fac * (MaxVernalisationRequirement.Value - CumulativeVernalisationDays.Value);
                vern_effect = Math.Max(vfac, 0.0);
                vern_effect = Math.Min(vern_effect, 1.0);

                return vern_effect;
            }
        }
    }
}
