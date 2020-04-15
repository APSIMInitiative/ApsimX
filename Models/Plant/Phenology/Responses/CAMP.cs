using Models.Core;
using Models.Functions;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Xml.Serialization;

namespace Models.PMF.Phen
{
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
        [Link]
        Phenology phenology = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Tt = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Pp = null;

        // Phenology parameters
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction BasedVrn1 = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction MUdVrn1 = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction MUdVrn2 = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction BasedVrn3 = null;

        private double CalcdPPVrn(double Pp, double baseUR, double maxUR, double Tt)
        {
            if (Pp <= 8.0)
                return baseUR * Tt / 120;
            else if ((Pp > 8.0) && (Pp < 16.0))
                return (baseUR + (maxUR * (Pp - 8) / (16 - 8))) * Tt / 120;
            else // (Pp >= 16.0)
                return maxUR * Tt / 120;
        }

        private double CalcBaseUpRegVrn1(double Tt, double BasedVrn1)
        {
            if (Tt < 0)
                BasedVrn1 = 0;
            return BasedVrn1 * Tt / 120;
        }

        private double CalcColdUpRegVrn1(double Tt, double MUdVrn1, double k)
        {
            double UdVrn1 = MUdVrn1 * Math.Exp(k * Tt);
            if (Tt < 20)
                return UdVrn1 * Tt / 120;
            else
                return -1;
        }

        private double CalcDeltaMethColdVrn1(double ColdVrn1, double MethVrn1, double BaseVrn1, double URdVrn1)
        {
            double UnMethVrn1 = ColdVrn1 + MethVrn1 - BaseVrn1;
            if ((UnMethVrn1 < 0.6) || (URdVrn1 < 0))
                return 0;
            else
                return Math.Min(URdVrn1, ColdVrn1);
        }

        private double calcTSHS(double FLN)
        {
            return (FLN - 2.85) / 1.1;
        }

        // Class constants, assumed the same for all cultivars
        private double BasePhyllochron { get; set; } = 120;
        private double k { get; set; } = -0.2;
        private double BasedVrn2 { get; set; } = 0;
        private double MUdVrn3 { get; set; } = 0.33;

        // Development state variables
        private bool IsImbibed { get; set; } = false;
        /// <summary></summary>
        [JsonIgnore] public bool IsVernalised { get; set; } 
        private bool IsInduced { get; set; }
        private bool IsReproductive { get; set; } 
        private bool IsAtFlagLeaf { get; set; }

        /// Vrn gene expression parameters
        /// <summary></summary>
        [JsonIgnore] public double BaseVrn1 { get; private set; }
        /// <summary></summary>
        [JsonIgnore] public double ColdVrn1 { get; private set; }
        /// <summary></summary>
        [JsonIgnore] public double MethVrn1 { get; private set; }
        /// <summary></summary>
        [JsonIgnore] public double Vrn1 { get; private set; }
        /// <summary></summary>
        [JsonIgnore] public double Vrn2 { get; private set; }
        /// <summary></summary>
        [JsonIgnore] public double Vrn3 { get; private set; }
        /// <summary></summary>
        [JsonIgnore] public double Vrn1Target { get; private set; }
        /// <summary></summary>
        [JsonIgnore] public double dBaseVrn1 { get; set; }
        /// <summary></summary>
        [JsonIgnore] public double dColdURVrn1 { get; set; }
        /// <summary></summary>
        [JsonIgnore] public double dMethColdVrn1 { get; set; }
        /// <summary></summary>
        [JsonIgnore] public double dVrn2 { get; set; }
        /// <summary></summary>
        [JsonIgnore] public double dVrn3 { get; set; }

        /// Leaf number variables
        /// <summary></summary>
        [JsonIgnore] public double FIHS { get; private set; }
        /// <summary></summary>
        [JsonIgnore] public double TSHS { get; private set; }
        /// <summary></summary>
        [JsonIgnore] public double FLN { get; private set; }


        [EventSubscribe("PrePhenology")]
        private void OnPrePhenology(object sender, EventArgs e)
        {

            if ((IsImbibed==true) && (IsAtFlagLeaf == false))
            {
                double HS = (Apsim.Find(phenology, "HaunStage") as IFunction).Value();
                ZeroDeltas();

                // Work base, cold induced and methylated Vrn1 expression
                if (IsVernalised == false)
                {    // If methalated Vrn1 expression is less that target do base expression
                    if (MethVrn1 < Vrn1Target)
                        dBaseVrn1 = CalcBaseUpRegVrn1(Tt.Value(), BasedVrn1.Value());
                    // Calculate cold Vrn1 upregulation and methalation of this
                    dColdURVrn1 = CalcColdUpRegVrn1(Tt.Value(), MUdVrn1.Value(), k);
                    dMethColdVrn1 = CalcDeltaMethColdVrn1(ColdVrn1, MethVrn1, BaseVrn1, dColdURVrn1);
                    BaseVrn1 += dBaseVrn1;
                    MethVrn1 += (dMethColdVrn1 + dBaseVrn1);
                    ColdVrn1 = Math.Max(0.0, ColdVrn1 + dColdURVrn1 - dMethColdVrn1);
                    Vrn1 = MethVrn1 + ColdVrn1;
                    // Downregulate cold vrn1 expression if we are at Vrn1target
                    ColdVrn1 = Math.Max(0.0, ColdVrn1 - Math.Max(0.0, Vrn1 - Vrn1Target));
                }

                // Then work out Vrn2 expression 
                if ((IsVernalised == false) && (HS >= 1.1))
                    dVrn2 = CalcdPPVrn(Pp.Value(), BasedVrn2, MUdVrn2.Value(), Tt.Value());
                Vrn2 += dVrn2;
                Vrn1Target = 1.0 + Vrn2;

                // Then work out Vrn3 expression
                if ((IsVernalised == true) && (HS >= 1.1) && (IsReproductive == false))
                    dVrn3 = CalcdPPVrn(Pp.Value(), BasedVrn3.Value(), MUdVrn3, Tt.Value());
                Vrn3 = Math.Min(1.0, Vrn3 + dVrn3);

                // Then work out Vrn3 expression effects on  Vrn1 expression
                if (IsVernalised == true)
                    Vrn1 += dVrn3;

                // Then work out phase progression based on Vrn expression
                if ((MethVrn1 >= Vrn1Target) && (HS >= 1.1) && (IsVernalised == false))
                    IsVernalised = true;
                if ((Vrn1 >= (Vrn1Target + 0.3)) && (IsInduced == false))
                    IsInduced = true;
                if ((Vrn1 >= (Vrn1Target + 1.0)) && (IsReproductive == false))
                    IsReproductive = true;
                if (IsInduced == false)
                    FIHS = HS;
                if (IsReproductive == false)
                {
                    TSHS = HS;
                    FLN = 2.86 + 1.1 * TSHS;
                }

                //Finally work out if Flag leaf has appeared.
                if (HS >= FLN)
                    IsAtFlagLeaf = true;

            }
        }

        [EventSubscribe("SeedImbibed")]
        private void OnSeedImbibed(object sender, EventArgs e)
        {
            IsImbibed = true;
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            Reset();
        }

        /// <summary>Resets the phase.</summary>
        public void Reset()
        {
            IsImbibed = false;
            IsVernalised = false;
            IsInduced = false;
            IsReproductive = false;
            IsAtFlagLeaf = false;
            BaseVrn1 = 0;
            ColdVrn1 = 0;
            MethVrn1 = 0;
            Vrn1 = 0;
            Vrn2 = 0.0;
            Vrn3 = 0.0;
            FIHS = 0;
            TSHS = 0;
            FLN = 2.86;
            Vrn1Target = 1.0;
            ZeroDeltas();
        }
        private void ZeroDeltas()
        {
            dBaseVrn1 = 0;
            dColdURVrn1 = 0;
            dMethColdVrn1 = 0;
            dVrn2 = 0.0;
            dVrn3 = 0.0;
        }
    }
}
