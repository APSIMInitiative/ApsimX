using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Takes FLN and base phyllochron inputs and calculates Vrn expression rate coefficients for CAMP
    /// </summary>
    [Serializable]
    [Description("")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CAMP))]
    public class CalcCAMPVrnRates : Model
    {

        /// <summary>The parent CAMP model</summary>
        [Link(Type = LinkType.Ancestor, ByName = true)]
        private CAMP camp = null;
        /// <summary>The ancestor CAMP model and some relations</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction basePhyllochron = null;

        /// <summary>
        /// Takes observed (or estimated) final leaf numbers and phyllochron for genotype with (V) and without (N) vernalisation in long (L)
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
            double BasePhyllochron = basePhyllochron.Value();
            
            // Base Phyllochron duration of the Emergence Phase
            double EmergDurat = EnvData.TtEmerge/BasePhyllochron;

            //Base Phyllochron duration of vernalisation treatment
            double VernTreatDurat = (EnvData.VrnTreatTemp * EnvData.VrnTreatDuration) / BasePhyllochron;

            // The soonest a wheat plant may exhibit vern saturation
            double MinVS = 1.1;

            //////////////////////////////////////////////////////////////////////////////////////
            // Calculate phase durations (in Phyllochrons)
            //////////////////////////////////////////////////////////////////////////////////////
            
            // Calculate the accumulated phyllochrons at Terminal spikelet (TSHS) for each treatment from FLNData
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            double TS_LV = camp.calcTSHS(FLNset.LV);
            double TS_LN = camp.calcTSHS(FLNset.LN);
            double TS_SV = camp.calcTSHS(FLNset.SV);
            double TS_SN = camp.calcTSHS(FLNset.SN);

            // Minimum phyllochron duration from vernalisation saturation to terminal spikelet under long day conditions (MinVsTs)
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            // Assume maximum of 3, Data from Lincoln CE (CRWT153) showed varieties that have a high TSHS hit VS ~3HS prior to TS 
            double MinVsTsHS = Math.Min(3.0, TS_LV - MinVS);

            // Calculate the accumulated phyllochrons at vernalisation saturation for each treatment
            // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            double VS_LV = TS_LV - MinVsTsHS;
            double VS_LN = Math.Max(TS_LN - MinVsTsHS, VS_LV); //Constrained so not earlier than the LV treatment
            double VS_SV = Math.Min(VS_LV, TS_SV - MinVsTsHS); // Assume happens at the same time as VS_LV but not sooner than TS and minVS->TS would allow 
            double VSTS_SV = TS_SV - VS_SV;
            double VS_SN = Math.Max(TS_SN - VSTS_SV, VS_LV); //Constrained so not earlier than the LV treatment

            //////////////////////////////////////////////////////////////////////////////////////
            // Calculate base and maximum rates and Pp sensitivities
            //////////////////////////////////////////////////////////////////////////////////////

            // Base Vrn delta during vegetative phase.  Assuming base Vrn expression starts at sowing and reaches 1 at VS for the SN treatment where no cold or Vrn3 (short Pp) to upregulate 
            Params.BaseDVrnVeg = 1 / (VS_SN + EmergDurat);
            // The fastest rate that Vrn can accumulate to reach saturation (VS).
            Params.MaxDVrnVeg = 1 / (VS_SV + EmergDurat);
            // Base Vrn delta during early reproductive phase.  Assuming Vrn expression increases by 1 between VS and TS and proceeds at a base rate where not Vrn3 upregulation (short PP)
            Params.BaseDVrnER = 1 / (TS_SN - VS_SN);
            // The maximum Vrn delta during the early reproductive phase.  Assuming Vrn increases by 1 bewtween VS and TS and proceeds at the maximum rate under long photoperiod
            Params.MaxDVrnER = 1 / MinVsTsHS;
            // The relative increase in delta Vrn cuased by Vrn3 expression under long photoperiod during early reproductive phase
            Params.PpVrn3FactER = (Params.MaxDVrnER / Params.BaseDVrnER - 1)  + 1;
            // The relative increase in delta Vrn1 caused by Vrn3 expression under long Pp during vegetative phase.  Same as PpVrn3FactER unless VS_LN is small
            Params.PpVrn3FactVeg = ((1 / VS_LN) / Params.BaseDVrnVeg - 1)  + 1;
            Params.PpVrn3FactVeg = Math.Max(Params.PpVrn3FactER, Params.PpVrn3FactVeg);

            //////////////////////////////////////////////////////////////////////////////////////
            // Calculate Maximum Vrn2 expression under long photoperiods
            //////////////////////////////////////////////////////////////////////////////////////

            //The rate of Vrn expression before VS under long Pp when Vrn2 = 0 is the product of Vrn3 and base Vrn
            double dvrnxVrn3 = Math.Max(1 / VS_LN, Params.BaseDVrnVeg * Params.PpVrn3FactVeg);
            // Therefore the haun stage duration when effective baseVrn x Vrn3 is up regulating during the vegetative phase under long photoperiod without vernalisation is a funciton of the VS target (1) and the rate
            double vrnxVrn3Durat = 1 / dvrnxVrn3;
            // The accumulated Haun stages when Vrn2 expression is ended and effective baseVrn x Vrn3 expression starts under long Pp without vernalisation is vrn1xVrn3Durat prior to VS
            double endVrn2_LN = Math.Max(0, VS_LN - vrnxVrn3Durat);
            // The Vrn2 Expression that must be matched by Vrn1 before effective Vrn3 expression starts.  
            Params.MaxVrn2 = (endVrn2_LN + EmergDurat) * Params.BaseDVrnVeg ;

            //////////////////////////////////////////////////////////////////////////////////////
            // Calculate cold upregulation of Vrn1
            //////////////////////////////////////////////////////////////////////////////////////
            
            // The amount of Vrn expression at base rate in the LV treatment between sowing and when Vrn2 is suppressed 
            double baseVrnVeg_LV = (EmergDurat + VS_LV) * Params.BaseDVrnVeg;
            // The amount of persistant (methalated) Vrn1 upregulated due to cold at the end of the vernalisation treatment for LV
            // Is the VS threshold (1) plus the maximum Vrn2 less base vrn expression up to VS and Vrn3 upregulated expression between end of Vrn2 expression and VS
            double coldVrn1_LV = camp.VSThreshold + Params.MaxVrn2 - baseVrnVeg_LV;
            // Cold threshold required before Vrn1 starts methalating.  Based on Brooking and Jamieson data the lag duration is the same length as the duration of response up to full vernalisation
            // Therefore we assume the methalation threshold must be the same size as the amount of vrn1 that is required to give full persistend vernalisation response.
            Params.MethalationThreshold = coldVrn1_LV;
            // Haun stage duration of vernalisation, may be less than treatment duration if treatment goes past VS
            double vernDurat_LV = Math.Min(VernTreatDurat, VS_LV + EmergDurat);
            // The rate of Vrn1 expression under cold treatment calculated from the amopunt of cold vrn1 up regulation apparante plus the methalation threshold
            double coldDVrn1_LV = (coldVrn1_LV + Params.MethalationThreshold) / vernDurat_LV;
            // The upregulation of Vrn1 expression above base rate at 0oC
            double coldDVrn1Max = coldDVrn1_LV / Math.Exp(camp.k * EnvData.VrnTreatTemp);
            // The relative increase in delta Vrn1 caused by cold upregulation of Vrn1
            Params.ColdVrn1Fact = coldDVrn1Max / Params.BaseDVrnVeg;

            return Params;
        }
    }
}
