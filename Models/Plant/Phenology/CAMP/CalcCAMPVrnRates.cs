using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Xml.Serialization;

namespace Models.PMF.Phen
{

    /// <summary>
    /// Vernalisation rate parameter set for specific cultivar
    /// </summary>
    [Serializable]
    public class CultivarRateParams : Model
    {
        /// <summary>Earliest HS at which Vern saturation may occur</summary>
        public double VernCompetenceHS { get; set; }
        /// <summary>Base delta for Upregulation of Vrn1 >20oC</summary>
        public double BaseDVrn1 { get; set; }
        /// <summary>Maximum delta for Upregulation of Vrn1 at 0oC</summary>
        public double MaxDVrn1 { get; set; }
        /// <summary>Maximum delta for Upregulation of Vrn2 under PP > 16h</summary>
        public double MaxDVrn2 { get; set; }
        /// <summary>Base delta for Upregulation of Vrn3 at Pp below 8h </summary>
        public double BaseDVrn3 { get; set; }
        /// <summary>Maximum delta for Upregulation of Vrn3 at Pp above 16h </summary>
        public double MaxDVrn3 { get; set; }
        /// <summary>Maximum delta for upregulation of Vrn1 due to long Pp</summary>
        public double MaxDVrnX { get; set; }
    }

    /// <summary>
    /// Takes FLN and base phyllochron inputs and calculates Vrn expression rate coefficients for CAMP
    /// </summary>
    [Serializable]
    [Description("")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CAMP))]
    public class CalcCAMPVrnRates : Model
    {

        /// <summary>The parent CAMP model</summary>
        [Link(Type = LinkType.Ancestor,  ByName = true)]
        private CAMP camp = null;
        /// <summary>
        /// Takes observed (or estimated) final leaf numbers for genotype with (V) and without (N) vernalisation in long (L)
        /// and short (S) photoperiods and works through calculation scheme and assigns values for vrn expresson parameters
        /// </summary>
        /// <param name="FLNset">Set of Final leaf number observations (or estimations) for genotype</param>
        /// <param name="TreatmentTtDuration">Thermal time duration of vernalisation treatment</param>
        /// <param name="TtEmerge">Thermal time from imbibing to emergence</param>
        /// <param name="Tt">Temperature during vernalisation treatment</param>
        /// <returns></returns>
        public CultivarRateParams CalcCultivarParams(
            FinalLeafNumberSet FLNset,
            double TreatmentTtDuration, double TtEmerge, double Tt)

        {
            CultivarRateParams Params = new CultivarRateParams();

            //Haun stage duration of vernalisation treatment period
            double VernTreatHS = TreatmentTtDuration / (camp.BasePhyllochron * 0.75);
            // Haun stage equivelents from imbibing to Emergence
            double EmergHS = TtEmerge / (camp.BasePhyllochron * 0.75);
            
            // Calculate TSHS for each envronment set from FLNData
            double TSHS_LV = camp.calcTSHS(FLNset.LV);
            double TSHS_LN = camp.calcTSHS(FLNset.LN);
            double TSHS_SV = camp.calcTSHS(FLNset.SV);
            double TSHS_SN = camp.calcTSHS(FLNset.SN);

            // Haun stage duration from vernalisation saturation to terminal spikelet under long day conditions
            // Assume VS occurs at Competence HS under long Pp full vernalisation for varieties with FLN < 7.3 in these conditions
            // Assume maximum of 3, Data from Lincoln CE showed varieties that harve a high TSHS hit VS ~3HS prior to TS under these conditions
            // Assume HS = 1.0 is the earliest VSHS can occur
            double MinHSVsTs = Math.Min(3.0, TSHS_LV - 1.0);

            // Photoperiod sensitivity (PPS)
            // the difference between TSHS at 8 and 16 h pp under 
            //full vernalisation.
            double PPS = Math.Max(0, TSHS_SV - TSHS_LV);

            //Calculate VSHS for each environment from TSHS and photoperiod response
            double VSHS_LV = Math.Max(1.0, TSHS_LV - MinHSVsTs);
            double VSHS_LN = Math.Max(1.0, TSHS_LN - MinHSVsTs);
            double VSHS_SV = Math.Max(1.0, TSHS_SV - (MinHSVsTs + PPS));
            double VSHS_SN = Math.Max(1.0, TSHS_SN - (MinHSVsTs + PPS));

            // The earliest possible vernalistion occurs following full vernalisation under short Pp where Vrn2 is absent
            Params.VernCompetenceHS = VSHS_SV;

            // Maximum delta for Upregulation of Vrn3 (MaxDVrn3)
            // Occurs under long Pp conditions
            // Assuming Vrn3 increases from 0 - VernSatThreshold  between VS to TS and this takes 
            // 3 HS under long Pp conditions.
            Params.MaxDVrn3 = camp.VrnSatThreshold / MinHSVsTs;

            // Base delta for upredulation of Vrn3 (BaseDVrn3) 
            // Occurs under short Pp conditions
            // Assuming Vrn3 infreases from 0 - VernSatThreshold  from VS to TS 
            // and this take 3 HS plus the additional HS from short Pp delay.
            Params.BaseDVrn3 = camp.VrnSatThreshold / (MinHSVsTs + PPS);

            // Base delta for upregulation of Vrn1 (BaseDVrn1) 
            // Occurs under non vernalising conditions
            // Assuming Vernalisation saturation occurs when Vrn1 == Vrn1Target 
            // under short Pp (no Vrn2) Vrn1Target = 1.0.
            // Need to include time from imbib to emerge in duration 
            Params.BaseDVrn1 = camp.VrnSatThreshold / (VSHS_SN + EmergHS);

            // Maximum delta for upregulation of Vrn2 (MaxDVrn2)
            // Occurs under long day conditions when Vrn2 expression increases the amount 
            // of Vrn1 expression needed to saturate vernalisation and so VS is delayed
            // Assume 1 unit of Vrn1 supresses 1 unit of Vrn2.
            // So under long days without vernalisation this can be calculated as the 
            // amount of baseVrn1 expressed at VS
            // Any numbers below VernSatThreshold are irrelevent so make that lower bound
            Params.MaxDVrn2 = Math.Max(camp.VrnSatThreshold,(VSHS_LN + EmergHS) * Params.BaseDVrn1);

            // The amount of methalated Vrn1 at the time of transition from vern treatment (MethVern1@Trans)
            // Under 8h when MethVrn1 will be 1.0 at VSHS
            // less the amount of base Vrn1 that expressed after transition
            double HSEndTreatToVS = Math.Max(0, (VSHS_SV + EmergHS) - VernTreatHS);
            double MethVern1AtTrans = Params.MaxDVrn2 - (HSEndTreatToVS * Params.BaseDVrn1);

            // BaseVer1 expressed at transition from vernalisation treatment
            double BaseVrn1AtTrans = VernTreatHS * Params.BaseDVrn1;

            // Methalated Cold upredulated Vrn1 expression at the time of transition (MethColdVrn1@Trans) 
            // Subtract out BasedVrn1 expression up to transition
            double MethColdVrn1AtTrans = Math.Max(0.0, MethVern1AtTrans - BaseVrn1AtTrans);

            // Cold upregulated Vrn1 at time of transition
            // Will be Methalated cold vernalisation at transition plus
            // the amount of cold Vrn1 that is required for 
            // methalation to start (MethVrn1Threshold)
            double ColdVern1AtTrans = camp.MethalationThreshold + MethColdVrn1AtTrans;

            // Cold induced delta upregulation of Vrn1 at treatment temperature ('DVrn1@Tt')
            // Methalation of Cold upregulated Vrn1 occurs when 
            // ColdUpRegVrn1 > Vrn1Target
            // Vrn1Target will be 1.0 in 8 hour conditions
            // so ColdUpRegVrn1 at transition will be:
            // 1.0 - BaseVern1@Meth + MethColdVrn1@Trans.
            // divide by treatment HS duration to give rate
            double DVrn1AtTt = MathUtilities.Divide(ColdVern1AtTrans, VernTreatHS,0);

            // Maximum upregulation delta Vrn1 (MUdVrn1)
            // The rate of dVrn1/HS at 0oC.  Calculate by rearanging UdVrn1 equation
            // UdVrn1 = MUdVrn1 * np.exp(k*Tt) as UdVrn1@Tt is known 
            Params.MaxDVrn1 = MathUtilities.Divide(DVrn1AtTt,Math.Exp(camp.k * Tt),0);

            double BaseVrn1AtVSHS_LN = Math.Min(camp.VrnSatThreshold, (VSHS_LN + EmergHS) * Params.BaseDVrn1);


            Params.MaxDVrnX = (1 - BaseVrn1AtVSHS_LN) / (VSHS_LN - camp.PpCompetenceHS);


            return Params;
        }
    }
}
