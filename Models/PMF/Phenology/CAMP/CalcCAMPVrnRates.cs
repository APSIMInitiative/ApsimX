using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Linq;

namespace Models.PMF.Phen
{

    /// <summary>
    /// Vernalisation rate parameter set for specific cultivar
    /// </summary>
    [Serializable]
    public class CultivarRateParams : Model
    {
        /// <summary>Base delta for Upregulation of Vrn1 >20oC</summary>
        public double BaseDVrn1 { get; set; }
        /// <summary>Maximum delta for Upregulation of Vrn1 at 0oC</summary>
        public double MaxDVrn1 { get; set; }
        /// <summary>potential Vrn2 expression soon after emergence under > 16h</summary>
        public double MaxIpVrn2 { get; set; }
        /// <summary>delta potential Vrn2 expression  under > 16h</summary>
        public double MaxDpVrn2 { get; set; }
        /// <summary>Base delta for Upregulation of Vrn3 at Pp below 8h </summary>
        public double BaseDVrn3 { get; set; }
        /// <summary>Maximum delta for Upregulation of Vrn3 at Pp above 16h </summary>
        public double MaxDVrn3 { get; set; }
        /// <summary>Maximum delta for upregulation of Vrn1 due to long Pp</summary>
        public double MaxDVrnX { get; set; }
        /// <summary> Base phyllochron</summary>
        public double BasePhyllochron { get; set; }
        /// <summary>Intercept of the realationship between FLN and TSHS</summary>
        public double IntFLNvsTSHS { get; set; }
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
        /// <summary>The ancestor CAMP model and some relations</summary>
        [Link(Type = LinkType.Ancestor, ByName = true)]
        Phenology phenology = null;

        ///<summary>Function to convert HaunStages to base phyllochrons</summary>
        public double ConvertHStoBP(double HS)
        {
            double[] x = { 0, 2, 7, 30 };
            double[] y = { 0, 1.5, 6.5, 38.7 };
            bool DidInterpolate = false;
            return MathUtilities.LinearInterpReal(HS,x,y, out DidInterpolate);
        }

        /// <summary>
        /// Takes observed (or estimated) final leaf numbers for genotype with (V) and without (N) vernalisation in long (L)
        /// and short (S) photoperiods and works through calculation scheme and assigns values for vrn expresson parameters
        /// </summary>
        /// <param name="FLNset">Set of Final leaf number observations (or estimations) for genotype</param>
        /// <param name="EnvData">Controlled environment conditions used when observing FLN set</param>
        /// <returns>CultivarRateParams</returns>
        public CultivarRateParams CalcCultivarParams(
            FinalLeafNumberSet FLNset, FLNParameterEnvironment EnvData)

        {
            //////////////////////////////////////////////////////////////////////////////////////
            // Get some parameters organized and set up structure for results
            //////////////////////////////////////////////////////////////////////////////////////            

            // Initialise structure to hold vern rate coefficients
            CultivarRateParams Params = new CultivarRateParams();

            // Get some other parameters from phenology
            double maxLAR = phenology.FindChild<IFunction>("MaxLAR").Value();
            double minLAR = phenology.FindChild<IFunction>("MinLAR").Value();
            double PTQhf = phenology.FindChild<IFunction>("PTQhf").Value();
            LARPTQmodel LARmodel = phenology.FindChild<LARPTQmodel>("LARPTQmodel");

            //Calculate base phyllochron
            Params.BasePhyllochron = 1 / LARmodel.CalculateLAR(1.0, maxLAR, minLAR, PTQhf);

            //////////////////////////////////////////////////////////////////////////////////////
            // Calculate phase durations (in Base Phyllochrons)
            //////////////////////////////////////////////////////////////////////////////////////

            // Base phyllochron duration of Emergence (EmergBP)
            double EmergBP = EnvData.TtEmerge / Params.BasePhyllochron;

            //Haun stage duration of vernalisation treatment period
            double VrnTreatTtDurat = EnvData.VrnTreatTemp * EnvData.VrnTreatDuration;
            double VernTreatBP = VrnTreatTtDurat / Params.BasePhyllochron;

            // The soonest a wheat plant may exhibit vern saturation
            double MinVSBP = ConvertHStoBP(1.1);

            // Minimum Haun stage duration from vernalisation saturation to terminal spikelet under long day conditions (MinHSVsTs)
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            // Assume maximum of 3, Data from Lincoln CE (CRWT153) showed varieties that harve a high TSHS hit VS ~3HS prior to TS 
            double MinBPVsTs = Math.Min(ConvertHStoBP(3.0), (ConvertHStoBP(FLNset.LV) - MinVSBP) / 2.0);

            // The HS duratin from Vern sat to final leaf under long Pp vernalised treatment
            double VSBP_FL_LV = ConvertHStoBP(FLNset.LV) - MinVSBP - MinBPVsTs;

            // The Intercept of the assumed relationship between FLN and TSHS for genotype (IntFLNvsTSHS)
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            Params.IntFLNvsTSHS = Math.Min(2.85, VSBP_FL_LV / 1.1);

            // Calculate Terminal spikelet duration (TSHS) for each treatment from FLNData
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            double TSBP_LV = ConvertHStoBP(camp.calcTSHS(FLNset.LV, Params.IntFLNvsTSHS));
            double TSBP_LN = ConvertHStoBP(camp.calcTSHS(FLNset.LN, Params.IntFLNvsTSHS));
            double TSBP_SV = ConvertHStoBP(camp.calcTSHS(FLNset.SV, Params.IntFLNvsTSHS));
            double TSBP_SN = ConvertHStoBP(camp.calcTSHS(FLNset.SN, Params.IntFLNvsTSHS));
                                                      
            // Photoperiod sensitivity (PPS) is the difference between TSBP at 8 and 16 h pp under full vernalisation.
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            double PPS = Math.Max(0, TSBP_SV - TSBP_LV);

            // Vernalisation Saturation duration (VSBP) for each environment from TSBP and photoperiod response
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            // Terminal spikelet duration less the minimum duration from VS to TS under long day treatment
            double VSBP_LV = Math.Max(MinVSBP, TSBP_LV - MinBPVsTs);
            double VSBP_LN = Math.Max(MinVSBP, TSBP_LN - MinBPVsTs);
            // Terminal spikelet duration less the minimum duration from VS to TS and the photoperiod extension of VS to TS under short day treatment
            double VSBP_SV = Math.Max(MinVSBP, TSBP_SV - (MinBPVsTs + PPS));
            double VSBP_SN = Math.Max(MinVSBP, TSBP_SN - (MinBPVsTs + PPS));

            ////////////////////////////////////////////////////////////////////////
            // Calculate Photoperiod sensitivities
            ////////////////////////////////////////////////////////////////////////

            // Maximum delta for Upregulation of Vrn3 (MaxDVrn3)
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            // Occurs under long Pp conditions, Assuming Vrn3 increases from 0 - VernSatThreshold between VS to TS and this takes 
            // MinBPVsTs under long Pp conditions.
            Params.MaxDVrn3 = camp.VrnSatThreshold / MinBPVsTs;


            // Base delta for upredulation of Vrn3 (BaseDVrn3)
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            // Occurs under short Pp conditions, Assuming Vrn3 increases from 0 - VernSatThreshold from VS to TS 
            // and this take MinBPVSTs plus the additional BP from short Pp delay.
            Params.BaseDVrn3 = camp.VrnSatThreshold / (MinBPVsTs + PPS);

            //// Under long day conditions Vrn3 expresses at its maximum rate and plant moves quickley from VS to TS.
            //// Genotypic variation in MaxDVrn3 will contribute to differences in earlyness per se
            //// Under shord day condition Vrn3 expressssion may be slower, taking longer to get from VS to TS
            //// Photoperiod sensitiviey of a genotype is determined by differences in BaseVrn3 and MaxVrn3.  
            //// if the two values are the same the genotype will not show photoperiod sensitivity
            //// the greater the difference the more resonse the genotype will show to photoperiod

            //////////////////////////////////////////////////////////////////
            // Calculate Base development rate 
            //////////////////////////////////////////////////////////////////

            // Base delta for upregulation of Vrn1 (BaseDVrn1) 
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            // The rate of expression is calculated as the amount of expression divided by the duration
            // Under short Pp treatment Vrn2 is absent so the amount of Vrn1 expression required for VS to occur is given by VernalisationThreshold
            // Under non vernalising conditions Vrn1 expression will happen at the base rate.  
            // VrnX expression is absent in short days so baseVrn1 is the only contributor to the timing of VS
            // BaseVrn1 expression starts when the seed is imbibed so the duration of expression is emergence plus VS
            Params.BaseDVrn1 = camp.VrnSatThreshold / (VSBP_SN + EmergBP);

            // Genotypic variation in BaseDVrn1 contributes to intrinsic earlyness.
            // A genotype with high BaseDVrn1 will reach VS quickly regardless of vernalisation exposure
            // A genotype with low BaseDVrn1 will reach VS slowly if not vernalised but the duration of VS may be decreased
            // by exposure to vernalising temperatures depending on cold vernalisation sensitivity

            //////////////////////////////////////////////////////////////////////////////////////////
            // Cold Vernalisation Sensitivity Calculations
            //////////////////////////////////////////////////////////////////////////////////////////

            // Calcualtions of vernalisation sensitivity use data from short Pp treatments because Vrn2 and Vrnx are absent
            // in these conditions and measured vernalistion response will be the result of Vrn1 expression alone.

            // Vernalisation Sensitivity (VS) measured simply as the difference between SV and SN treatuemnts
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            double VS = VSBP_SN - VSBP_SV;

            // To determine the effects of vernalisation treatment on Vrn1 expression we need to seperate these from baseVrn1 expression
            // BaseVrn1 expression at VSBP ('BaseVrn1AtVSBP_SV')
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            double BaseVrn1AtVSBP_SV = (VSBP_SV + EmergBP) * Params.BaseDVrn1;

            // Any accelleration in VS due to cold exposure under short Photoperiod will be due to methatalated Vrn1 expression
            // The amount of Vrn1 required for VS is given by the Vernalisation Threshold so the amount of methalated Vrn expression
            // at VS must be this threshold less the amount of vrn1 contributed by base Vrn1 expression over this duration
            // Methalated Cold Vrn1 expression under short Pp vernalisiation treatment (MethColdVern1AtTrans_SV)
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            double MethColdVern1AtTrans_SV = Math.Max(0.0, camp.VrnSatThreshold - BaseVrn1AtVSBP_SV);

            // Cold Vrn 1 expression must first be upregulated to a methalation threshold and any further expression beyond this 
            // threshold is methalated into a persistant vernalisation response.  Thus the total expression of cold Vrn1 due to
            // the vernalisation treatment applied is givin by MethColdVern1AtTrans_SV plus the Methalation Threshold
            // Cold Vern1 expression at VSBP under short Pp vernalised conditions (ColdVrn1AtVSBP_SV)
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            double ColdVrn1AtVSBP_SV = camp.MethalationThreshold + MethColdVern1AtTrans_SV;

            // The Base Phyllochron duration over which vernalisation temperatures will have an effect is the minimum of the cold 
            // treatment duration (VernTreatBP) and the BP when vernalisation occurs as cold exposure after this is irrelevent
            // Effective BP duration of cold treatment (EffectiveColdBP)
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            double EffectiveColdBP = Math.Min(VSBP_SV + EmergBP, VernTreatBP);

            // Then we calculate the rate as the amount divided by the duration
            // Rate of cold Vrn1 expression at the vernalisation treatment temperture ('DVrn1AtVrnTreatTemp')
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            double DVrn1AtVrnTreatTemp = ColdVrn1AtVSBP_SV / EffectiveColdBP;

            // This rate is dependent on the temperature of the duration treatment and can be extrapolated to the maximum rate (at 0oC)
            // using the exponential funciton proposed by Brown etal 2013 Annals of Botany.
            // dVrn1 = MaxdVrn1 * np.exp(k*VrnTreatTemp) as DVrn1 At VrnTreatTemp' is known 
            // MaxDVrn1 is set to vero for genotypes with VS < 0.5 as these varieties are insensitive and calculated rates of MaxDVrn1 are simply amplifying noise
            // Maximum upregulation delta Vrn1 (MaxDVrn1)
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            if (VS > 0)
                Params.MaxDVrn1 = DVrn1AtVrnTreatTemp / Math.Exp(camp.k * EnvData.VrnTreatTemp);
            else
                Params.MaxDVrn1 = 0.0;

            // Genotypic variation in MaxDVrn1 combine with BaseDVrn1 to determine cold temperature vernalisatin sensitiviy
            // A genotype with low BaseDVrn1 and high MaxDVrn1 will show high vernalisation sensitivity and sensitivity will decline 
            // either BaseDVrn1 increasees of MaxDVrn1 decreases.

            ////////////////////////////////////////////////////////////////////////////
            // Photoperiod effects on vernalisation
            ////////////////////////////////////////////////////////////////////////////

            // The parameters calculated above deal with photoperiod sensitivity in fully vernalised crops and vernalisation sensitivity
            // under short photoperiod where cold is the only factor driving vernalisation response.
            // Under long days photoperiod can also interact with vernalisation, either slowing the rate of vernalisation or speding it up

            // Vernalisation photoperiod sensitivy parameter (VPPS) is calculated to determine what effect photoperiod will have.
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            double VPPS = 1 - ((VSBP_LN + EmergBP) * Params.BaseDVrn1);

            // Genotypes that have a negative VPPS demonstrate short day vernalisation.  Under long days vernalisation requirement is reduced
            // This is caused by the expression of Vrn2 which must be blocked by additional expression of Vrn1 meaning vernalisation takes longer.  
            // The assumed mechanisum for Vrn2 is that it is expressed to a potential level and that one unit of Vrn1 will block the effective 
            // expression of Vrn2 so actual Vrn2 expression will always be lower than potential
            // We can make some assumptions about the amounts of Vrn2 expression under long Pp conditions to calcualte rates                                
            // Firstly, under un-vernalised conditions we can calculate the potential Vrn2 expression simply from
            // the amount of BaseVrn1 expression up to VSBP

            // Potential Vern2 expression at VS under long Pp UnVerrnalised treatment (pVrn2AtVSBP_LV)
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            double pVrn2AtVSBP_LN = (VSBP_LN + EmergBP) * Params.BaseDVrn1;

            // Under full vernalised treatment Vrn2 expression must be less than or equal to VernalisationThreshold at the time of VS.
            // If we assume it is equal to VernalisationThreshold this provides us with another amount of pVrn2
            // Potential Vern2 expression at VS under long Pp Vernalised treatment (pVrn2AtVSBP_LV)
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            double pVrn2AtVSBP_LV = camp.VrnSatThreshold;

            // If we regress these two potential Vrn2 amounts against their durations to VS the slope give us a rate of Vrn2 expression
            // The resulting rate needs to always be less than BaseDVrn1 so Vrn1 can catch up with Vrn2 to cause vernalisation
            // the amount of Vrn2 needs to quickly exceed BaseDVrn1 soon after emergence to achieve a delay in VS.
            // The intercept of the regression quantifys how much Vrn2 would be expressed at emergence.  

            // Initial potential Vrn2 at emergence (IPVrn2) and potential Vrn2 rate there after (DpVrn2)
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            Params.MaxIpVrn2 = 0;
            Params.MaxDpVrn2 = 0;

            //Vrn2 parameters only relevent for genotypes with nevaitve VPPS and a low BaseDVrn1.  Genotypes with a high base
            if (VPPS < 0) //and (BaseDVrn1 < 0.4): 
            {
                if (VSBP_LN - VSBP_LV == 0)
                    Params.MaxIpVrn2 = pVrn2AtVSBP_LN;
                else
                {
                    Params.MaxDpVrn2 = Math.Max(0, (pVrn2AtVSBP_LN - pVrn2AtVSBP_LV) / (VSBP_LN - VSBP_LV));
                    Params.MaxIpVrn2 = (pVrn2AtVSBP_LV) - (VSBP_LV * Params.MaxDpVrn2);
                    if (Params.MaxDpVrn2 >= (Params.BaseDVrn1 * 0.99)) //If DpVrn2 exceeds baseVrn1 we need to do some forcing so it doesn't
                    {
                        double BaseVrn1AtVSBP_LV = (VSBP_LV + EmergBP) * Params.BaseDVrn1;
                        Params.MaxDpVrn2 = Math.Max(0, (pVrn2AtVSBP_LN - (BaseVrn1AtVSBP_LV * 1.2)) / (VSBP_LN - VSBP_LV));
                        Params.MaxIpVrn2 = (pVrn2AtVSBP_LN) - (VSBP_LN * Params.MaxDpVrn2);
                    }
                }
            }

            // Genotypes that have a positive VPPS show an acelleration of vernalisation under long day conditions.
            // The molecular mechanium for this is uncertain so we attrubute it to VrnX
            // Under Long Pp unvernalised varieties will reach VS BP sooner than BaseVrn1 expresion determines.
            // BaseVrn1 expression at VS under long Pp unvernalised treatment (BaseVrn1AtVSBP_LN)
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            double BaseVrn1AtVSBP_LN = Math.Min(camp.VrnSatThreshold, (VSBP_LN + EmergBP) * Params.BaseDVrn1);

            // If we assume the expression of VrnX upregulates Vrn1 an equivelent amount then the amount of VrnX expression can be
            // Estimated as the difference between Base Vrn1 at VS and the Vernalisation threshold.  The duration of expression is
            // VSBP assuming VrnX is expressed from emergence until VS.
            // Maximum rate of Vrnx Expression (MaxDVrnX)
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            Params.MaxDVrnX = (camp.VrnSatThreshold - BaseVrn1AtVSBP_LN) / (VSBP_LN);
            
            return Params;
        }
    }
}
