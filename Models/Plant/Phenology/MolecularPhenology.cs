using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using System.Xml.Serialization;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Molecular phenology model
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Phenology))]
    public class MolecularPhenology : Model
    {
        /// <summary>The plant</summary>
        [Link]
        Plant Plant = null;

        /// <summary>
        /// 
        /// </summary>
        [Link(IsOptional = true)]
        public IFunction HaunStage = null;

        /// <summary>
        /// 
        /// </summary>
        [Link(IsOptional = true)]
        public IFunction BaseVrn1Target = null;

        /// <summary>
        /// 
        /// </summary>
        [Link(IsOptional = true)]
        public IFunction DeltaHaunStage = null;

        /// <summary>The vrn1rate</summary>
        [Link]
        IFunction Vrn1rate = null;
        /// <summary>The vrn2rate</summary>
        [Link]
        IFunction Vrn2rate = null;
        /// <summary>The vrn3rate</summary>
        [Link]
        IFunction Vrn3rate = null;


        /// <summary>The accumulated vernalisation</summary>
        [XmlIgnore]
        [Units("Vernal Units")]
        [Description("The relative progression to vernalisation saturation")]
        public double AccumulatedVernalisation { get; set; }

        /// <summary>The VRN1</summary>
        [XmlIgnore]
        [Units("relative to saturation")]
        [Description("The expression of Vrn1 relative to what is needed to cause reproductive comittment")]
        public double Vrn1 { get; set; }

        /// <summary>The VRN2</summary>
        [XmlIgnore]
        [Units("relative to saturation")]
        [Description("The expression of Vrn2")]
        public double Vrn2 { get; set; }

        /// <summary>The VRN3</summary>
        [XmlIgnore]
        [Units("relative to saturation")]
        [Description("The expression of Vrn3")]
        public double Vrn3 { get; set; }

        /// <summary>The VRN4</summary>
        [XmlIgnore]
        [Units("relative to saturation")]
        [Description("The expression of Vrn4")]
        public double Vrn4 { get; set; }

        /// <summary>The VRN1 target</summary>
        [XmlIgnore]
        [Units("relative to saturation")]
        [Description("The amount of Vrn1 needed to saturate vernalisation")]
        public double Vrn1Target { get; set; }

        /// <summary>The fihs</summary>
        [XmlIgnore]
        [Units("Liguals")]
        [Description("The HaunStage at which Floral initiation occurs")]
        public double FIHS { get; set; }

        /// <summary>The TSHS</summary>
        [XmlIgnore]
        [Units("Liguals")]
        [Description("The HaunStage at which Terminal spikelet occurs")]
        public double TSHS { get; set; }

        /// <summary>The FLN</summary>
        [XmlIgnore]
        [Units("Leaves")]
        [Description("The Number of main-stem leaves produces")]
        public double FLN { get; set; }
        
        /// <summary>The is pre vernalised</summary>
        bool IsPreVernalised = false;

        /// <summary>The is vernalised</summary>
        bool IsVernalised = false;

        /// <summary>The is induced</summary>
        bool IsInduced = false;

        /// <summary>The is reproductive</summary>
        bool IsReproductive = false;

        //Event procedures
        /// <summary>Called when [commencing].</summary>
        public void OnCommencing()
        {
            Vrn4 = 1.0;
            Vrn1Target = 0.74;
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (Plant.IsGerminated)
            {
                //Pre-Vernalisation lag, determine the repression of Vrn4
                if (IsPreVernalised == false)
                {
                    Vrn4 -= Vrn1rate.Value * DeltaHaunStage.Value;
                    Vrn4 = Math.Max(Vrn4, 0.0);
                    if (Vrn4 == 0.0)
                        IsPreVernalised = true;
                }

                //Vernalisation, determine extent of Vrn1 expression when Vrn 4 is suppressed
                if ((IsPreVernalised) && (IsVernalised == false))
                {
                    Vrn1 += Vrn1rate.Value * DeltaHaunStage.Value;
                    Vrn1 = Math.Min(1.0, Vrn1);
                }

                //Update Vernalisation target to reflect photoperiod conditions and determine Vernalisation status
                if ((IsVernalised == false) && (Vrn1Target <= 1.0))
                {
                    if (HaunStage.Value >= 1.1)
                    {
                        Vrn2 += Vrn2rate.Value * DeltaHaunStage.Value;
                        Vrn1Target = Math.Min(1.0, BaseVrn1Target.Value + Vrn2);
                    }
                    if (Vrn1 >= Vrn1Target)
                        IsVernalised = true;
                }
                //If Vernalisation is complete begin expressing Vrn3
                if ((IsVernalised) && (IsReproductive == false))
                {
                    Vrn3 += Vrn3rate.Value * DeltaHaunStage.Value;
                    Vrn3 = Math.Min(1.0, Vrn3);
                }

                //Set timings of floral initiation, terminal spiklet and FLN in response to Vrn3 expression
                if ((Vrn3 >= 0.3) && (IsInduced == false))
                {
                    IsInduced = true;
                    FIHS = HaunStage.Value;
                }
                if ((Vrn3 >= 1.0) && (IsReproductive == false))
                {
                    IsReproductive = true;
                    TSHS = HaunStage.Value + 1.0;
                    FLN = 2.86 + 1.1 * TSHS;
                }
            }
        }
    }
}
