using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using System;


namespace Models.PMF.Phen
{

    /// <summary>
    /// Calculates the leaf appearance rate from photo thermal quotient
    /// </summary>
    [Serializable]
    [Description("")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IFunction))]
    public class PTQPhyllochron : Model, IFunction
    {
        // LAR parameters
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction maxLAR = null;
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction minLAR = null;
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction PTQhf = null;
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction PTQ = null;
        [Link(Type = LinkType.Ancestor, ByName = true)]
        Phenology phenology = null;

        LARPTQmodel lARPTQmodel = null;

        /// <summary>
        /// Apply leaf stage factors in calculating phyllochron
        /// </summary>
        [Description("Apply leaf stage factors in calculating phyllochron")]
        public bool ApplyLeafStageFactors { get; set; }

        private bool CropVernalised = false;
        private bool Phase1complete = false;
        private bool VernalisedInPhase1 = false;
        private bool Phase2complete = false;
        private bool VernalisedInPhase2 = false;

        private double maxLARFactor = 1.0;
        private double PTQhfFactor = 1.0;
        /// <summary>
        /// Name of event to swith Vernalisation Factor
        /// </summary>
        [Description("Name of event to swith Vernalisation Factor")]
        public string SetEvent { get; set; }

        /// <summary>
        /// The value
        /// </summary>
        /// <param name="arrayIndex"></param>
        /// <returns></returns>
        public double Value(int arrayIndex = -1)
        {
            if (ApplyLeafStageFactors)
            {
                double HS = phenology.FindChild<IFunction>("HaunStage").Value();

                //Phase1 transition
                if ((HS > 3.0) && (Phase1complete == false))
                {
                    Phase1complete = true;
                    if (CropVernalised == true)
                    {
                        VernalisedInPhase1 = true;
                    }
                }
                //Phase 2 transition
                if ((HS > 7.0) && (Phase2complete == false))
                {
                    Phase2complete = true;
                    if ((CropVernalised == true) && (VernalisedInPhase1 == false))
                    {
                        VernalisedInPhase2 = true;
                    }
                }

                // Phase 2 each day
                if ((HS > 3.0) && (HS < 7.0))
                    if (VernalisedInPhase1 == false)
                    {
                        maxLARFactor = 0.7;
                        PTQhfFactor = 0.5;
                    }
                // Phase 3 each day
                if (HS > 7.0)
                {
                    if ((VernalisedInPhase1 == true) || (VernalisedInPhase2 == true))
                    {
                        maxLARFactor = 0.7;
                        PTQhfFactor = 0.5;
                    }
                    else
                    {
                        maxLARFactor = 0.55;
                        PTQhfFactor = 0.25;
                    }
                }
            }
            if (lARPTQmodel != null)
            {
                double LAR = lARPTQmodel.CalculateLAR(PTQ.Value(), maxLAR.Value() * maxLARFactor, minLAR.Value(), PTQhf.Value() * PTQhfFactor);
                if (LAR > 0)
                    return 1 / LAR;
                else
                    return 0;
            }
            else
                return 0;
        }

        /// <summary>Called when crop is sown</summary>
        [EventSubscribe("Sowing")]
        private void OnSowing(object sender, EventArgs e)
        {
            lARPTQmodel = phenology.FindChild<LARPTQmodel>("LARPTQmodel");
            CropVernalised = false;
            Phase1complete = false;
            VernalisedInPhase1 = false;
            Phase2complete = false;
            VernalisedInPhase2 = false;
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == SetEvent)
            {
                CropVernalised = true;
            }
        }
    }
}
