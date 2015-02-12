using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Molecular phenology model
    /// </summary>
    [Serializable]
    public class MolecularPhenology
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The structure</summary>
        [Link]
        Structure Structure = null;

        /// <summary>The plant</summary>
        [Link]
        Plant Plant = null;

        //[Link] Function ThermalTime = null;
        //[Link] Function PhotoperiodFunction = null;
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
        public double AccumulatedVernalisation = 0;


        //Set up class variables
        
        //double AccumTt = 0;
        
        //double Vrn1Lag = 0;

        /// <summary>The VRN1</summary>
        double Vrn1 = 0;

        /// <summary>The VRN2</summary>
        double Vrn2 = 0;

        /// <summary>The VRN3</summary>
        double Vrn3 = 0;

        /// <summary>The VRN4</summary>
        double Vrn4 = 0;

        /// <summary>The VRN1 target</summary>
        double Vrn1Target = 0;
        
        //double Pp = 0;

        /// <summary>The tt</summary>
        double Tt = 0;
        
        //double MeanT = 0;
        
        //double HaunStageYesterday = 0;

        /// <summary>The delta haun stage</summary>
        double DeltaHaunStage = 0;

        /// <summary>The fihs</summary>
        double FIHS = 0;

        /// <summary>The TSHS</summary>
        double TSHS = 0;

        /// <summary>The FLN</summary>
        double FLN = 0;
        //
        //bool IsGerminated = false;

        /// <summary>The is pre vernalised</summary>
        bool IsPreVernalised = false;

        /// <summary>The is vernalised</summary>
        bool IsVernalised = false;

        /// <summary>The is induced</summary>
        bool IsInduced = false;

        /// <summary>The is reproductive</summary>
        bool IsReproductive = false;

        /// <summary>The VRN rate at0</summary>
        public double VrnRateAt0 = 1.6;
        /// <summary>The VRN rate at30</summary>
        public double VrnRateAt30 = 0.08;
        /// <summary>The VRN rate curve</summary>
        public double VrnRateCurve = -0.19;
        /// <summary>The base VRN1 target</summary>
        public double BaseVrn1Target = 0.74;

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
            if (Plant.IsAlive)
            {
                if (Phenology.CurrentPhaseName == "Emerging")
                    DeltaHaunStage = Tt / 90; //Fixme, need to do something better than this
                else
                    DeltaHaunStage = Structure.DeltaNodeNumber;

                //Pre-Vernalisation lag, determine the repression of Vrn4
                if (IsPreVernalised == false)
                {
                    Vrn4 -= Vrn1rate.Value * DeltaHaunStage;
                    Vrn4 = Math.Max(Vrn4, 0.0);
                    if (Vrn4 == 0.0)
                        IsPreVernalised = true;
                }

                //Vernalisation, determine extent of Vrn1 expression when Vrn 4 is suppressed
                if ((IsPreVernalised) && (IsVernalised == false))
                {
                    Vrn1 += Vrn1rate.Value * DeltaHaunStage;
                    Vrn1 = Math.Min(1.0, Vrn1);
                }

                //Update Vernalisation target to reflect photoperiod conditions and determine Vernalisation status
                if ((IsVernalised == false) && (Vrn1Target <= 1.0))
                {
                    if (Structure.MainStemNodeNo >= 1.1)
                    {
                        Vrn2 += Vrn2rate.Value * DeltaHaunStage;
                        Vrn1Target = Math.Min(1.0, BaseVrn1Target + Vrn2);
                    }
                    if (Vrn1 >= Vrn1Target)
                        IsVernalised = true;
                }
                //If Vernalisation is complete begin expressing Vrn3
                if ((IsVernalised) && (IsReproductive == false))
                {
                    Vrn3 += Vrn3rate.Value * DeltaHaunStage;
                    Vrn3 = Math.Min(1.0, Vrn3);
                }

                //Set timings of floral initiation, terminal spiklet and FLN in response to Vrn3 expression
                if ((Vrn3 >= 0.3) && (IsInduced == false))
                {
                    IsInduced = true;
                    FIHS = Structure.MainStemNodeNo;
                }
                if ((Vrn3 >= 1.0) && (IsReproductive == false))
                {
                    IsReproductive = true;
                    TSHS = Structure.MainStemNodeNo + 1.0;
                    FLN = 2.86 + 1.1 * TSHS;
                }
            }
        }
    }
}
