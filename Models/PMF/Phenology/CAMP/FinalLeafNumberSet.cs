using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Newtonsoft.Json;
using System;
using System.Linq;

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
        [Description("Final Leaf Number when fully vernalised before HS1.1 and then grown in >16h Pp")]
        public double LV { get; set; }
        /// <summary>Final Leaf Number when fully vernalised before HS1.1 and then grown in 8h Pp</summary>
        [Description("Final Leaf Number when fully vernalised before HS1.1 and then grown in 8h Pp")]
        public double SV { get; set; }
        /// <summary>Final Leaf Number when grown at >20oC in >16h Pp</summary>
        [Description("Final Leaf Number when grown at >20oC in >16h Pp")]
        public double LN { get; set; }
        /// <summary>Final Leaf Number when grown at > 20oC in 8h Pp</summary>
        [Description("Final Leaf Number when grown at > 20oC in 8h Pp")]
        public double SN { get; set; }
    }
}
