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
    public class CultivarRateParams
    {
        /// <summary>Base delta for Upregulation of Vrn1 >20oC</summary>
        public double BaseDVrn1 { get; set; }
        /// <summary>Maximum delta for Upregulation of Vrn1 at 0oC</summary>
        public double MaxDVrn1 { get; set; }
        /// <summary>Maximum delta for Upregulation of Vrn2 under PP > 16h</summary>
        public double MaxDVrn2 { get; set; }
        /// <summary>Base delta for Upregulation of Vrn3 at Pp below 8h </summary>
        public double BaseDVrn3 { get; set; }
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
        /// works through calculation scheme and assigns values to each parameter
        /// </summary>
        /// <param name="FLNset">Final leaf number set observations (or estimations) for cultivar</param>
        /// <param name="TreatmentTtDuration">Thermal time duration of vernalisation treatment</param>
        /// <param name="TtEmerge">Thermal time from imbibing to emergence</param>
        /// <param name="Tt">Temperature during vernalisatin treatment</param>
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
            // Haun stage duration from vernalisation saturation to terminal spikelet under long day conditions
            double LDVsTsHS = 3.0;

            // Calculate TSHS for each envronment set from FLNData
            double TSHS16hFull = camp.calcTSHS(FLNset.LongPpFullVern);
            double TSHS16hNil = camp.calcTSHS(FLNset.LongPpNilVern);
            double TSHS8hFull = camp.calcTSHS(FLNset.ShortPpFullVern);
            double TSHS8hNil = camp.calcTSHS(FLNset.ShortPpNilVern);

            // Photoperiod sensitivity (PPS)
            // the difference between TSHS at 8 and 16 h pp under 
            //full vernalisation.
            double PPS = Math.Max(0, TSHS8hFull - TSHS16hFull);

            // Maximum delta for Upregulation of Vrn3 (MaxDVrn3)
            // Occurs under long Pp conditions
            // Assuming Vrn3 increases from 0 - 1 between VS to TS and this takes 
            // 3 HS under long Pp conditions.
            double MaxDVrn3 = 1.0 / LDVsTsHS;

            // Base delta for upredulation of Vrn3 (BaseDVrn3) 
            // Occurs under short Pp conditions
            // Assuming Vrn3 infreases from 0 - 1 from VS to TS 
            // and this take 3 HS plus the additional HS from short Pp delay.
            Params.BaseDVrn3 = 1.0 / (LDVsTsHS + PPS);

            // Vernalistion Saturation Haun Stage under 8h Nil vern conditins  (VSHS8hNil)
            // Determine how many HS it would take to express Vrn3 == 1.0 
            // and subtract this from TSHS8hNill.
            // Bound to CompetenceHS as Vernalisation won't occur before this.
            double VSHS8hNil = Math.Max(camp.CompetenceHS, TSHS8hNil - (LDVsTsHS + PPS));

            // Base delta for upregulation of Vrn1 (BaseDVrn1) 
            // Occurs under non vernalising conditions
            // Assuming Vernalisation saturation occurs when Vrn1 == Vrn1Target 
            // under short Pp (no Vrn2) Vrn1Target = 1.0.
            // Need to include time from imbib to emerge in duration 
            Params.BaseDVrn1 = 1.0 / (VSHS8hNil + EmergHS);

            // Vernalisation Saturation Haun Stage under 16h Nil vern conditions (VSHS16hNil)
            // Assuming Vern saturation occurs 3.0 HS before TSHS16hNil
            // 3.0 is HS from VS to TS under long Pp conditions
            // Bound to CompetenceHS as Vernalisation won't occur before this.
            double VSHS16hNil = Math.Max(camp.CompetenceHS, TSHS16hNil - LDVsTsHS);

            // Long day Increase in Vrn1Target (Vrn1TargetInc) 
            // The extention in VSHS due to the action of Vrn2 under long days 
            // Work out how much VSHS is delayed under long Pp conditions.
            // Multiply this by BaseDVrn1 to get how much more Vrn1 was 
            // expressed before vernalisation
            double LDVrn1TargetInc = (VSHS16hNil - VSHS8hNil) * Params.BaseDVrn1;

            // Maximum delta for upregulation of Vrn2 (MaxDVrn2)
            // Occurs under long day conditions
            // LDVrnTargetInc is divided by the HS duration of Vrn2 expression 
            // which goes from CompetenceHS to VS.
            Params.MaxDVrn2 = Math.Max(0.0, LDVrn1TargetInc / (VSHS16hNil - camp.CompetenceHS));

            // Vernalistion Saturation Haun Stage under 8h Full vern conditions (VSHS8hFull)
            // Assuming VSHS occurs 3 HS pluss the HS delay from short Pp prior
            // prior to TSHS
            // Bound to CompetenceHS as Vernalisation won't occur before this.
            double VSHS8hFull = Math.Max(camp.CompetenceHS, TSHS8hFull - (LDVsTsHS + PPS));

            // Number of Haun stages from the end of treatment until Vernalisation saturation
            // Under 8 H full vern conditions (TransToVSHS8hFull)
            double TransToVSHS8hFull = VSHS8hFull + EmergHS - VernTreatHS;

            // The amount of methalated Vrn1 at the time of transition from vern treatment (MethVern1@Trans)
            // Under 8h when VrnTarget will be 1.0
            // VrnTarget less the amount of Vrn1 that expressed after transition
            double MethVern1AtTrans = 1.0 - TransToVSHS8hFull * Params.BaseDVrn1;

            // BaseVer1 expressed at transition from vernalisation treatment
            double BaseVrn1AtTrans = VernTreatHS * Params.BaseDVrn1;

            // Methalated Cold upredulated Vrn1 expression at the time of transition (MethColdVrn1@Trans) 
            // Subtract out BasedVrn1 expression up to transition
            double MethColdVrn1AtTrans = Math.Max(0.0, MethVern1AtTrans - BaseVrn1AtTrans);

            // The timing at which methalation started 
            // relative to transition from vernalising treatments (RelativeMethTiming)
            double RelativeMethTiming = 1.0 - MethColdVrn1AtTrans;

            // The amount of BaseVrn1 that had expressed at the time methalation of ColdVrn1 started
            double BaseVern1AtMeth = BaseVrn1AtTrans * RelativeMethTiming;

            // Cold induced delta upregulation of Vrn1 at treatment temperature ('DVrn1@Tt')
            // Methalation of Cold upregulated Vrn1 occurs when 
            // ColdUpRegVrn1 > Vrn1Target
            // Vrn1Target will be 1.0 in 8 hour conditions
            // so ColdUpRegVrn1 at transition will be:
            // 1.0 - BaseVern1@Meth + MethColdVrn1@Trans.
            // divide by treatment HS duration to give rate
            double DVrn1AtTt = (1.0 - BaseVern1AtMeth + MethColdVrn1AtTrans) / VernTreatHS;

            // Maximum upregulation delta Vrn1 (MUdVrn1)
            // The rate of dVrn1/HS at 0oC.  Calculate by rearanging UdVrn1 equation
            // UdVrn1 = MUdVrn1 * np.exp(k*Tt) as UdVrn1@Tt is known 
            Params.MaxDVrn1 = DVrn1AtTt / Math.Exp(camp.k * Tt);

            return Params;
        }
    }
}
