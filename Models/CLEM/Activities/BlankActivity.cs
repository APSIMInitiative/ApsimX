using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Models.CLEM.Resources;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Blank activity for passing details
    /// </summary>
    public class BlankActivity : CLEMActivityBase
    {
        /// <inheritdoc/>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        /// <inheritdoc/>
        public override void DoActivity()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override LabourRequiredArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            throw new NotImplementedException();
        }
    }
}
