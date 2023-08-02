using System;
using System.Collections.Generic;
using Models.CLEM.Resources;
using Models.CLEM.Interfaces;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Blank activity for passing details
    /// </summary>
    public class BlankActivity : CLEMActivityBase, IHandlesActivityCompanionModels
    {
        /// <inheritdoc/>
        protected override void AdjustResourcesForTimestep()
        {
            return;
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            throw new NotImplementedException();
        }

    }
}
