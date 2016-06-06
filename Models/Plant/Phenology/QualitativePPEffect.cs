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
    /// Qualitative Photoperiod effect on developmental rate
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(GenericPhase))]
    public class QualitativePPEffect : Model
    {
        /// <summary>The photoperiod</summary>
        [Link]
        IFunction Photoperiod = null;

        /// <summary>Gets or sets the photop eff.</summary>
        /// <value>The photop eff.</value>
        [XmlIgnore]
        public double PhotoperiodEffect { get; set; }
        /// <summary>Gets or sets the Optimum Photoperiod</summary>
        /// <value>Optimum Photoperiod</value>
        [Description("Optimum Photoperiod for development")]
        public double OptimumPhotoperiod { get; set; }
        /// <summary>Gets or sets the Critical Photoperiod</summary>
        /// <value>Critical Photoperiod</value>
        [Description("Critical Photoperiod for development")]
        public double CriticalPhotoperiod { get; set; }

        /// <summary>Trap the DoDailyInitialisation event.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            PhotoperiodEffect = Effect(Photoperiod.Value, OptimumPhotoperiod);
        }

        /// <summary>Initialise everything</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            PhotoperiodEffect = 1;
        }


        /// <summary>Photoperiod factor</summary>
        /// <param name="Photoperiod">The photoperiod.</param>
        /// <param name="photop_sens">The photop_sens.</param>
        /// <returns></returns>
        private double Effect(double Photoperiod, double photop_sens)
        {
            double photop_eff = 1.0;

            double PS = Math.Pow(OptimumPhotoperiod - CriticalPhotoperiod, -2);
            if (Photoperiod < OptimumPhotoperiod)
                photop_eff = 1 - PS * Math.Pow(OptimumPhotoperiod - Photoperiod, 2);
            else
                photop_eff = 1.0;

            photop_eff = Math.Max(photop_eff, 0.0);
            photop_eff = Math.Min(photop_eff, 1.0);

            return photop_eff;
        }


    }
}
