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
    [Description("")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CAMP))]
    public class FinalLeafNumberSet : Model
    {
        /// <summary>Final Leaf Number when fully vernalised before HS1.1 and then grown in >16h Pp</summary>
        [Description("Final Leaf Number when fully vernalised before HS1.1 and then grown in >16h Pp</summary")]
        public double LV { get; set; }
        /// <summary>Final Leaf Number when fully vernalised before HS1.1 and then grown in >16h Pp</summary>
        [Description("Final Leaf Number when fully vernalised before HS1.1 and then grown in >16h Pp")]
        public double SV { get; set; }
        /// <summary>Final Leaf Number when grown at >20oC in >16h Pp</summary>
        [Description("Final Leaf Number when grown at >20oC in >16h Pp")]
        public double LN { get; set; }
        /// <summary>Final Leaf Number when grown at > 20oC in 8h Pp</summary>
        [Description("Final Leaf Number when grown at > 20oC in 8h Pp")]
        public double SN { get; set; }
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
        IFunction hs = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction dhs = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction basePhyllochron = null;

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
        /// Upregulation of Vrn1 from cold.  Is additional to base vrn1.
        /// BaseDVrn1 in seperate calculation otherwise te same as Brown etal 2013
        /// </summary>
        /// <param name="Tt">Thermal time</param>
        /// <param name="dHS">delta Haun stage</param>
        /// <param name="MUdVrn1">Maximum upregulation of Vrn1/HS (Notionaly at 0oC)</param>
        /// <param name="k">temperature response coefficient</param>
        /// <returns></returns>
        private double CalcColdUpRegVrn1(double Tt, double dHS, double MUdVrn1, double k)
        {
            double UdVrn1 = MUdVrn1 * Math.Exp(k * Tt);
            if (Tt < 20)
                return UdVrn1 * dHS;
            else
                return -1;
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
        private double baseDVrn2 { get { return 0; } }
        /// <summary>Maximum delta upregulation of Vrn3 at Long Pp</summary>
        private double maxDVrn3 { get { return 0.33; } }
        /// <summary>The haun stage at which the crop is able to detect and acto upon photoperiod stimuli, needed for vernalisation to occur</summary>
        public double CompetenceHS { get { return 1.1; } }

        // Development state variables
        /// <summary>IsImbibed True if seed is sown and moisture in soil sufficient to start germination</summary>
        private bool isImbibed { get; set; } = false;
        /// <summary>IsMethalating True if Vrn1 expression equals TargetVrn1, the cold response will start methalating</summary>
        private bool isMethalating { get; set; }
        /// <summary>IsEmerged is True if seed has emerged</summary>
        private bool isEmerged { get; set; } = false;
        /// <summary>IsCompetent True when HS>1.1, the plant is large enough to sense and respond to Pp stimulus</summary>
        private bool isCompetent { get; set; }
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
        /// <summary></summary>
        [JsonIgnore] public double BasePhyllochron { get { return basePhyllochron.Value(); } }

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
        /// <summary>This is the Target Vrn1 expression must reach for methalation to occur
        /// and Methalated Vrn1 expression must reach for vernalisation to occur.
        /// Equals 1 + Vrn2 expression
        /// Provides mechanism for long days to extend vernalisation response</summary>
        [JsonIgnore] public double Vrn1Target { get; private set; }
        /// <summary>The current expression of Vrn2
        /// Incremented daily by dVrn2 when plant is competent 
        /// Assumes zero upregulation under short photoperiod,
        /// upregulation under long photoperiod,
        /// not down regulated under any conditions</summary>
        [JsonIgnore] public double Vrn2 { get; private set; }
        /// <summary>The current expression of Vrn3
        /// Incremented daily by dVrn3 when plant is competent 
        /// Assumes upregulated at BaseDVrn3 under short photoperiod,
        /// Additional upregulation under long photoperiod,
        /// not down regulated under any conditions</summary>
        [JsonIgnore] public double Vrn3 { get; private set; }
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
        /// <summary>daily delta Haun stage</summary>
        [JsonIgnore] private double dHS { get; set; } 

        /// Leaf number variables
        /// <summary>Haun stage of Floral Initiation</summary>
        [JsonIgnore] public double FIHS { get; private set; }
        /// <summary>Haun Stage of Terminal Spikelet</summary>
        [JsonIgnore] public double TSHS { get; private set; }
        /// <summary>Final Leaf Number</summary>
        [JsonIgnore] public double FLN { get; private set; }

        private double Vrn1atVS { get; set; }

        private CultivarRateParams Params = null;


        [EventSubscribe("PrePhenology")]
        private void OnPrePhenology(object sender, EventArgs e)
        {
            if ((isImbibed==true) && (isAtFlagLeaf == false))
            {
                ZeroDeltas();

                if (isEmerged == false)
                    dHS = tt.Value() / basePhyllochron.Value() * 0.75; //dhs from phenology is incorrect here because photoperiod will be zero.
                else
                    dHS = dhs.Value();

                if ((hs.Value() >= CompetenceHS) && (isCompetent == false))
                    isCompetent = true;

                // Work out base, cold induced Vrn1 expression and methalyation until vernalisation is complete
                if (isVernalised == false)
                {    // If methalated Vrn1 expression is less that Vrn1Target do Vrn1 upregulation
                    if (MethVrn1 < Vrn1Target)
                    {
                        dBaseVrn1 = CalcBaseUpRegVrn1(tt.Value(), dHS, Params.BaseDVrn1);
                        dColdVrn1 = CalcColdUpRegVrn1(tt.Value(), dHS, Params.MaxDVrn1, k);
                    }
                    // If Vrn1(base + cold) equals Vrn1Target methalate coldVrn1.  BaseVrn1 is all methalated every day
                    if (isMethalating ==true)
                        dMethColdVrn1 = Math.Max(0,Math.Min(dColdVrn1, ColdVrn1));
                    // Increment Vnr1 expression state variables
                    BaseVrn1 += dBaseVrn1;
                    MethVrn1 += (dMethColdVrn1 + dBaseVrn1);
                    ColdVrn1 = Math.Max(0.0, ColdVrn1 + dColdVrn1 - dMethColdVrn1);
                    Vrn1 = MethVrn1 + ColdVrn1;
                }

                // Then work out Vrn2 expression 
                if ((isVernalised == false) && (isCompetent == true))
                    dVrn2 = CalcdPPVrn(pp.Value(), baseDVrn2, Params.MaxDVrn2, dHS);
                Vrn2 += dVrn2;
                Vrn1Target = 1.0 + Vrn2;

                // Workout if methalation of cold response has started
                if ((Vrn1 >= Vrn1Target) && (isMethalating == false))
                    isMethalating = true;

                // Then work out if vernalisation is complete
                if ((MethVrn1 >= Vrn1Target) && (isCompetent == true) && (isVernalised == false))
                {
                    isVernalised = true;
                    Vrn1atVS = MethVrn1;
                }

                // Downregulate Vrn1 expression if Vrn1Target has been reached
                if((isMethalating==true)&&(isVernalised==false)&&dColdVrn1>0)
                {
                    double dDRVrn1 = Vrn1 - Vrn1Target;
                    double dDRColdVrn1 = Math.Min(dDRVrn1, Math.Min(dColdVrn1 + dBaseVrn1, ColdVrn1));
                    ColdVrn1 -= dDRColdVrn1;
                }

                // Then work out Vrn3 expression
                if ((isVernalised == true) && (isCompetent == true) && (isReproductive == false))
                    dVrn3 = CalcdPPVrn(pp.Value(), Params.BaseDVrn3, maxDVrn3, dHS);
                Vrn3 = Math.Min(1.0, Vrn3 + dVrn3);

                // Then add Vrn3 expression effects to Vrn1 upregulation
                Vrn1 += dVrn3;

                // Then work out phase progression based on Vrn expression
                if ((Vrn1 >= (Vrn1atVS + 0.3)) && (isInduced == false))
                    isInduced = true;
                if ((Vrn1 >= (Vrn1atVS + 1.0)) && (isReproductive == false))
                    isReproductive = true;
                if (isInduced == false)
                    FIHS = hs.Value();
                if (isReproductive == false)
                {
                    TSHS = hs.Value();
                    FLN = 2.86 + 1.1 * TSHS;
                }

                //Finally work out if Flag leaf has appeared.
                if (hs.Value() >= FLN)
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
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            Reset();
            Params = calcCAMPVrnRates.CalcCultivarParams(FLNparams, 90, 90, 1);
        }

        /// <summary>Resets the phase.</summary>
        public void Reset()
        {
            isImbibed = false;
            isMethalating = false;
            isEmerged = false;
            isCompetent = false;
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
            FIHS = 0;
            TSHS = 0;
            FLN = 2.86;
            Vrn1Target = 1.0;
            ZeroDeltas();
            Vrn1atVS = 100;
        }
        private void ZeroDeltas()
        {
            dBaseVrn1 = 0;
            dColdVrn1 = 0;
            dMethColdVrn1 = 0;
            dVrn2 = 0.0;
            dVrn3 = 0.0;
        }
    }
}
