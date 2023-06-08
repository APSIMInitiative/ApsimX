using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Newtonsoft.Json;

namespace Models.Soils
{

    /// <summary>
    /// The clock model is resonsible for controlling the daily timestep in APSIM. It 
    /// keeps track of the simulation date and loops from the start date to the end
    /// date, publishing events that other models can subscribe to.
    /// </summary>
    [Serializable]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    [ValidParent(ParentType = typeof(Soil))]
    public class Erosion : Model
    {
        [Link]
        private ISummary summary = null;

        [Link]
        private ISoilWater waterBalance = null;

        /// <summary>Describes the different erosion models supported.</summary>
        public enum ModelTypeEnum
        {
            /// <summary>Freebairn cover-sediment concentration model.</summary>
            Freebairn,

            /// <summary>Simplified rose model from PERFECT.</summary>
            Rose
        }

        /// <summary>Slope of plot (%).</summary>
        [Description("Slope (%)")]
        public double slope { get; set; }

        /// <summary>Length of plot (m).</summary>
        [Description("Slope length")]
        public double slope_length { get; set; }

        /// <summary>Erosion model algorithm.</summary>
        [Description("Erosion model algorithm")]
        public ModelTypeEnum ModelType { get; set; }

        /// <summary>USLE Soil erodibility factor (t/ha/EI 30 )(bedload).</summary>
        [Description("USLE Soil erodibility factor (t/ha/EI 30 )(bedload)")]
        [Display(VisibleCallback = "IsFreebairnModel")]
        public double k_factor_bed { get; set; }

        /// <summary>USLE Soil erodibility factor (t/ha/EI 30 )(suspended load).</summary>
        [Description("USLE Soil erodibility factor (t/ha/EI 30 )(suspended load) OPTIONAL")]
        [Display(VisibleCallback = "IsFreebairnModel")]
        public double k_factor_susp { get; set; }

        /// <summary>USLE Supporting practise factor.</summary>
        [Description("USLE Supporting practise factor")]
        [Display(VisibleCallback = "IsFreebairnModel")]
        public double p_factor { get; set; }

        /// <summary>Efficency of bedload entrainmnt(bare).</summary>
        [Description("Efficency of bedload entrainmnt(bare)")]
        [Display(VisibleCallback = "IsRoseModel")]
        public double entrain_eff_bed { get; set; }

        /// <summary>Efficency of bedload entrainmnt(bare).</summary>
        [Description("Efficency of suspended load entrainmnt(bare)")]
        [Display(VisibleCallback = "IsRoseModel")]
        public double entrain_eff_susp { get; set; }

        /// <summary>Coeffieient for calculating lambda in Rose model(bedload).</summary>
        [Description("Coeffieient for calculating lambda in Rose model(bedload)")]
        [Display(VisibleCallback = "IsRoseModel")]
        public double eros_rose_b2_bed { get; set; }

        /// <summary>Coeffieient for calculating lambda in Rose model(suspended load).</summary>
        [Description("Coeffieient for calculating lambda in Rose model(suspended load)")]
        [Display(VisibleCallback = "IsRoseModel")]
        public double eros_rose_b2_susp { get; set; }

        /// <summary>Daily soil loss in bed.</summary>
        [Units("t/ha")]
        public double soil_loss_bed;

        /// <summary>Daily soil loss in suspension.</summary>
        [Units("t/ha")]
        public double soil_loss_susp;

        /// <summary>Soil loss from surface.</summary>
        [Units("t/ha")]
        public double SoilLoss => soil_loss_bed + soil_loss_susp;

        /// <summary>Cover used in soil loss equation.</summary>
        [Units("0-1")]
        public double erosion_cover { get; set; }

        /// <summary>USLE slope-length factor[calculated].</summary>
        [JsonIgnore]
        public double ls_factor { get; set; }

        /// <summary>Is the Freebairn model turned on?</summary>
        public bool IsFreebairnModel => ModelType == ModelTypeEnum.Freebairn;

        /// <summary>Is the Freebairn model turned on?</summary>
        public bool IsRoseModel => ModelType == ModelTypeEnum.Rose;


        /// <summary>An event handler to signal start of a simulation.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            soil_loss_bed = 0;
            soil_loss_susp = 0;

            // Calculate USLE LS factor
            double s = slope * Constants.pcnt2fract;
            double a = 0.6 * (1.0 - Math.Exp(-35.835 * s));
            ls_factor = Math.Pow(slope_length / 22.1, a) * (65.41 * s * s + 4.56 * s + 0.065);

            if (ModelType == ModelTypeEnum.Freebairn)
            {
                summary.WriteMessage(this, "Freebairn cover-sediment concentration model", MessageType.Information);
                summary.WriteMessage(this, $"LS factor:{ls_factor:F4}", MessageType.Information);
                if (k_factor_susp <= 0.0)
                    summary.WriteMessage(this, $"K factor:{k_factor_bed:F4}", MessageType.Information);
                else
                {
                    summary.WriteMessage(this, $"K factor (bedload):{k_factor_bed:F4}", MessageType.Information);
                    summary.WriteMessage(this, $"K factor (suspended load):{k_factor_susp:F4}", MessageType.Information);
                }
                summary.WriteMessage(this, $"P factor: {p_factor:F4}", MessageType.Information);
            }
            else
            {
                summary.WriteMessage(this, "Rose sediment concentration model", MessageType.Information);
                if (entrain_eff_susp <= 0)
                    summary.WriteMessage(this, $"Efficiency of entrainment:{entrain_eff_bed:F4}", MessageType.Information);
                else
                {
                    summary.WriteMessage(this, $"Efficiency of bed load entrainment:{entrain_eff_bed:F4}", MessageType.Information);
                    summary.WriteMessage(this, $"Efficiency of susp. load entrainment:{entrain_eff_susp:F4}", MessageType.Information);
                }
                summary.WriteMessage(this, $"Slope (%):{slope}", MessageType.Information);
            }
        }

        /// <summary>An event handler to signal erosion should perform its calculations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event data.</param>
        [EventSubscribe("DoSoilErosion")]
        private void OnDoSoilErosion(object sender, EventArgs e)
        {
            soil_loss_bed = 0.0;
            soil_loss_susp = 0.0;

            if (ModelType == ModelTypeEnum.Freebairn)
                CalculateFreebairn();
            else
                CalculateRose();
        }

        private void CalculateFreebairn()
        {
            // sediment concentration (%) ie.g soil / g water * 100
            double sed_conc;

            double erosion_cover_pcnt = erosion_cover * Constants.fract2pcnt;
            if (erosion_cover < 0.5)
                sed_conc = 16.52 - 0.46 * erosion_cover_pcnt + 0.0031 * erosion_cover_pcnt * erosion_cover_pcnt;
            else
                sed_conc = 2.54 - 0.0254 * erosion_cover_pcnt;

            soil_loss_bed = sed_conc * Constants.pcnt2fract * Constants.g2t / (Constants.g2mm * Constants.sm2ha)
                                     * ls_factor * k_factor_bed
                                     * p_factor * waterBalance.Runoff;

            soil_loss_susp = sed_conc * Constants.pcnt2fract * Constants.g2t / (Constants.g2mm * Constants.sm2ha)
                                      * ls_factor * k_factor_susp
                                      * p_factor * waterBalance.Runoff;
        }

        /// <summary>
        /// This subroutine calculates soil loss using the simplified Rose algorithm.                                                     *
        /// </summary>
        /// <remarks>
        ///     apsim         perfect   descr
        ///     -----------------------------------
        ///     total_cover - covm      - mulch cover(0 - 1)
        ///     entrain_eff - kusle     - efficiency of entrainment(bare conditions)
        ///     runoff      - runf      -  event runoff (mm)
        ///     (returned)  - sed       -  soil loss(t/ha)
        ///     slope       - aslope    -  slope(%)
        /// </remarks>
        private void CalculateRose()
        {
            double lambda_bed = entrain_eff_bed * Math.Exp(-eros_rose_b2_bed * erosion_cover * Constants.fract2pcnt);

            soil_loss_bed = 2700.0 * (slope * Constants.pcnt2fract)
                                   * (1.0 - erosion_cover)
                                   * lambda_bed * waterBalance.Runoff / 100.0;


            double lambda_susp = entrain_eff_susp * Math.Exp(-eros_rose_b2_susp * erosion_cover * Constants.fract2pcnt);

            soil_loss_susp = 2700.0 * (slope * Constants.pcnt2fract)
                                    * (1.0 - erosion_cover)
                                    * lambda_susp * waterBalance.Runoff / 100.0;
        }
    }
}