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
        protected override void AdjustResourcesForActivity()
        {
            return;
        }

        /// <inheritdoc/>
        protected override void PerformTasksForActivity()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override LabourRequiredArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override List<ResourceRequest> DetermineResourcesForActivity()
        {
            throw new NotImplementedException();
        }

    }
}
