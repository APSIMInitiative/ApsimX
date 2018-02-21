using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.CLEM.Resources;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Blank activity for passing details
    /// </summary>
    public class BlankActivity : CLEMActivityBase
    {
        /// <summary>
        /// 
        /// </summary>
        public override void DoActivity()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            throw new NotImplementedException();
        }
    }
}
