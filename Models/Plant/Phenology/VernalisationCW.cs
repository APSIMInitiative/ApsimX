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
    /// \pre A \ref Models.PMF.Phen.Phenology "Phenology" model has to exist.
    /// \pre A \ref Models.PMF.Functions.PhotoperiodFunction "PhotoperiodFunction" 
    ///     model has to exist to calculate the day length.
    /// \pre A \ref Models.WeatherFile "WeatherFile" model has to exist to 
    /// retrieve the daily minimum and maximum temperature.
    /// \param VernSens The vernalization sensitivity
    /// \param PhotopSens The photoperiod sensitivity
    /// \param StartStageForEffects The start stage to calculate the vernalization and photoperiod effects
    /// \param EndStageForEffects The end stage to calculate the vernalization and photoperiod effects
    /// \param StartStageForCumulativeVD The start stage to calculate the cumulative vernalization
    /// \param EndStageForCumulativeVD The end stage to calculate the cumulative vernalization
    /// \retval VernEff The vernalization effects (from 0 to 1)
    /// \retval PhotopEff The photoperiod effects (from 0 to 1)
    ///<remarks>
    /// Vernalization
    /// ------------------------
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
    /// Photoperiod
    /// ------------------------
    /// Photoperiod is calculated from day of year and latitude using standard astronomical equations accounting for civil twilight 
    /// using the parameter \p twilight in \ref Models.PMF.Functions.PhotoperiodFunction. In APSIM, 
    /// the photoperiod affects phenology between \p StartStageForEffects and \p EndStageForEffects. During this period, thermal time 
    /// is affected by a photoperiod factor (\f$f_{D}\f$) that is calculated by
    /// \f[
    /// f_{D}=1-0.002R_{p}(20-L_{P})^{2}
    /// \f]
    /// where \f$L_{P}\f$ is the day length (h) from \ref Models.PMF.Functions.PhotoperiodFunction.
    ///</remarks>
    [Serializable]
    public class VernalisationCW : Model
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The photoperiod</summary>
        [Link]
        IFunction Photoperiod = null;

        /// <summary>The weather</summary>
        [Link]
        IWeather Weather = null;

        /// <summary>Gets or sets the photop eff.</summary>
        /// <value>The photop eff.</value>
        [XmlIgnore]
        public double PhotopEff { get; set; }
        /// <summary>Gets or sets the vern eff.</summary>
        /// <value>The vern eff.</value>
        [XmlIgnore]
        public double VernEff { get; set; }
        /// <summary>The snow</summary>
        private const double Snow = 0.0;
        /// <summary>The maxt</summary>
        private double Maxt = 0;
        /// <summary>The mint</summary>
        private double Mint = 0;
        /// <summary>Gets or sets the vern sens.</summary>
        /// <value>The vern sens.</value>
        public double VernSens { get; set; }
        /// <summary>Gets or sets the photop sens.</summary>
        /// <value>The photop sens.</value>
        public double PhotopSens { get; set; }
        /// <summary>The start stage for effects</summary>
        public string StartStageForEffects = "";
        /// <summary>The end stage for effects</summary>
        public string EndStageForEffects = "";
        /// <summary>The start stage for cumulative vd</summary>
        public string StartStageForCumulativeVD = "";
        /// <summary>The end stage for cumulative vd</summary>
        public string EndStageForCumulativeVD = "";

        /// <summary>The cumulative vd</summary>
        private double CumulativeVD = 0;

        /// <summary>Trap the DoDailyInitialisation event.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            Maxt = Weather.MaxT;
            Mint = Weather.MinT;
            Vernalisation(Weather.MaxT, Weather.MinT);
        }

        /// <summary>Initialise everything</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            CumulativeVD = 0;
            VernEff = 1;
            PhotopEff = 1;
        }

        /// <summary>Do our vernalisation</summary>
        /// <param name="Maxt">The maxt.</param>
        /// <param name="Mint">The mint.</param>
        public void Vernalisation(double Maxt, double Mint)
        {
            double DeltaCumulativeVD = VernalisationDays(Maxt, Mint, CrownTemperature, 0.0, CumulativeVD);

            double MaxVernalisationRequirement = 50; //maximum vernalisation requirement is 50 days
            VernEff = VernalisationEffect(VernSens, CumulativeVD, DeltaCumulativeVD, MaxVernalisationRequirement);

            PhotopEff = PhotoperiodEffect(Photoperiod.Value, PhotopSens);

            CumulativeVD += DeltaCumulativeVD;
        }

        /// <summary>Crown temperature from nwheat</summary>
        /// <value>The crown temperature.</value>
        
        public double CrownTemperature
        {
            get
            {
                // Calculate max crown temperature
                double cx;
                if (Maxt < 0.0)
                    cx = 2.0 + Maxt * (0.4 + 0.0018 * Math.Pow(Snow - 15.0, 2));
                else
                    cx = Maxt;

                // Calculate min crown temperature
                double cn;
                if (Mint < 0.0)
                    cn = 2.0 + Mint * (0.4 + 0.0018 * Math.Pow(Snow - 15.0, 2));
                else
                    cn = Mint;

                return ((cn + cx) / 2.0);
            }
        }

        /// <summary>Calculate daily vernalisation and accumulate to g_cumvd</summary>
        /// <param name="MaxT">The maximum t.</param>
        /// <param name="MinT">The minimum t.</param>
        /// <param name="CrownTemperature">The crown temperature.</param>
        /// <param name="Snow">The snow.</param>
        /// <param name="CumulativeVD">The cumulative vd.</param>
        /// <returns></returns>
        private double VernalisationDays(double MaxT, double MinT, double CrownTemperature, double Snow, double CumulativeVD)
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
                if (MinT < 15.0 && MaxT > 0.0)
                {
                    // Cold
                    double vd, vd1, vd2;
                    vd1 = 1.4 - 0.0778 * CrownTemperature;
                    vd2 = 0.5 + 13.44 / Math.Pow(MaxT - MinT + 3.0, 2) * CrownTemperature;
                    vd = Math.Min(vd1, vd2);
                    DeltaCumulativeVD = Math.Max(vd, 0.0);
                }
                if (MaxT > 30.0 && CumulativeVD + DeltaCumulativeVD < 10.0)
                {
                    // high temperature will reduce vernalization
                    DeltaCumulativeVD = -0.5 * (MaxT - 30.0);
                    DeltaCumulativeVD = -Math.Min(-(DeltaCumulativeVD), CumulativeVD);
                }
            }
            return DeltaCumulativeVD;
        }

        /// <summary>Vernalisation factor</summary>
        /// <param name="vern_sens">The vern_sens.</param>
        /// <param name="CumulativeVD">The cumulative vd.</param>
        /// <param name="DeltaCumulativeVD">The delta cumulative vd.</param>
        /// <param name="MaxVernalisationRequirement">The maximum vernalisation requirement.</param>
        /// <returns></returns>
        private double VernalisationEffect(double vern_sens, double CumulativeVD, double DeltaCumulativeVD, double MaxVernalisationRequirement)
        {
            double vfac;                // vernalization factor
            double vern_sens_fac;
            double vern_effect = 1.0;

            if (Phenology.Between(StartStageForEffects, EndStageForEffects))
            {
                vern_sens_fac = vern_sens * 0.0054545 + 0.0003;
                vfac = 1.0 - vern_sens_fac * (MaxVernalisationRequirement - (CumulativeVD + DeltaCumulativeVD));
                vern_effect = Math.Max(vfac, 0.0);
                vern_effect = Math.Min(vern_effect, 1.0);
            }
            return vern_effect;
        }

        /// <summary>Photoperiod factor</summary>
        /// <param name="Photoperiod">The photoperiod.</param>
        /// <param name="photop_sens">The photop_sens.</param>
        /// <returns></returns>
        private double PhotoperiodEffect(double Photoperiod, double photop_sens)
        {
            double photop_eff = 1.0;

            if (Phenology.Between(StartStageForEffects, EndStageForEffects))
            {
                double photop_sen_factor = photop_sens * 0.002;
                photop_eff = 1.0 - photop_sen_factor * Math.Pow(20.0 - Photoperiod, 2);
                photop_eff = Math.Max(photop_eff, 0.0);
                photop_eff = Math.Min(photop_eff, 1.0);
            }
            return photop_eff;
        }


    }
}
