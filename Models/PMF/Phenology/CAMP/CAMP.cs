using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Models.PMF.Phen
{
   
    /// <summary>
    /// Development Gene Expression
    /// </summary>
    [Serializable]
    [Description("")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class CAMP : Model, IVrn1Expression
    {
        /// <summary>The summary</summary>
        [Link]
        private ISummary summary = null;

        // Other Model dependencies links
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction tt = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction DailyColdVrn1 = null;

        [Link(Type = LinkType.Child, ByName = true)]
        CalcCAMPVrnRates calcCAMPVrnRates = null;

        // Cultivar specific Phenology parameters
        [Link(Type = LinkType.Child, ByName = true)]
        FinalLeafNumberSet FLNparams = null;

        // Cultivar specific Phenology parameters
        [Link(Type = LinkType.Child, ByName = true)]
        FLNParameterEnvironment EnvData = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction PpResponse = null;

        /// <summary>The ancestor CAMP model and some relations</summary>
        [Link(Type = LinkType.Ancestor, ByName = true)]
        Phenology phenology = null;

        /// <summary>The ancestor CAMP model and some relations</summary>
        [Link(Type = LinkType.Path, Path = "[Phenology].HaunStage")]
        IFunction haunStage = null;

        /// <summary>
        /// Calculate delta of upregulation for photo period (Pp) sensitive genes
        /// </summary>
        /// <param name="baseUR">dVrn/HS below 8h Pp</param>
        /// <param name="maxUR">dVrn/HS above 16h Pp</param>
        /// <param name="dTt">delta base phyllochron</param>
        /// <returns></returns>
        private double CalcdPPVrn(double baseUR, double maxUR, double dTt)
        {
            return (baseUR + (maxUR - baseUR) * PpResponse.Value()) * dTt;
        }

        /// <summary>
        /// Calculate upregulation of base Vrn1
        /// </summary>
        /// <param name="dHS">delta haun stage</param>
        /// <param name="BaseDVrn1">delta Vrn1/BP at non-vernalising temperatures</param>
        /// <returns></returns>
        private double CalcBaseUpRegVrn1(double dHS, double BaseDVrn1)
        {
            if (this.dHS < 0)
                BaseDVrn1 = 0;
            return BaseDVrn1 * dHS;
        }

        /// <summary>
        /// Potential Upregulation of Vrn2 from long photoperiod.  Actual Vrn2 expression will be less than this because it is blocked by Vrn1
        /// </summary>
        /// <param name="LPp">Long photoperiod hours</param>
        /// <param name="IpVrn2"> Initial potential Vrn2 at first experience of Pp > 8 (normally at emergence)</param>
        /// <param name="DpVrn2">Delta of potential Vrn2 in response to accumulation of LPpHS</param>
        /// <returns>delta ColdVrn1 representing the additional Vrn1 expression from cold upregulation</returns>
        private double CalcpVrn2(double LPp, double IpVrn2, double DpVrn2)
        {
            double InitPhaseLength = 1.0;
            if (LPp < InitPhaseLength)
            {
                double InitSlope = (IpVrn2 + DpVrn2) / InitPhaseLength;
                return LPp * InitSlope;
            }
            else
                return IpVrn2 + LPp * DpVrn2;
        }

        /// <summary>
        /// Haun stage timing of terminal spikelet.
        /// Inverts equation 5 from Brown etal 2013 FLN =  2.85 + 1.1*TSHS and converts it to Base Phyllochrons
        /// Note the intercept differs, was type on publication
        /// </summary>
        /// <param name="FLN">Final leaf number observed</param>
        /// <param name="IntFLNvsTSHS">Intercept of relationship between FLN and TSHS</param>
        /// <returns>Estimation of number of HaunStages to terminal spikelet</returns>
        public double calcTSHS(double FLN, double IntFLNvsTSHS)
        {
            return (FLN - IntFLNvsTSHS) / 1.1;
        }

        // Class constants, assumed the same for all cultivars
        /// <summary>Temperature response coefficient for vernalisation</summary>
        public double k { get { return -0.17; } }
        /// <summary>Base delta upregulation of Vrn2 at short Pp</summary>
        private double baseDVrn2 { get { return 0.0; } }
        /// <summary>The amount of methalated Vrn1 needed for vernalisation saturation to occur</summary>
        public double VrnSatThreshold { get { return 1.0; } }
        /// <summary>The amount of cold induced Vrn1 saturation required for methalation of cold Vrn1 to occur</summary>
        public double MethalationThreshold { get { return 0.5; } }
        /// <summary></summary>
        public double BaseDVrnX { get { return 0.0; } }


        // Development state variables
        /// <summary>IsImbibed True if seed is sown and moisture in soil sufficient to start germination</summary>
        private bool isImbibed { get; set; } = false;
        /// <summary>IsMethalating True if Vrn1 expression equals TargetVrn1, the cold response will start methalating</summary>
        private bool isMethalating { get; set; }
        /// <summary>IsEmerged is True if seed has emerged</summary>
        private bool isEmerged { get; set; } = false;
        /// <summary>IsVernCompetent True when the plant is large enough to proceed to vrn saturation</summary>
        private bool isVernalised { get; set; }
        /// <summary>IsInduced True when floral initiation occurs, when Vrn3 > 0.3 </summary>
        private bool isInduced { get; set; }
        /// <summary>IsReproductive True when terminal spikelet occurs, when Vrn3 >= 1.0 </summary>
        private bool isReproductive { get; set; }
        /// <summary>IsAtFlagLeaf True when flag leaf ligule has emerged</summary>
        private bool isAtFlagLeaf { get; set; }

        /// <summary></summary>
        [JsonIgnore] public bool IsVernalised { get { return isVernalised; }}

        /// <summary>Long photoperiod Haunstage accumulation.</summary>
        [JsonIgnore] public double LPp { get; private set; }

        /// Vrn gene expression state variables
        /// <summary>The current expression of Vrn1 upregulated at base rate.  
        /// Is methalated each day so always accumulates.
        /// Provides mechanism for gradual vernalisation at warm temperatures</summary>
        [JsonIgnore] public double BaseVrn1 { get; private set; }
        /// <summary>The current expression of Vrn1 upregulated by cold.  
        /// Is methalated when Vrn1 reaches Vrn1Target, 
        /// Downregulated to keep Vrn1 at target level
        /// Downregulated by exposure to temperatures > 20oC.
        /// Provides mechanism for acellerated vernalisatin under cold temperatures</summary>
        [JsonIgnore] public double ColdVrn1 { get; private set; }
        /// <summary>The current expression of Vrn1 that has been methalated
        /// This is persistant expression than can not be down regulated
        /// BaseDVrn1 methalated each day
        /// ColdDVrn1 methalated each day when Vrn1 reaches Vrn1Target.
        /// Provides mechanism for vernalisation lag.  Vern expression lost if cold exposure not long enough</summary>
        [JsonIgnore] public double MethColdVrn1 { get; private set; }
        /// <summary>The current expression of all Vrn1
        /// Sum of MethVrn1 and ColdVrn1</summary>
        [JsonIgnore] public double Vrn1 { get; private set; }
        /// <summary>The current expression of Vrn2
        /// Incremented daily by dVrn2 when plant is competent 
        /// Assumes zero upregulation under short photoperiod,
        /// upregulation under long photoperiod,
        /// not down regulated under any conditions</summary>
        [JsonIgnore] public double Vrn2 { get; private set; }
        /// <summary>The current expression of Vrn3
        /// Incremented daily by dVrn3 when plant is Ppcompetent 
        /// Assumes upregulated at BaseDVrn3 under short photoperiod,
        /// Additional upregulation under long photoperiod,
        /// not down regulated under any conditions</summary>
        [JsonIgnore] public double Vrn3 { get; private set; }
        /// <summary>The current expression of VrnX
        /// Incremented daily by dVrnX when plant is Ppcompetent 
        /// Assumes upregulated at BaseDVrn3 under short photoperiod,
        /// Additional upregulation under long photoperiod,
        /// not down regulated under any conditions</summary>
        [JsonIgnore] public double VrnX { get; private set; }
        /// <summary>Long photoperiod dHS equal dHS at Pp > 16 and is a decreasing proporiton of dHS
        /// as Pp decreases reaching zero at 8hPp </summary>
        [JsonIgnore] public double dLPp { get; set; }
        /// <summary>daily delta upregulation of BaseVrn1</summary>
        [JsonIgnore] public double dBaseVrn1 { get; set; }
        /// <summary>daily delta upregulation of ColdVrn1</summary>
        [JsonIgnore] public double dColdVrn1 { get; set; }
        /// <summary>daily delta methalation of ColdVrn1</summary>
        [JsonIgnore] public double dMethColdVrn1 { get; set; }
        /// <summary>Potential upregulation of Vrn2</summary>
        [JsonIgnore] public double pVrn2 { get; set; }
        /// <summary>daily delta upregulation of Vrn3</summary>
        [JsonIgnore] public double dVrn3 { get; set; }
        /// <summary>daily delta upregulation of VrnX</summary>
        [JsonIgnore] public double dVrnX { get; set; }

        /// <summary>daily delta Haun stage, proxy for tt and should be refactored out</summary>
        [JsonIgnore] public double dHS { get; set; }

        /// Leaf number variables
        /// <summary>Haun stage of Vernalisation saturation</summary>
        [JsonIgnore] public double VSHS { get; private set; }
        /// <summary>Haun stage of Floral Initiation</summary>
        [JsonIgnore] public double FIHS { get; private set; }
        /// <summary>Haun Stage of Terminal Spikelet</summary>
        [JsonIgnore] public double TSHS { get; private set; }
        /// <summary>Final Leaf Number</summary>
        [JsonIgnore] public double FLN { get; private set; }

        /// <summary>Proportion of emergence day that it not used for emergence</summary>
        [JsonIgnore]
        public double PropnOfDay { get; private set; }

        /// <summary>Vernalisation rate parameters for current cultivar</summary>
        [JsonIgnore] public CultivarRateParams Params { get; set; }

        /// <summary>The ancestor CAMP model and some relations</summary>
        [Link(Type = LinkType.Path, Path = "[Phenology].Phyllochron.BasePhyllochron")]
        IFunction basePhyllochron = null;

        //[Link(Type = LinkType.Path, Path = "[Phenology].PhyllochronPpSensitivity")]
        //IFunction phylloPpSens = null;

        [Link(Type = LinkType.Path, Path = "[Phenology].HaunStage.Delta")]
        IFunction DHS = null;


        [EventSubscribe("PrePhenology")]
        private void OnPrePhenology(object sender, EventArgs e)
        {
            if ((isImbibed==true) && (isAtFlagLeaf == false))
            {
                ZeroDeltas();

                // Calculate daily Haun Stage changes
                // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                if (isEmerged == false)
                { //Crop not yet emerged but we still need a dHS value to drive Vrn1 expression prior to emergence
                    double EmergeDurationFactor = 1.0;
                    if (phenology.AccumulatedTT > 90) //Calculate EmergenceDurationFactor to slow accumulation of BP if emergence is taking a long time.  This slows Vrn1 expression under slow emergence and strange responses to delayed sowing
                        EmergeDurationFactor = Math.Exp(-0.015 * (phenology.AccumulatedTT - 90));
                    dHS = tt.Value() / basePhyllochron.Value() * EmergeDurationFactor;
                    dLPp = 0;
                }
                else
                { // Crop emerged
                    dHS = DHS.Value();
                    // Calculate delta long photoperiod thermal time
                    dLPp = dHS * PpResponse.Value() * PropnOfDay;
                }

                LPp += dLPp;

                // Calculate Vrn gene expression
                if (isVernalised == false) // do vernalisation calculations if crop not yet vernalised
                {
                    VSHS = haunStage.Value();
                    dBaseVrn1 = CalcBaseUpRegVrn1(dHS, Params.BaseDVrn1);
                    dColdVrn1 = DailyColdVrn1.Value();
                    ColdVrn1 = Math.Max(0.0, ColdVrn1 + dColdVrn1);

                    // Calculate daily methalation
                    if ((ColdVrn1 >= MethalationThreshold) && (MethColdVrn1 < Params.MaxMethColdVern1)) // ColdVrn1 expressed to threshold required for methalation to occur
                    { isMethalating = true; }
                    else
                    { isMethalating = false; }
                    
                    if (isMethalating == true)
                    {
                        dMethColdVrn1 = Math.Min(ColdVrn1 - MethalationThreshold,
                                                Math.Max(0.0, dColdVrn1));
                    }

                    // Calcualte expression of photoperiod sensitive genes
                    dVrnX = 0.0;
                    if (isEmerged == true)  // Photoperiod sensitive genes only express after emergence
                    {
                        if (MethColdVrn1 == 0.0)  // VrnX expression only occurs if no methalation of Vrn1 has occured
                        {
                            dVrnX = CalcdPPVrn(BaseDVrnX, Params.MaxDVrnX, dHS);
                        }
                        pVrn2 = CalcpVrn2(LPp, Params.MaxIpVrn2, Params.MaxDpVrn2);
                    }
                }

                // Increment todays Vrn expression values using deltas just calculated
                VrnX += dVrnX;
                BaseVrn1 += dBaseVrn1;
                MethColdVrn1 += dMethColdVrn1;
                
                // Effective expression of Vrn1 is the sum of baseVrn1, MethalatedVrn1 and Vrnx expression
                Vrn1 += (dMethColdVrn1 + dBaseVrn1 + dVrnX);
                
                // Effective Vrn2 expression is the potential expression less that which is blocked by Vrn1
                Vrn2 = Math.Max(0.0, pVrn2 - Vrn1);

                // Workout if Vernalisation is complete
                // Vernalisation saturation occurs when Vrn1 > the vernalisation threshold and Vrn2 expression is zero
                if ((isEmerged == true) && (Vrn1 >= VrnSatThreshold) && (Vrn2 == 0) && (isVernalised == false))
                {
                    isVernalised = true;
                }
                    
                // Then work out Vrn3 expression
                if ((isVernalised == true) && (isReproductive == false))
                dVrn3 = CalcdPPVrn(Params.BaseDVrn3, Params.MaxDVrn3, dHS);
                Vrn3 = Math.Min(1.0, Vrn3 + dVrn3);

                // Then work out phase progression based on Vrn expression
                if ((Vrn3 >=  0.3) && (isInduced == false))
                    isInduced = true;
                if ((Vrn3 >= 1.0) && (isReproductive == false))
                    isReproductive = true;
                if (isInduced == false)
                    FIHS = haunStage.Value();
                if (isReproductive == false)
                {
                    TSHS = haunStage.Value();
                    FLN = Params.IntFLNvsTSHS + 1.1 * TSHS;
                }

                //Finally work out if Flag leaf has appeared.
                if (haunStage.Value() >= FLN)
                    isAtFlagLeaf = true;
                PropnOfDay = 1.0;
            }
        }

        [EventSubscribe("SeedImbibed")]
        private void OnSeedImbibed(object sender, EventArgs e)
        {
            isImbibed = true;
        }

        [EventSubscribe("PlantEmerged")]
        private void OnPlantEmerged(object sender, EventArgs e)
        {
            isEmerged = true;
            PropnOfDay = (phenology.thermalTime.Value() - phenology.AccumulatedEmergedTT) / phenology.thermalTime.Value();
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters data)
        {
            Reset();
            Params = calcCAMPVrnRates.CalcCultivarParams(FLNparams, EnvData);
            summary.WriteMessage(this, "The following FLN parameters were used for " + data.Cultivar, MessageType.Diagnostic);
            summary.WriteMessage(this, "FLN LV = " + FLNparams.LV.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "FLN SV = " + FLNparams.SV.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "FLN LN = " + FLNparams.LN.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "FLN SN = " + FLNparams.SN.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "The following Vrn expression rate parameters have been calculated", MessageType.Diagnostic);
            summary.WriteMessage(this, "BaseDVrn1 = " + Params.BaseDVrn1.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "MaxDVrn1  = " + Params.MaxDVrn1.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "MaxIpVrn2 = " + Params.MaxIpVrn2.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "MaxDpVrn2 = " + Params.MaxDpVrn2.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "BaseDVrn3 = " + Params.BaseDVrn3.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "MaxDVrn3  = " + Params.MaxDVrn3.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "MaxDVrnX  = " + Params.MaxDVrnX.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "IntFLNvsTSHS     = " + Params.IntFLNvsTSHS.ToString(), MessageType.Diagnostic);
        }

        /// <summary>
        /// Called externally to recalculate phenology parameters
        /// </summary>
        /// <param name="overRideFLNParams"></param>
        public void ResetVernParams(FinalLeafNumberSet overRideFLNParams)
        {
            Params = calcCAMPVrnRates.CalcCultivarParams(overRideFLNParams, EnvData);
        }

        /// <summary>Resets the phase.</summary>
        public void Reset()
        {
            isImbibed = false;
            isMethalating = false;
            isEmerged = false;
            isVernalised = false;
            isInduced = false;
            isReproductive = false;
            isAtFlagLeaf = false;
            BaseVrn1 = 0;
            ColdVrn1 = 0;
            MethColdVrn1 = 0;
            Vrn1 = 0;
            Vrn2 = 0;
            Vrn3 = 0;
            VrnX = 0;
            FIHS = 0;
            TSHS = 0;
            FLN = 2.86;
            LPp = 0;
            ZeroDeltas();
            //HS = 0;
        }
        private void ZeroDeltas()
        {
            dHS = 0;
            dBaseVrn1 = 0;
            dColdVrn1 = 0;
            dMethColdVrn1 = 0;
            pVrn2 = 0.0;
            dVrn3 = 0.0;
            dVrnX = 0.0;
        }
    }
}
