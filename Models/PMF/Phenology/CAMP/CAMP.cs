using Models.Core;
using Models.Functions;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Xml.Serialization;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Final Leaf Number observations (or estimates) for genotype from specific environmental conditions
    /// </summary>
    [Serializable]
    [Description("")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CAMP))]
    public class FinalLeafNumberSet : Model
    {
        /// <summary>Final Leaf Number when fully vernalised before HS1.1 and then grown in >16h Pp</summary>
        [Description("Final Leaf Number when fully vernalised before HS1.1 and then grown in >16h Pp</summary")]
        public double LV { get; set; }
        /// <summary>Final Leaf Number when fully vernalised before HS1.1 and then grown in >8h Pp</summary>
        [Description("Final Leaf Number when fully vernalised before HS1.1 and then grown in >8h Pp")]
        public double SV { get; set; }
        /// <summary>Final Leaf Number when grown at >20oC in >16h Pp</summary>
        [Description("Final Leaf Number when grown at >20oC in >16h Pp")]
        public double LN { get; set; }
        /// <summary>Final Leaf Number when grown at > 20oC in 8h Pp</summary>
        [Description("Final Leaf Number when grown at > 20oC in 8h Pp")]
        public double SN { get; set; }
    }


    /// <summary>
    /// Upregulation of Vrn1 from cold.  Is additional to base vrn1.
    /// BaseDVrn1 in seperate calculation otherwise te same as Brown etal 2013
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IFunction))]
    public class ColdUpRegVrn1 : Model, IFunction, IIndexedFunction
    {

        [Link(ByName = true, Type = LinkType.Ancestor)]
        CAMP camp = null;

        /// <summary> The k factor controls the shape of the exponential decline of vernalisation with temperature </summary>
        [Description("The exponential shape function")]
        public double k { get; set; }

        /// <summary> The temperature above which Vrn1 is down regulated </summary>
        [Description("The temperature above which Vrn1 is down regulated")]
        public double DeVernalisationTemp { get; set; }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Cannot call Value on XYPairs function. Must be indexed.</exception>
        public double Value(int arrayIndex = -1)
        {
            throw new Exception("Cannot call Value onColdUpRegVrn1 function. Must be indexed.");
        }

        /// <summary>Values the indexed.</summary>
        /// <param name="dX">The d x.</param>
        /// <returns></returns>
        public double ValueIndexed(double dX)
        {
            if (camp.Params != null)
            {
                double dHS = camp.dHS / 24;  //divide by 24 to make hourly
                double UdVrn1 = camp.Params.MaxDVrn1 * Math.Exp(k * dX);
                if (dX < DeVernalisationTemp)
                    return UdVrn1 * dHS;
                else
                    return -10 * dHS;
            }
            else return 0.0;
        }
    }

    /// <summary>
    /// Development Gene Expression
    /// </summary>
    [Serializable]
    [Description("")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class CAMP : Model, IVrn1Expression
    {
        // Other Model dependencies links
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction tt = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction pp = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction deltaHaunStage = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction haunStage = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction DailyColdVrn1 = null;

        [Link(Type = LinkType.Child, ByName = true)]
        CalcCAMPVrnRates calcCAMPVrnRates = null;

        // Cultivar specific Phenology parameters
        [Link(Type = LinkType.Child, ByName = true)]
        FinalLeafNumberSet FLNparams = null;

        /// <summary>
        /// Calculate delta of upregulation for photo period (Pp) sensitive genes
        /// </summary>
        /// <param name="Pp">Photoperiod</param>
        /// <param name="baseUR">dVrn/HS below 8h Pp</param>
        /// <param name="maxUR">dVrn/HS above 16h Pp</param>
        /// <param name="dHS">delta haun stage</param>
        /// <returns></returns>
        private double CalcdPPVrn(double Pp, double baseUR, double maxUR, double dHS)
        {
            if (Pp <= 8.0)
                return baseUR * dHS;
            else if ((Pp > 8.0) && (Pp < 16.0))
                return (baseUR + (maxUR * (Pp - 8) / (16 - 8))) * dHS;
            else // (Pp >= 16.0)
                return maxUR * dHS;
        }

        /// <summary>
        /// Calculate upregulation of base Vrn1
        /// </summary>
        /// <param name="Tt">Thermal time</param>
        /// <param name="dHS">delta haun stage</param>
        /// <param name="BaseDVrn1">delta Vrn1/HS at non-vernalising temperatures</param>
        /// <returns></returns>
        private double CalcBaseUpRegVrn1(double Tt,double dHS, double BaseDVrn1)
        {
            if (Tt < 0)
                BaseDVrn1 = 0;
            return BaseDVrn1 * dHS;
        }

        /// <summary>
        /// Haun stage timing of terminal spikelet.
        /// Inverts equation 5 from Brown etal 2013 FLN =  2.85 + 1.1*TSHS
        /// Note the intercept differs, was type on publication
        /// </summary>
        /// <param name="FLN">Final leaf number observed</param>
        /// <returns></returns>
        public double calcTSHS(double FLN)
        {
            return (FLN - 2.85) / 1.1;
        }

        // Class constants, assumed the same for all cultivars
        /// <summary>Temperature response coefficient for vernalisation</summary>
        public double k { get { return -0.19; } }
        /// <summary>Base delta upregulation of Vrn2 at short Pp</summary>
        private double baseDVrn2 { get { return 0.0; } }
        /// <summary>The amount of methalated Vrn1 needed for vernalisation saturation to occur</summary>
        public double VrnSatThreshold { get { return 1.0; } }
        /// <summary>The amount of cold induced Vrn1 saturation required for methalation of cold Vrn1 to occur</summary>
        public double MethalationThreshold { get { return 1.375; } }
        /// <summary></summary>
        public double BasePhyllochron { get { return 120; } }
        /// <summary></summary>
        public double PpCompetenceHS { get { return 1.1; } }
        /// <summary></summary>
        public double BaseDVrnX { get { return 0.0; } }


        // Development state variables
        /// <summary>IsImbibed True if seed is sown and moisture in soil sufficient to start germination</summary>
        private bool isImbibed { get; set; } = false;
        /// <summary>IsMethalating True if Vrn1 expression equals TargetVrn1, the cold response will start methalating</summary>
        private bool isMethalating { get; set; }
        /// <summary>IsEmerged is True if seed has emerged</summary>
        private bool isEmerged { get; set; } = false;
        /// <summary>IsPpCompetent True when HS>1.1, the plant is large enough to sense and respond to Pp stimulus</summary>
        private bool isPpCompetent { get; set; }
        /// <summary>IsVernCompetent True when the plant is large enough to proceed to vrn saturation</summary>
        private bool isVernCompetent { get; set; }
        /// <summary>isVernalisaed True when Methalated Vrn1 reaches Vrn1Target, enables occurance of floral initiation and terminal spikelet</summary>
        private bool isVernalised { get; set; }
        /// <summary>IsInduced True when floral initiation occurs, when Vrn3 > 0.3 </summary>
        private bool isInduced { get; set; }
        /// <summary>IsReproductive True when terminal spikelet occurs, when Vrn3 >= 1.0 </summary>
        private bool isReproductive { get; set; }
        /// <summary>IsAtFlagLeaf True when flag leaf ligule has emerged</summary>
        private bool isAtFlagLeaf { get; set; }

        /// <summary></summary>
        [JsonIgnore] public bool IsVernalised { get { return isVernalised; }}


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
        [JsonIgnore] public double MethVrn1 { get; private set; }
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
        /// <summary>daily delta upregulation of BaseVrn1</summary>
        [JsonIgnore] public double dBaseVrn1 { get; set; }
        /// <summary>daily delta upregulation of ColdVrn1</summary>
        [JsonIgnore] public double dColdVrn1 { get; set; }
        /// <summary>daily delta methalation of ColdVrn1</summary>
        [JsonIgnore] public double dMethColdVrn1 { get; set; }
        /// <summary>daily delta upregulation of Vrn2</summary>
        [JsonIgnore] public double dVrn2 { get; set; }
        /// <summary>daily delta upregulation of Vrn3</summary>
        [JsonIgnore] public double dVrn3 { get; set; }
        /// <summary>daily delta upregulation of VrnX</summary>
        [JsonIgnore] public double dVrnX { get; set; }
        /// <summary>Factor for accumulated exposure to short days</summary>
        [JsonIgnore] public double SDDurationFactor { get; set; }

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

        private double Vrn1atVS { get; set; }

        /// <summary>Vernalisation rate parameters for current cultivar</summary>
        [JsonIgnore] public CultivarRateParams Params { get; set; }

        [EventSubscribe("PrePhenology")]
        private void OnPrePhenology(object sender, EventArgs e)
        {
            if ((isImbibed==true) && (isAtFlagLeaf == false))
            {
                ZeroDeltas();
                

                if (isEmerged == false)
                    dHS = tt.Value()/ (BasePhyllochron * 0.75);
                else
                    dHS = deltaHaunStage.Value(); 
                
                if ((haunStage.Value() >= Params.VernCompetenceHS) && (isVernCompetent == false))
                    isVernCompetent = true;

                if ((haunStage.Value()>= PpCompetenceHS)&& (isPpCompetent == false))
                    isPpCompetent = true;

                // Work out base, cold induced Vrn1 expression and methalyation until vernalisation is complete
                if (isVernalised == false)
                {
                    VSHS = haunStage.Value();
                    dBaseVrn1 = CalcBaseUpRegVrn1(tt.Value(), dHS, Params.BaseDVrn1);
                    dColdVrn1 = DailyColdVrn1.Value();
                    
                    // If Vrn1(base + cold) equals Vrn1Target methalate coldVrn1.  BaseVrn1 is all methalated every day
                    if (isMethalating ==true)
                        dMethColdVrn1 = Math.Max(0,Math.Min(dColdVrn1, ColdVrn1));
                    // Work out VrnX expression
                    dVrnX = 0;
                    if ((isPpCompetent == true) && (isVernalised == false))
                        dVrnX = CalcdPPVrn(pp.Value(), BaseDVrnX, Params.MaxDVrnX,dHS);
                    VrnX += dVrnX;
                    // Increment Vnr1 expression state variables
                    BaseVrn1 += dBaseVrn1;
                    MethVrn1 += (dMethColdVrn1 + dBaseVrn1 + dVrnX);
                    ColdVrn1 = Math.Max(0.0, ColdVrn1 + dColdVrn1 - dMethColdVrn1);
                    Vrn1 = MethVrn1 + ColdVrn1;
                }

                // Then work out Vrn2 expression 
                if ((isVernalised == false) && (isPpCompetent == true))
                {
                    SDDurationFactor = Math.Max(0.5, SDDurationFactor - (1 - CalcdPPVrn(pp.Value(), 0, 1, 1)) / 35);
                    dVrn2 = CalcdPPVrn(pp.Value(), baseDVrn2, Params.MaxDVrn2, 1) * SDDurationFactor;
                    Vrn2 = Math.Max(0, dVrn2 - MethVrn1);
                }
                 
                // Workout if methalation of cold response has started
                if ((Vrn1 >= MethalationThreshold) && (isMethalating == false))
                    isMethalating = true;

                // Work out if vernalisation is complete
                if ((MethVrn1 >= 1.0) && (Vrn2 ==0.0) && (isVernCompetent == true) && (isVernalised == false))
                {
                    isVernalised = true;
                    Vrn1atVS = Vrn1;
                    MethVrn1 = Vrn1;
                }

                // Then work out Vrn3 expression
                if ((isVernalised == true) && (isVernCompetent == true) && (isReproductive == false))
                    dVrn3 = CalcdPPVrn(pp.Value(), Params.BaseDVrn3, Params.MaxDVrn3, dHS);
                Vrn3 = Math.Min(1.0, Vrn3 + dVrn3);

                // Then add Vrn3 expression effects to Vrn1 upregulation
                Vrn1 += dVrn3;

                // Then work out phase progression based on Vrn expression
                if ((Vrn1 >= (Vrn1atVS + 0.3)) && (isInduced == false))
                    isInduced = true;
                if ((Vrn1 >= (Vrn1atVS + 1.0)) && (isReproductive == false))
                    isReproductive = true;
                if (isInduced == false)
                    FIHS = haunStage.Value();
                if (isReproductive == false)
                {
                    TSHS = haunStage.Value();
                    FLN = 2.86 + 1.1 * TSHS;
                }

                //Finally work out if Flag leaf has appeared.
                if (haunStage.Value() >= FLN)
                    isAtFlagLeaf = true;
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
        [EventSubscribe("Sowing")]
        private void OnSowing(object sender, EventArgs e)
        {
            Reset();
            Params = calcCAMPVrnRates.CalcCultivarParams(FLNparams, 90, 90, 1);
        }

        /// <summary>
        /// Called externally to recalculate phenology parameters
        /// </summary>
        /// <param name="overRideFLNParams"></param>
        public void ResetVernParams(FinalLeafNumberSet overRideFLNParams)
        {
            Params = calcCAMPVrnRates.CalcCultivarParams(overRideFLNParams, 90, 90, 1);
        }

        /// <summary>Resets the phase.</summary>
        public void Reset()
        {
            isImbibed = false;
            isMethalating = false;
            isEmerged = false;
            isPpCompetent = false;
            isVernCompetent = false;
            isVernalised = false;
            isInduced = false;
            isReproductive = false;
            isAtFlagLeaf = false;
            BaseVrn1 = 0;
            ColdVrn1 = 0;
            MethVrn1 = 0;
            Vrn1 = 0;
            Vrn2 = 0;
            Vrn3 = 0;
            VrnX = 0;
            FIHS = 0;
            TSHS = 0;
            FLN = 2.86;
            ZeroDeltas();
            Vrn1atVS = 100;
            SDDurationFactor = 1.0;
            //HS = 0;
        }
        private void ZeroDeltas()
        {
            dHS = 0;
            dBaseVrn1 = 0;
            dColdVrn1 = 0;
            dMethColdVrn1 = 0;
            dVrn2 = 0.0;
            dVrn3 = 0.0;
            dVrnX = 0.0;
        }
    }
}
