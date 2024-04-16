using System;
using Models.Core;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Final Leaf Number observations (or estimates) for genotype from specific environmental conditions
    /// </summary>
    [Serializable]
    [Description(" Final Leaf Number observations (or estimates) for genotype from specific environmental conditions")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CAMP))]
    public class FinalLeafNumberSet : Model
    {
        /// <summary>Final Leaf Number when fully vernalised before HS1.1 and then grown in >16h Pp</summary>
        [Description("MinLN - Final Leaf Number (FLN) when fully vernalised before HS1.1 and then grown in >16h Pp")]
        public double MinLN { get; set; }

        /// <summary>PpLN - Increase in FLN when fully vernalised before HS1.1 then grown at >18oC in 8h Pp</summary>
        [Description("PpLN - Increase in FLN when fully vernalised before HS1.1 then grown at >18oC in <8h Pp")]
        public double PpLN { get; set; }

        /// <summary>VrnLN - Increase in FLN when un-vernalised and then grown in 8h Pp</summary>
        [Description("VrnLN - Increase in FLN when un-vernalised and grown in 8h Pp")]
        public double VrnLN { get; set; }

        /// <summary>Final Leaf Number when grown at > 20oC in 8h Pp</summary>
        [Description("VxPLN - Increase/Decrease in VrnLN when unvernalised and grown in >16h Pp")]
        public double VxPLN { get; set; }

        /// <summary>Final Leaf Number when fully vernalised before HS1.1 and then grown in >16h Pp</summary>
        public double LV
        {
            get
            {
                return MinLN;
            }
        }
        /// <summary>Final Leaf Number when fully vernalised before HS1.1 and then grown in 8h Pp</summary>
        public double SV
        {
            get
            {
                return MinLN + PpLN;
            }
        }

        /// <summary>Final Leaf Number when grown at > 20oC in 8h Pp</summary>
        public double SN
        {
            get
            {
                return MinLN + VrnLN + PpLN;
            }
        }

        /// <summary>Final Leaf Number when grown at >20oC in >16h Pp</summary>
        public double LN
        {
            get
            {
                return MinLN + VrnLN + VxPLN;
            }
        }
    }
}
