using System;
using Models.Core;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Controlled environment conditions that FinalLeafNumberSet was observed in.
    /// </summary>
    [Serializable]
    [Description("Controlled environment conditions that FinalLeafNumberSet was observed in")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CAMP))]
    public class FLNParameterEnvironment : Model
    {
        /// <summary>Vernalisation Treatment Temperature</summary>
        [Description("Vernalisation Treatment Temperature")]
        public double VrnTreatTemp { get; set; }
        /// <summary>Final Leaf Number when grown at > 20oC in 8h Pp</summary>
        [Description("Days exposure to vernalisting temperature")]
        public double VrnTreatDuration { get; set; }
        /// <summary>The PTQ under long Pp</summary>
        [Description("The PTQ under long Pp")]
        public double TreatmentPTQ_L { get; set; }
        /// <summary>The PTQ under short Pp</summary>
        [Description("The PTQ under short Pp")]
        public double TreatmentPTQ_S { get; set; }
        /// <summary>Observed Thermal time from sowing to emergence</summary>
        [Description("Observed Thermal time from sowing to emergence")]
        public double TtEmerge { get; set; }
    }
}
