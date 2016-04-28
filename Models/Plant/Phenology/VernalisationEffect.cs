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
    /// In CERES-Wheat \cite jones_dssat_2003, vernalisation is simulated from daily average crown temperature (\f$T_{c}\f$), daily maximum (\f$T_{max}\f$) and 
    /// minimum (\f$T_{min}\f$) temperatures using the original CERES approach.
    /// \f[
    /// \Delta V=\min(1.4-0.0778T_{c},\:0.5+13.44\frac{T_{c}}{(T_{max}-T_{min}+3)^{2}})\quad\text{when, }T_{max}&lt;30\,{}^{\circ}\text{C}\:\text{and}\, T_{min}&lt;15\,{}^{\circ}\text{C}
    /// \f]
    /// Devernalisation can occur if daily \f$T_{max}\f$ is above 30 \f$^{\circ}\text{C}\f$ and the total vernalisation (\f$V\f$) is less than 10 .
    /// \f[
    /// \Delta V_{d}=\min(0.5(T_{max}-30),\: V)\quad\text{when, }T_{max}&gt;30\,{}^{\circ}\text{C}\;\text{and}\; V&lt;10
    /// \f]
    /// The total vernalisation (\f$V\f$) is calculated by summing daily vernalisation and devernalisation from \p StartStageForEffects to \p EndStageForEffects. 
    /// \f[
    /// V=\sum(\Delta V-\Delta V_{d})
    /// \f]
    /// However, the vernalisation factor (\f$f_{v}\f$) is calculated just from \p StartStageForCumulativeVD to \p EndStageForCumulativeVD.
    /// \f[
    /// f_{V}=1-(0.0054545R_{V}+0.0003)\times(50-V)
    /// \f]
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class VernalisationEffect : Model, IFunction
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary>The vernalisation sensitivity factor</summary>
        [Link]
        IFunction VernSens = null;

        /// <summary>Amount of Vernal days accumulated</summary>
        [XmlIgnore]
        public double CumulativeVD { get; set; }

        /// <summary>Gets or sets the vern eff.</summary>
        /// <value>The vern eff.</value>
        [XmlIgnore]
        public double VernEff { get; set; }

        /// <summary>The start stage for cumulative vd</summary>
        [Description("StartStageForCumulativeVD")]
        public string StartStageForCumulativeVD { get; set; }
        /// <summary>The end stage for cumulative vd</summary>
        [Description("EndStageForCumulativeVD")]
        public string EndStageForCumulativeVD { get; set; }

        /// <summary>Trap the DoDailyInitialisation event.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            Vernalisation();
        }

        /// <summary>Initialise everything</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            CumulativeVD = 0;
        }

        /// <summary>Do our vernalisation</summary>
        public void Vernalisation()
        {
            double DeltaCumulativeVD = VernalisationDays(CrownTemperature, 0.0, CumulativeVD);

            double MaxVernalisationRequirement = 50; //maximum vernalisation requirement is 50 days
            VernEff = _VernalisationEffect(VernSens.Value, CumulativeVD, DeltaCumulativeVD, MaxVernalisationRequirement);


            CumulativeVD += DeltaCumulativeVD;
        }

        /// <summary>Calculate daily vernalisation and accumulate to g_cumvd</summary>
        /// <param name="CrownTemperature">The crown temperature.</param>
        /// <param name="Snow">The snow.</param>
        /// <param name="CumulativeVD">The cumulative vd.</param>
        /// <returns></returns>
        private double VernalisationDays(double CrownTemperature, double Snow, double CumulativeVD)
        {
            // Nwheat originally had the following if logic for determining whether
            // vernalisation is calculated for today
            //     if (cumvd .lt. reqvd
            //     :                    .and.
            //     :     (istage .eq.emerg .or. istage .eq. germ)) then
            //
            // In order to remove the explicit value 'reqvd' and make the stages
            // more flexibile this logic was replaced. - NIH 14/07/98
            double DeltaCumulativeVD = 0.0;
            if (Phenology.Between(StartStageForCumulativeVD, EndStageForCumulativeVD))
            {
                if (MetData.MinT < 15.0 && MetData.MaxT > 0.0)
                {
                    // Cold
                    double vd, vd1, vd2;
                    vd1 = 1.4 - 0.0778 * CrownTemperature;
                    vd2 = 0.5 + 13.44 / Math.Pow(MetData.MaxT - MetData.MinT + 3.0, 2) * CrownTemperature;
                    vd = Math.Min(vd1, vd2);
                    DeltaCumulativeVD = Math.Max(vd, 0.0);
                }
                if (MetData.MaxT > 30.0 && CumulativeVD + DeltaCumulativeVD < 10.0)
                {
                    // high temperature will reduce vernalization
                    DeltaCumulativeVD = -0.5 * (MetData.MaxT - 30.0);
                    DeltaCumulativeVD = -Math.Min(-(DeltaCumulativeVD), CumulativeVD);
                }
            }
            return DeltaCumulativeVD;
        }

        /// <summary>Crown temperature from nwheat</summary>
        /// <value>The crown temperature.</value>

        public double CrownTemperature
        {
            get
            {
                // Calculate max crown temperature
                double cx;
                if (MetData.MaxT < 0.0)
                    cx = 2.0 + MetData.MaxT * (0.4 + 0.0018 * Math.Pow(0 - 15.0, 2));
                else
                    cx = MetData.MaxT;

                // Calculate min crown temperature
                double cn;
                if (MetData.MinT < 0.0)
                    cn = 2.0 + MetData.MinT * (0.4 + 0.0018 * Math.Pow(0 - 15.0, 2));
                else
                    cn = MetData.MinT;

                return ((cn + cx) / 2.0);
            }
        }

        /// <summary>Vernalisation factor</summary>
        /// <param name="vern_sens">The vern_sens.</param>
        /// <param name="CumulativeVD">The cumulative vd.</param>
        /// <param name="DeltaCumulativeVD">The delta cumulative vd.</param>
        /// <param name="MaxVernalisationRequirement">The maximum vernalisation requirement.</param>
        /// <returns></returns>
        private double _VernalisationEffect(double vern_sens, double CumulativeVD, double DeltaCumulativeVD, double MaxVernalisationRequirement)
        {
            double vfac;                // vernalization factor
            double vern_sens_fac;
            double vern_effect = 1.0;

            if (Phenology.Between(StartStageForCumulativeVD, EndStageForCumulativeVD))
            {
                vern_sens_fac = vern_sens * 0.0054545 + 0.0003;
                vfac = 1.0 - vern_sens_fac * (MaxVernalisationRequirement - (CumulativeVD + DeltaCumulativeVD));
                vern_effect = Math.Max(vfac, 0.0);
                vern_effect = Math.Min(vern_effect, 1.0);
            }
            return vern_effect;
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        [Units("0-1")]
        public double Value
        {
            get
            {
                return VernEff;
            }
        }
    }
}
