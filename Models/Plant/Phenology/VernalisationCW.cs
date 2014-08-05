using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using System.Xml.Serialization;

namespace Models.PMF.Phen
{
    [Serializable]
    public class VernalisationCW : Model
    {
        [Link]
        Phenology Phenology = null;

        [Link]
        Function Photoperiod = null;

        [Link]
        WeatherFile Weather = null;

        [XmlIgnore]
        public double PhotopEff { get; set; }
        [XmlIgnore]
        public double VernEff { get; set; }
        private const double Snow = 0.0;
        private double Maxt = 0;
        private double Mint = 0;
        public double VernSens { get; set; }
        public double PhotopSens { get; set; }
        public string StartStageForEffects = "";
        public string EndStageForEffects = "";
        public string StartStageForCumulativeVD = "";
        public string EndStageForCumulativeVD = "";
        
        private double CumulativeVD = 0;

        /// <summary>
        /// Trap the NewWeatherDataAvailable event.
        /// </summary>
        [EventSubscribe("NewWeatherDataAvailable")]
        private void OnNewWeatherDataAvailable(object sender, EventArgs e)
        {
            Maxt = Weather.MetData.Maxt;
            Mint = Weather.MetData.Mint;
            Vernalisation(Weather.MetData.Maxt, Weather.MetData.Mint);
        }

        /// <summary>
        /// Initialise everything
        /// </summary>
        public override void OnSimulationCommencing()
        {
            CumulativeVD = 0;
            VernEff = 1;
            PhotopEff = 1;
        }

        /// <summary>
        /// Do our vernalisation
        /// </summary>
        public void Vernalisation(double Maxt, double Mint)
        {
            double DeltaCumulativeVD = VernalisationDays(Maxt, Mint, CrownTemperature, 0.0, CumulativeVD);

            double MaxVernalisationRequirement = 50; //maximum vernalisation requirement is 50 days
            VernEff = VernalisationEffect(VernSens, CumulativeVD, DeltaCumulativeVD, MaxVernalisationRequirement);

            PhotopEff = PhotoperiodEffect(Photoperiod.Value, PhotopSens);

            CumulativeVD += DeltaCumulativeVD;
        }

        /// <summary>
        /// Crown temperature from nwheat
        /// </summary>
        
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

        /// <summary>
        /// Calculate daily vernalisation and accumulate to g_cumvd
        /// </summary>
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

        /// <summary>
        /// Vernalisation factor
        /// </summary>
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

        /// <summary>
        /// Photoperiod factor
        /// </summary>
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
