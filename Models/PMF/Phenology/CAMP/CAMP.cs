using System;
using Models.Core;
using Models.Functions;
using Newtonsoft.Json;

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
    public class CAMP : Model, IVrnExpression
    {
        /// <summary>The summary</summary>
        [Link]
        private ISummary summary = null;

        // Other Model dependencies links
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction tt = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction ColdVrnResponse = null;

        [Link(Type = LinkType.Child, ByName = true)]
        CalcCAMPVrnRates calcCAMPVrnRates = null;

        // Cultivar specific Phenology parameters
        [Link(Type = LinkType.Child, ByName = true)]
        FinalLeafNumberSet FLNparams = null;

        // Cultivar specific Phenology parameters
        [Link(Type = LinkType.Child, ByName = true)]
        FLNParameterEnvironment EnvData = null;

        /// <summary>The Pp response shape function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction PpResponse = null;

        /// <summary>The ancestor CAMP model and some relations</summary>
        [Link(Type = LinkType.Ancestor, ByName = true)]
        Phenology phenology = null;

        /// <summary>The ancestor CAMP model and some relations</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction haunStage = null;

        /// <summary>
        /// Calculate delta of upregulation for photo period (Pp) sensitive genes
        /// </summary>
        /// <param name="baseUR">dVrn/HS below 8h Pp</param>
        /// <param name="maxUR">dVrn/HS above 16h Pp</param>
        /// <returns></returns>
        private double CalcdPPVrn(double baseUR, double maxUR)
        {
            return (baseUR + (maxUR - baseUR) * PpResponse.Value());
        }

        /// <summary>
        /// Haun stage timing of terminal spikelet.
        /// Inverts equation 5 from Brown etal 2013 FLN =  2.85 + 1.1*TSHS and converts it to Base Phyllochrons
        /// Note the intercept differs, was type on publication
        /// </summary>
        /// <param name="FLN">Final leaf number observed</param>
        /// <returns>Estimation of number of HaunStages to terminal spikelet</returns>
        public double calcTSHS(double FLN)
        {
            return (FLN - IntFLNvsTSHS) / SlopeFLNvsTS;
        }

        // Class constants, assumed the same for all cultivars
        /// <summary>Temperature response coefficient for vernalisation</summary>
        public double k { get { return -0.17; } }
        /// <summary>Base delta upregulation of Vrn2 at short Pp</summary>
        public double VSThreshold { get { return 1.0; } }
        /// <summary>The amount of methalated Vrn1 needed for vernalisation saturation to occur</summary>
        public double TSThreshold { get { return 2.0; } }
        /// <summary>The slope of the realationship between FLN an the HS of terminal spikelet</summary>
        public double SlopeFLNvsTS { get { return 1.1; } }
        /// <summary>The intercept of the relationship between FLN and the HS of terminal spikelet</summary>
        public double IntFLNvsTSHS { get { return 2.85; } }


        // Development state variables
        /// <summary>IsImbibed True if seed is sown and moisture in soil sufficient to start germination</summary>
        private bool isImbibed { get; set; } = false;
        /// <summary>IsEmerged is True if seed has emerged</summary>
        private bool isEmerged { get; set; } = false;
        /// <summary>IsVernCompetent True when the plant is large enough to proceed to vrn saturation</summary>
        private bool isVernalised { get; set; }
        /// <summary>IsReproductive True when terminal spikelet occurs, when Vrn3 >= 1.0 </summary>
        private bool isReproductive { get; set; }

        /// <summary></summary>
        [JsonIgnore] public bool IsGerminated { get { return isImbibed; } }
        /// <summary></summary>
        [JsonIgnore] public bool IsEmerged { get { return isEmerged; } }
        /// <summary></summary>
        [JsonIgnore] public bool IsVernalised { get { return isVernalised; } }
        /// <summary></summary>
        [JsonIgnore] public bool IsReproductive { get { return isReproductive; } }
        /// Vrn gene expression state variables
        /// <summary>The current expression of Vrn upregulated at base rate.  
        /// Is methalated each day so always accumulates.
        /// Provides mechanism for gradual vernalisation at warm temperatures</summary>
        [JsonIgnore] public double BaseVrn { get; private set; }
        /// <summary>The current expression of Vrn upregulated by cold.  
        /// Is methalated when Vrn1 reaches params.MethalationThreshold, 
        /// Downregulated by exposure to temperatures > 20oC.
        /// Provides mechanism for acellerated vernalisatin under cold temperatures</summary>
        [JsonIgnore] public double Cold { get; private set; }
        /// <summary>The sum of expression of all vrn genes
        /// BaseVrn + ColdVrn1 + Vrn3 - Vrn2</summary>
        [JsonIgnore] public double Vrn { get; private set; }
        /// <summary>The methalated Vrn1 expressed
        /// This is what gives persistant cold vernalisation response</summary>
        [JsonIgnore] public double Vrn1 { get; private set; }
        /// <summary>The current expression of Vrn2
        /// is zero under short photoperiod and increases under long photoperiod.  
        /// It represents a potential Vrn2 expression and representes the amount of Vrn
        /// that must be expressed to enable rapid progress toward vernalisation</summary>
        [JsonIgnore] public double Vrn2 { get; private set; }
        /// <summary>The current expression of Vrn3
        /// Incremented daily by dVrn3 when plant is Ppcompetent 
        /// Assumes zero upregulation under short photoperiod,
        /// upregulated under long photoperiod,
        /// not down regulated under any conditions</summary>
        [JsonIgnore] public double Vrn3 { get; private set; }
        /// <summary>Vrn3 expression (relative to baseVrn) due to long Pp</summary>
        [JsonIgnore] public double PpVrn3Fact { get; set; }
        /// <summary>Maximum potential Vrn expression</summary>
        [JsonIgnore] public double MaxVrn { get; private set; }
        /// <summary>Maximum potential Vrn expression</summary>
        [JsonIgnore] public double MaxVrn2 { get; private set; }
        /// <summary>daily delta methalation of Vrn1</summary>
        [JsonIgnore] public double dVrn { get; set; }
        /// <summary>daily delta upregulation of BaseVrn</summary>
        [JsonIgnore] public double dBaseVrn { get; set; }
        /// <summary>daily delta upregulation of Vrn1 due to cold</summary>
        [JsonIgnore] public double dCold { get; set; }
        /// <summary>daily delta methalation of Vrn1</summary>
        [JsonIgnore] public double dVrn1 { get; set; }
        /// <summary>daily delta upregulation of Vrn3</summary>
        [JsonIgnore] public double dVrn3 { get; set; }
        /// <summary>daily delta of Max Vrn expression</summary>
        [JsonIgnore] public double dMaxVrn { get; set; }
        /// <summary>daily delta Haun stage</summary>
        [JsonIgnore] public double dHS { get; set; }
        /// <summary>Photo period releative to max and min thresholds</summary>
        [JsonIgnore] public double RelPp { get; set; }
        /// <summary>Vernalisation releative to max and min temperatures</summary>
        [JsonIgnore] public double RelCold { get; set; }

        /// Leaf number variables
        /// <summary>Haun stage of Vernalisation saturation</summary>
        [JsonIgnore] public double VSHS { get; private set; }
        /// <summary>Haun Stage of Terminal Spikelet</summary>
        [JsonIgnore] public double TSHS { get; private set; }
        /// <summary>Final Leaf Number</summary>
        [JsonIgnore] public double FLN { get; private set; }

        /// <summary>Vernalisation rate parameters for current cultivar</summary>
        [JsonIgnore] public CultivarRateParams Params { get; set; }

        /// <summary>The ancestor CAMP model and some relations</summary>
        [Link(Type = LinkType.Child, ByName =true)]
        IFunction basePhyllochron = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction deltaHaunStage = null;


        [EventSubscribe("PrePhenology")]
        private void OnPrePhenology(object sender, EventArgs e)
        {
            if ((isImbibed == true) && (isReproductive == false))
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
                    RelPp = 0;
                    Vrn2 = 0;
                }
                else
                { // Crop emerged
                    dHS = deltaHaunStage.Value();
                }
                
                // Set stage specific parameter values
                // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                if (isVernalised == false) // do vernalisation calculations if crop not yet vernalised
                {
                    VSHS = haunStage.Value();
                    dBaseVrn = Params.BaseDVrnVeg * dHS;
                    dMaxVrn = Params.MaxDVrnVeg * dHS;
                    PpVrn3Fact = Params.PpVrn3FactVeg;
                }
                else
                {
                    if (isReproductive == false)
                    {
                        dBaseVrn = Params.BaseDVrnER * dHS;
                        dMaxVrn = Params.MaxDVrnER * dHS;
                        PpVrn3Fact = Params.PpVrn3FactER;
                    }
                }
                // Increment todays Vrn expression values using deltas just calculated
                BaseVrn += dBaseVrn;
                MaxVrn += dMaxVrn;

                // Calculate daily cold response
                // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                if (IsVernalised == false)
                {
                    double ColdYesterday = Cold;
                    RelCold = ColdVrnResponse.Value();
                    dCold = RelCold * dBaseVrn * Params.ColdVrn1Fact;
                    Cold = Math.Max(0.0, Cold + dCold);
                    if (Cold > Params.MethalationThreshold)
                    {
                        dVrn1 = Math.Max(0, (Cold - Params.MethalationThreshold) - (ColdYesterday - Params.MethalationThreshold));
                    }
                }
                // Increment Vrn 1
                Vrn1 += dVrn1;

                // Calcualte expression of photoperiod sensitive genes
                // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                if (isEmerged == true)  // Photoperiod sensitive genes only express after emergence
                {
                    RelPp = PpResponse.Value(); //relative Pp, scaled between 0 at lower threshold and 1 at upper threshold
                    if (RelPp == 0) //Reduce MaxVrn2 if short Pp encountered
                    {
                        MaxVrn2 = Math.Max(0,MaxVrn2-dBaseVrn);  // Fixme.  I don't think this should be here. I dont think is doing anything and can be removed
                    }
                    if (isVernalised == false) // Set Vrn2 relative to MaxVrn2 and current Pp
                    {
                        Vrn2 = MaxVrn2 * RelPp;
                    }
                    if ((BaseVrn + Vrn1) > Vrn2) // express vrn3 relative to Pp if Vrn2 is down regulated
                        dVrn3 = (PpVrn3Fact - 1) * RelPp * dBaseVrn;
                }
                // Increment Vrn3
                Vrn3 += dVrn3;

                // Increment todays Vrn expression values using deltas just calculated
                // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                double VrnYesterday = Vrn;
                if (isVernalised == false) //If not vernalised need to include Vrn2 and constrain to maxrate
                {
                    Vrn = Math.Min(MaxVrn, Math.Max(0, BaseVrn + Vrn1 + Vrn3 - Vrn2));
                }
                else
                {
                    Vrn = Vrn + dBaseVrn + dVrn3;
                }
                dVrn = Vrn - VrnYesterday;

                // Then work out phase progression based on Vrn expression
                // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                if ((isEmerged == true) && (Vrn >= VSThreshold) && (IsVernalised == false))
                    isVernalised = true;
                if (isVernalised == false)
                {
                    VSHS = haunStage.Value();
                }

                if ((isVernalised == true) && (Vrn >= TSThreshold) && (isReproductive == false))
                    isReproductive = true;
                
                TSHS = haunStage.Value();
                FLN = IntFLNvsTSHS + 1.1 * TSHS;
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
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters data)
        {
            Reset();
            Params = calcCAMPVrnRates.CalcCultivarParams(FLNparams, EnvData);
            MaxVrn2 = Params.MaxVrn2;
            summary.WriteMessage(this, "The following FLN parameters were used for " + data.Cultivar, MessageType.Diagnostic);
            summary.WriteMessage(this, "FLN LV = " + FLNparams.LV.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "FLN SV = " + FLNparams.SV.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "FLN LN = " + FLNparams.LN.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "FLN SN = " + FLNparams.SN.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "The following Vrn expression rate parameters have been calculated", MessageType.Diagnostic);
            summary.WriteMessage(this, "BaseDVrnVeg = " + Params.BaseDVrnVeg.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "MaxDVrnVeg  = " + Params.MaxDVrnVeg.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "BaseDVrnER = " + Params.BaseDVrnER.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "MaxDVrnER = " + Params.MaxDVrnER.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "PpVrn3FactER = " + Params.PpVrn3FactER.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "PpVrn3FactVeg  = " + Params.PpVrn3FactVeg.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "MaxVrn2  = " + Params.MaxVrn2.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "MethalationThreshold= " + Params.MethalationThreshold.ToString(), MessageType.Diagnostic);
            summary.WriteMessage(this, "ColdVrn1Fact     = " + Params.ColdVrn1Fact.ToString(), MessageType.Diagnostic);
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
            isEmerged = false;
            isVernalised = false;
            isReproductive = false;
            BaseVrn = 0;
            Vrn = 0;
            Cold = 0;
            Vrn1 = 0;
            Vrn2 = 0;
            Vrn3 = 0;
            TSHS = 0;
            FLN = 2.86;
            ZeroDeltas();
        }
        private void ZeroDeltas()
        {
            dHS = 0;
            dVrn = 0;
            dBaseVrn = 0;
            dCold = 0;
            dVrn1 = 0;
            dVrn3 = 0.0;
            dMaxVrn = 0;
        }
    }
}
