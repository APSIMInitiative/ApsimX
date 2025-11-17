using System;
using APSIM.Core;
using Models.Core;
using Models.Functions;

namespace Models.PMF.Phen
{

    /// <summary>
    /// Calculates the leaf appearance rate from photo thermal quotient
    /// </summary>
    [Serializable]
    [Description("")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IFunction))]
    public class PTQPhyllochron : Model, IFunction, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }


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

        private double HSFactor = 1.0;

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
                double HS = Structure.FindChild<IFunction>("HaunStage", relativeTo: phenology).Value();

                //Phase1 transition
                if ((HS > 3.0) && (Phase1complete == false))
                {
                    Phase1complete = true;
                    if (CropVernalised == true)
                    {
                        VernalisedInPhase1 = true;
                    }
                    else
                    {
                        HSFactor = 1.25;
                    }
                }
                //Phase 2 transition
                if ((HS > 7.0) && (Phase2complete == false))
                {
                    Phase2complete = true;
                    if ((CropVernalised == true) && (VernalisedInPhase1 == false))
                    {
                    }
                    else
                    {
                        HSFactor *= 1.35;
                    }
                }
            }

            if (lARPTQmodel != null)
            {
                double LAR = 1 / HSFactor * lARPTQmodel.CalculateLAR(PTQ.Value(), maxLAR.Value(), minLAR.Value(), PTQhf.Value());
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
            lARPTQmodel = Structure.FindChild<LARPTQmodel>("LARPTQmodel", relativeTo: phenology);
            CropVernalised = false;
            Phase1complete = false;
            VernalisedInPhase1 = false;
            Phase2complete = false;
            HSFactor = 1.0;
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
