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
    /// Returns the Photoperiod modifier for reducing Thermal Time Accumulation.
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PhotoPeriodEffect : Model, IFunction
    {
        /// <summary>The photoperiod</summary>
        [Link]
        IFunction Photoperiod = null;

        /// <summary>The Photoperiod sensitivity factor</summary>
        [Link]
        IFunction PhotoSens = null;
  
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        [Units("0-1")]
        public double Value
        {
            get
            {
                double photop_eff = 1.0;

                double photop_sen_factor = PhotoSens.Value * 0.002;
                photop_eff = 1.0 - photop_sen_factor * Math.Pow(20.0 - Photoperiod.Value, 2);
                photop_eff = Math.Max(photop_eff, 0.0);
                photop_eff = Math.Min(photop_eff, 1.0);

                return photop_eff;
            }
        }
    }
}
