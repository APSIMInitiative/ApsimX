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
    [ViewName("UserInterface.Views.PropertyView")]
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
        /// The value
        /// </summary>
        /// <param name="arrayIndex"></param>
        /// <returns></returns>
        public double Value(int arrayIndex = -1)
        {
            if (lARPTQmodel != null)
            {
                double LAR = lARPTQmodel.CalculateLAR(PTQ.Value(), maxLAR.Value(), minLAR.Value(), PTQhf.Value());
                if (LAR > 0)
                    return 1 / LAR;
                else
                    return 0;
            }
            else
                return 0;
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("Sowing")]
        private void OnSowing(object sender, EventArgs e)
        {
            lARPTQmodel = phenology.FindChild<LARPTQmodel>("LARPTQmodel");
        }
    }
}
